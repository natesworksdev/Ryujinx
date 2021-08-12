using Ryujinx.Common;
using Ryujinx.Graphics.Shader.StructuredIr;
using Ryujinx.Graphics.Shader.Translation;
using Spv.Generator;
using System;
using System.Collections.Generic;
using System.Linq;
using static Spv.Specification;

namespace Ryujinx.Graphics.Shader.CodeGen.Spirv
{
    using SpvInstruction = Spv.Generator.Instruction;

    static class Declarations
    {
        // At least 16 attributes are guaranteed by the spec.
        public const int MaxAttributes = 16;

        private static readonly string[] StagePrefixes = new string[] { "cp", "vp", "tcp", "tep", "gp", "fp" };

        public static void DeclareParameters(CodeGenContext context, StructuredFunction function)
        {
            DeclareParameters(context, function.InArguments, 0);
            DeclareParameters(context, function.OutArguments, function.InArguments.Length);
        }

        private static void DeclareParameters(CodeGenContext context, IEnumerable<VariableType> argTypes, int argIndex)
        {
            foreach (var argType in argTypes)
            {
                var argPointerType = context.TypePointer(StorageClass.Function, context.GetType(argType.Convert()));
                var spvArg = context.FunctionParameter(argPointerType);

                context.DeclareArgument(argIndex++, spvArg);
            }
        }

        public static void DeclareLocals(CodeGenContext context, StructuredFunction function)
        {
            foreach (AstOperand local in function.Locals)
            {
                var localPointerType = context.TypePointer(StorageClass.Function, context.GetType(local.VarType.Convert()));
                var spvLocal = context.Variable(localPointerType, StorageClass.Function);

                context.AddLocalVariable(spvLocal);
                context.DeclareLocal(local, spvLocal);
            }
        }

        public static void DeclareLocalForArgs(CodeGenContext context, List<StructuredFunction> functions)
        {
            for (int funcIndex = 0; funcIndex < functions.Count; funcIndex++)
            {
                StructuredFunction function = functions[funcIndex];
                SpvInstruction[] locals = new SpvInstruction[function.InArguments.Length];

                for (int i = 0; i < function.InArguments.Length; i++)
                {
                    var type = function.GetArgumentType(i).Convert();
                    var localPointerType = context.TypePointer(StorageClass.Function, context.GetType(type));
                    var spvLocal = context.Variable(localPointerType, StorageClass.Function);

                    context.AddLocalVariable(spvLocal);

                    locals[i] = spvLocal;
                }

                context.DeclareLocalForArgs(funcIndex, locals);
            }
        }

        public static void DeclareAll(CodeGenContext context, StructuredProgramInfo info)
        {
            if (context.Config.Stage == ShaderStage.Compute)
            {
                int localMemorySize = BitUtils.DivRoundUp(context.Config.GpuAccessor.QueryComputeLocalMemorySize(), 4);

                if (localMemorySize != 0)
                {
                    DeclareLocalMemory(context, localMemorySize);
                }

                int sharedMemorySize = BitUtils.DivRoundUp(context.Config.GpuAccessor.QueryComputeSharedMemorySize(), 4);

                if (sharedMemorySize != 0)
                {
                    DeclareSharedMemory(context, sharedMemorySize);
                }
            }
            else if (context.Config.LocalMemorySize != 0)
            {
                int localMemorySize = BitUtils.DivRoundUp(context.Config.LocalMemorySize, 4);
                DeclareLocalMemory(context, localMemorySize);
            }

            DeclareUniformBuffers(context, context.Config.GetConstantBufferDescriptors());
            DeclareStorageBuffers(context, context.Config.GetStorageBufferDescriptors());
            DeclareSamplers(context, context.Config.GetTextureDescriptors());
            DeclareImages(context, context.Config.GetImageDescriptors());
            DeclareInputAttributes(context, info);
            DeclareOutputAttributes(context, info);
        }

        private static void DeclareLocalMemory(CodeGenContext context, int size)
        {
            context.LocalMemory = DeclareMemory(context, StorageClass.Private, size);
        }

        private static void DeclareSharedMemory(CodeGenContext context, int size)
        {
            context.SharedMemory = DeclareMemory(context, StorageClass.Workgroup, size);
        }

        private static SpvInstruction DeclareMemory(CodeGenContext context, StorageClass storage, int size)
        {
            var arrayType = context.TypeArray(context.TypeU32(), context.Constant(context.TypeU32(), size));
            var pointerType = context.TypePointer(storage, arrayType);
            var variable = context.Variable(pointerType, storage);

            context.AddGlobalVariable(variable);

            return variable;
        }

        private static void DeclareUniformBuffers(CodeGenContext context, BufferDescriptor[] descriptors)
        {
            uint ubSize = Constants.ConstantBufferSize / 16;

            var ubArrayType = context.TypeArray(context.TypeVector(context.TypeFP32(), 4), context.Constant(context.TypeU32(), ubSize), true);
            context.Decorate(ubArrayType, Decoration.ArrayStride, (LiteralInteger)16);
            var ubStructType = context.TypeStruct(true, ubArrayType);
            context.Decorate(ubStructType, Decoration.Block);
            context.MemberDecorate(ubStructType, 0, Decoration.Offset, (LiteralInteger)0);
            var ubPointerType = context.TypePointer(StorageClass.Uniform, ubStructType);

            foreach (var descriptor in descriptors)
            {
                var ubVariable = context.Variable(ubPointerType, StorageClass.Uniform);

                context.Name(ubVariable, $"{GetStagePrefix(context.Config.Stage)}_c{descriptor.Slot}");
                context.Decorate(ubVariable, Decoration.DescriptorSet, (LiteralInteger)0);
                context.Decorate(ubVariable, Decoration.Binding, (LiteralInteger)descriptor.Binding);
                context.AddGlobalVariable(ubVariable);
                context.UniformBuffers.Add(descriptor.Slot, ubVariable);
            }
        }

        private static void DeclareStorageBuffers(CodeGenContext context, BufferDescriptor[] descriptors)
        {
            if (descriptors.Length == 0)
            {
                return;
            }

            int setIndex = context.Config.Options.TargetApi == TargetApi.Vulkan ? 1 : 0;
            int count = descriptors.Max(x => x.Binding) + 1;

            var sbArrayType = context.TypeRuntimeArray(context.TypeU32());
            context.Decorate(sbArrayType, Decoration.ArrayStride, (LiteralInteger)4);
            var sbStructType = context.TypeStruct(true, sbArrayType);
            context.Decorate(sbStructType, Decoration.BufferBlock);
            context.MemberDecorate(sbStructType, 0, Decoration.Offset, (LiteralInteger)0);
            var sbStructArrayType = context.TypeArray(sbStructType, context.Constant(context.TypeU32(), count));
            var sbPointerType = context.TypePointer(StorageClass.Uniform, sbStructArrayType);
            var sbVariable = context.Variable(sbPointerType, StorageClass.Uniform);

            context.Name(sbVariable, $"{GetStagePrefix(context.Config.Stage)}_s");
            context.Decorate(sbVariable, Decoration.DescriptorSet, (LiteralInteger)setIndex);
            context.Decorate(sbVariable, Decoration.Binding, (LiteralInteger)descriptors[0].Binding);
            context.AddGlobalVariable(sbVariable);

            context.StorageBuffersArray = sbVariable;
        }

        private static void DeclareSamplers(CodeGenContext context, TextureDescriptor[] descriptors)
        {
            foreach (var descriptor in descriptors)
            {
                var meta = new TextureMeta(descriptor.CbufSlot, descriptor.HandleIndex, descriptor.Format, descriptor.Type);

                if (context.Samplers.ContainsKey(meta))
                {
                    continue;
                }

                bool isBuffer = (descriptor.Type & SamplerType.Mask) == SamplerType.TextureBuffer;
                int setIndex = context.Config.Options.TargetApi == TargetApi.Vulkan ? (isBuffer ? 4 : 2) : 0;

                var dim = (meta.Type & SamplerType.Mask) switch
                {
                    SamplerType.Texture1D => Dim.Dim1D,
                    SamplerType.Texture2D => Dim.Dim2D,
                    SamplerType.Texture3D => Dim.Dim3D,
                    SamplerType.TextureCube => Dim.Cube,
                    SamplerType.TextureBuffer => Dim.Buffer,
                    _ => throw new InvalidOperationException($"Invalid sampler type \"{meta.Type & SamplerType.Mask}\".")
                };

                var imageType = context.TypeImage(
                    context.TypeFP32(),
                    dim,
                    meta.Type.HasFlag(SamplerType.Shadow),
                    meta.Type.HasFlag(SamplerType.Array),
                    meta.Type.HasFlag(SamplerType.Multisample),
                    1,
                    ImageFormat.Unknown);

                var nameSuffix = meta.CbufSlot < 0 ? $"_tcb_{meta.Handle:X}" : $"_cb{meta.CbufSlot}_{meta.Handle:X}";

                var sampledImageType = context.TypeSampledImage(imageType);
                var sampledImagePointerType = context.TypePointer(StorageClass.UniformConstant, sampledImageType);
                var sampledImageVariable = context.Variable(sampledImagePointerType, StorageClass.UniformConstant);

                context.Samplers.Add(meta, (imageType, sampledImageType, sampledImageVariable));

                context.Name(sampledImageVariable, $"{GetStagePrefix(context.Config.Stage)}_tex{nameSuffix}");
                context.Decorate(sampledImageVariable, Decoration.DescriptorSet, (LiteralInteger)setIndex);
                context.Decorate(sampledImageVariable, Decoration.Binding, (LiteralInteger)descriptor.Binding);
                context.AddGlobalVariable(sampledImageVariable);
            }
        }

        private static void DeclareImages(CodeGenContext context, TextureDescriptor[] descriptors)
        {
            foreach (var descriptor in descriptors)
            {
                var meta = new TextureMeta(descriptor.CbufSlot, descriptor.HandleIndex, descriptor.Format, descriptor.Type);

                if (context.Images.ContainsKey(meta))
                {
                    continue;
                }

                bool isBuffer = (descriptor.Type & SamplerType.Mask) == SamplerType.TextureBuffer;
                int setIndex = context.Config.Options.TargetApi == TargetApi.Vulkan ? (isBuffer ? 5 : 3) : 0;

                var dim = GetDim(meta.Type);

                var imageType = context.TypeImage(
                    context.GetType(meta.Format.GetComponentType().Convert()),
                    dim,
                    meta.Type.HasFlag(SamplerType.Shadow),
                    meta.Type.HasFlag(SamplerType.Array),
                    meta.Type.HasFlag(SamplerType.Multisample),
                    2,
                    GetImageFormat(meta.Format));

                var nameSuffix = meta.CbufSlot < 0 ?
                    $"_tcb_{meta.Handle:X}_{meta.Format.ToGlslFormat()}" :
                    $"_cb{meta.CbufSlot}_{meta.Handle:X}_{meta.Format.ToGlslFormat()}";

                var imagePointerType = context.TypePointer(StorageClass.UniformConstant, imageType);
                var imageVariable = context.Variable(imagePointerType, StorageClass.UniformConstant);

                context.Images.Add(meta, (imageType, imageVariable));

                context.Name(imageVariable, $"{GetStagePrefix(context.Config.Stage)}_img{nameSuffix}");
                context.Decorate(imageVariable, Decoration.DescriptorSet, (LiteralInteger)setIndex);
                context.Decorate(imageVariable, Decoration.Binding, (LiteralInteger)descriptor.Binding);
                context.AddGlobalVariable(imageVariable);
            }
        }

        private static Dim GetDim(SamplerType type)
        {
            return (type & SamplerType.Mask) switch
            {
                SamplerType.Texture1D => Dim.Dim1D,
                SamplerType.Texture2D => Dim.Dim2D,
                SamplerType.Texture3D => Dim.Dim3D,
                SamplerType.TextureCube => Dim.Cube,
                SamplerType.TextureBuffer => Dim.Buffer,
                _ => throw new ArgumentException($"Invalid sampler type \"{type & SamplerType.Mask}\".")
            };
        }

        private static ImageFormat GetImageFormat(TextureFormat format)
        {
            return format switch
            {
                TextureFormat.Unknown => ImageFormat.Unknown,
                TextureFormat.R8Unorm => ImageFormat.R8,
                TextureFormat.R8Snorm => ImageFormat.R8Snorm,
                TextureFormat.R8Uint => ImageFormat.R8ui,
                TextureFormat.R8Sint => ImageFormat.R8i,
                TextureFormat.R16Float => ImageFormat.R16f,
                TextureFormat.R16Unorm => ImageFormat.R16,
                TextureFormat.R16Snorm => ImageFormat.R16Snorm,
                TextureFormat.R16Uint => ImageFormat.R16ui,
                TextureFormat.R16Sint => ImageFormat.R16i,
                TextureFormat.R32Float => ImageFormat.R32f,
                TextureFormat.R32Uint => ImageFormat.R32ui,
                TextureFormat.R32Sint => ImageFormat.R32i,
                TextureFormat.R8G8Unorm => ImageFormat.Rg8,
                TextureFormat.R8G8Snorm => ImageFormat.Rg8Snorm,
                TextureFormat.R8G8Uint => ImageFormat.Rg8ui,
                TextureFormat.R8G8Sint => ImageFormat.Rg8i,
                TextureFormat.R16G16Float => ImageFormat.Rg16f,
                TextureFormat.R16G16Unorm => ImageFormat.Rg16,
                TextureFormat.R16G16Snorm => ImageFormat.Rg16Snorm,
                TextureFormat.R16G16Uint => ImageFormat.Rg16ui,
                TextureFormat.R16G16Sint => ImageFormat.Rg16i,
                TextureFormat.R32G32Float => ImageFormat.Rg32f,
                TextureFormat.R32G32Uint => ImageFormat.Rg32ui,
                TextureFormat.R32G32Sint => ImageFormat.Rg32i,
                TextureFormat.R8G8B8A8Unorm => ImageFormat.Rgba8,
                TextureFormat.R8G8B8A8Snorm => ImageFormat.Rgba8Snorm,
                TextureFormat.R8G8B8A8Uint => ImageFormat.Rgba8ui,
                TextureFormat.R8G8B8A8Sint => ImageFormat.Rgba8i,
                TextureFormat.R16G16B16A16Float => ImageFormat.Rgba16f,
                TextureFormat.R16G16B16A16Unorm => ImageFormat.Rgba16,
                TextureFormat.R16G16B16A16Snorm => ImageFormat.Rgba16Snorm,
                TextureFormat.R16G16B16A16Uint => ImageFormat.Rgba16ui,
                TextureFormat.R16G16B16A16Sint => ImageFormat.Rgba16i,
                TextureFormat.R32G32B32A32Float => ImageFormat.Rgba32f,
                TextureFormat.R32G32B32A32Uint => ImageFormat.Rgba32ui,
                TextureFormat.R32G32B32A32Sint => ImageFormat.Rgba32i,
                TextureFormat.R10G10B10A2Unorm => ImageFormat.Rgb10A2,
                TextureFormat.R10G10B10A2Uint => ImageFormat.Rgb10a2ui,
                TextureFormat.R11G11B10Float => ImageFormat.R11fG11fB10f,
                _ => throw new ArgumentException($"Invalid texture format \"{format}\".")
            };
        }

        private static void DeclareInputAttributes(CodeGenContext context, StructuredProgramInfo info)
        {
            foreach (int attr in info.Inputs)
            {
                PixelImap iq = PixelImap.Unused;

                if (context.Config.Stage == ShaderStage.Fragment &&
                    attr >= AttributeConsts.UserAttributeBase &&
                    attr < AttributeConsts.UserAttributeEnd)
                {
                    iq = context.Config.ImapTypes[(attr - AttributeConsts.UserAttributeBase) / 16].GetFirstUsedType();
                }

                if (context.Config.Options.Flags.HasFlag(TranslationFlags.Feedback))
                {
                    throw new NotImplementedException();
                }
                else
                {
                    DeclareInputOrOutput(context, attr, false, iq);
                }
            }
        }

        private static void DeclareOutputAttributes(CodeGenContext context, StructuredProgramInfo info)
        {
            foreach (int attr in info.Outputs)
            {
                DeclareOutputAttribute(context, attr);
            }

            if (context.Config.Stage != ShaderStage.Compute &&
                context.Config.Stage != ShaderStage.Fragment &&
                !context.Config.GpPassthrough)
            {
                for (int attr = 0; attr < MaxAttributes; attr++)
                {
                    DeclareOutputAttribute(context, AttributeConsts.UserAttributeBase + attr * 16);
                }

                if (context.Config.Stage == ShaderStage.Vertex)
                {
                    DeclareOutputAttribute(context, AttributeConsts.PositionX);
                }
            }
        }

        private static void DeclareOutputAttribute(CodeGenContext context, int attr)
        {
            if (context.Config.Options.Flags.HasFlag(TranslationFlags.Feedback))
            {
                throw new NotImplementedException();
            }
            else
            {
                DeclareInputOrOutput(context, attr, true);
            }
        }

        public static void DeclareInvocationId(CodeGenContext context)
        {
            DeclareInputOrOutput(context, AttributeConsts.LaneId, false);
        }

        private static void DeclareInputOrOutput(CodeGenContext context, int attr, bool isOutAttr, PixelImap iq = PixelImap.Unused)
        {
            var dict = isOutAttr ? context.Outputs : context.Inputs;
            var attrInfo = AttributeInfo.From(context.Config, attr);

            if (attrInfo.BaseValue == AttributeConsts.PositionX && context.Config.Stage != ShaderStage.Fragment)
            {
                isOutAttr = true;
                dict = context.Outputs;
            }

            if (dict.ContainsKey(attrInfo.BaseValue))
            {
                return;
            }

            var storageClass = isOutAttr ? StorageClass.Output : StorageClass.Input;
            var spvType = context.TypePointer(storageClass, context.GetType(attrInfo.Type, attrInfo.Length));
            var spvVar = context.Variable(spvType, storageClass);

            if (attrInfo.IsBuiltin)
            {
                context.Decorate(spvVar, Decoration.BuiltIn, (LiteralInteger)GetBuiltIn(context, attrInfo.BaseValue));
            }
            else if (attr >= AttributeConsts.UserAttributeBase && attr < AttributeConsts.UserAttributeEnd)
            {
                int location = (attr - AttributeConsts.UserAttributeBase) / 16;
                context.Decorate(spvVar, Decoration.Location, (LiteralInteger)location);

                if (!isOutAttr)
                {
                    switch (iq)
                    {
                        case PixelImap.Constant:
                            context.Decorate(spvVar, Decoration.Flat);
                            break;
                        case PixelImap.ScreenLinear:
                            context.Decorate(spvVar, Decoration.NoPerspective);
                            break;
                    }
                }
            }
            else if (attr >= AttributeConsts.FragmentOutputColorBase && attr < AttributeConsts.FragmentOutputColorEnd)
            {
                int location = (attr - AttributeConsts.FragmentOutputColorBase) / 16;
                context.Decorate(spvVar, Decoration.Location, (LiteralInteger)location);
            }

            context.AddGlobalVariable(spvVar);
            dict.Add(attrInfo.BaseValue, spvVar);
        }

        private static BuiltIn GetBuiltIn(CodeGenContext context, int attr)
        {
            return attr switch
            {
                AttributeConsts.Layer => BuiltIn.Layer,
                AttributeConsts.ViewportIndex => BuiltIn.ViewportIndex,
                AttributeConsts.PointSize => BuiltIn.PointSize,
                AttributeConsts.PositionX => context.Config.Stage == ShaderStage.Fragment ? BuiltIn.FragCoord : BuiltIn.Position,
                AttributeConsts.ClipDistance0 => BuiltIn.ClipDistance,
                AttributeConsts.PointCoordX => BuiltIn.PointCoord,
                AttributeConsts.TessCoordX => BuiltIn.TessCoord,
                AttributeConsts.InstanceId => BuiltIn.InstanceId,
                AttributeConsts.VertexId => BuiltIn.VertexId,
                AttributeConsts.FrontFacing => BuiltIn.FrontFacing,
                AttributeConsts.FragmentOutputDepth => BuiltIn.FragDepth,
                AttributeConsts.ThreadKill => BuiltIn.HelperInvocation,
                AttributeConsts.ThreadIdX => BuiltIn.LocalInvocationId,
                AttributeConsts.CtaIdX => BuiltIn.WorkgroupId,
                AttributeConsts.LaneId => BuiltIn.SubgroupLocalInvocationId,
                AttributeConsts.EqMask => BuiltIn.SubgroupEqMask,
                AttributeConsts.GeMask => BuiltIn.SubgroupGeMask,
                AttributeConsts.GtMask => BuiltIn.SubgroupGtMask,
                AttributeConsts.LeMask => BuiltIn.SubgroupLeMask,
                AttributeConsts.LtMask => BuiltIn.SubgroupLtMask,
                _ => throw new ArgumentException($"Invalid attribute number 0x{attr:X}.")
            };
        }

        private static string GetStagePrefix(ShaderStage stage)
        {
            return StagePrefixes[(int)stage];
        }
    }
}
