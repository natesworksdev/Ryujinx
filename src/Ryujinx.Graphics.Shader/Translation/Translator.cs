using Ryujinx.Graphics.Shader.CodeGen.Glsl;
using Ryujinx.Graphics.Shader.CodeGen.Spirv;
using Ryujinx.Graphics.Shader.Decoders;
using Ryujinx.Graphics.Shader.IntermediateRepresentation;
using Ryujinx.Graphics.Shader.StructuredIr;
using Ryujinx.Graphics.Shader.Translation.Optimizations;
using System;
using System.Collections.Generic;
using System.Linq;
using static Ryujinx.Graphics.Shader.IntermediateRepresentation.OperandHelper;

namespace Ryujinx.Graphics.Shader.Translation
{
    public static class Translator
    {
        private const int ThreadsPerWarp = 32;
        private const int HeaderSize = 0x50;

        internal readonly struct FunctionCode
        {
            public Operation[] Code { get; }

            public FunctionCode(Operation[] code)
            {
                Code = code;
            }
        }

        public static TranslatorContext CreateContext(ulong address, IGpuAccessor gpuAccessor, TranslationOptions options)
        {
            return DecodeShader(address, gpuAccessor, options);
        }

        internal static ShaderProgram Translate(
            FunctionCode[] functions,
            AttributeUsage attributeUsage,
            ShaderDefinitions definitions,
            ResourceManager resourceManager,
            IGpuAccessor gpuAccessor,
            TranslationOptions options,
            FeatureFlags usedFeatures,
            byte clipDistancesWritten)
        {
            var cfgs = new ControlFlowGraph[functions.Length];
            var frus = new RegisterUsage.FunctionRegisterUsage[functions.Length];

            for (int i = 0; i < functions.Length; i++)
            {
                cfgs[i] = ControlFlowGraph.Create(functions[i].Code);

                if (i != 0)
                {
                    frus[i] = RegisterUsage.RunPass(cfgs[i]);
                }
            }

            List<Function> funcs = new(functions.Length);

            for (int i = 0; i < functions.Length; i++)
            {
                funcs.Add(null);
            }

            HelperFunctionManager hfm = new(funcs, definitions.Stage);

            for (int i = 0; i < functions.Length; i++)
            {
                var cfg = cfgs[i];

                int inArgumentsCount = 0;
                int outArgumentsCount = 0;

                if (i != 0)
                {
                    var fru = frus[i];

                    inArgumentsCount = fru.InArguments.Length;
                    outArgumentsCount = fru.OutArguments.Length;
                }

                if (cfg.Blocks.Length != 0)
                {
                    RegisterUsage.FixupCalls(cfg.Blocks, frus);

                    Dominance.FindDominators(cfg);
                    Dominance.FindDominanceFrontiers(cfg.Blocks);

                    Ssa.Rename(cfg.Blocks);

                    Optimizer.RunPass(hfm, cfg.Blocks, resourceManager, gpuAccessor, options.TargetLanguage, definitions.Stage);
                    Rewriter.RunPass(hfm, cfg.Blocks, resourceManager, gpuAccessor, options.TargetLanguage, definitions.Stage, ref usedFeatures);
                }

                funcs[i] = new Function(cfg.Blocks, $"fun{i}", false, inArgumentsCount, outArgumentsCount);
            }

            var identification = ShaderIdentifier.Identify(funcs, gpuAccessor, definitions.Stage, out int layerInputAttr);

            return Generate(
                funcs,
                attributeUsage,
                definitions,
                resourceManager,
                gpuAccessor,
                usedFeatures,
                clipDistancesWritten,
                options,
                identification,
                layerInputAttr);
        }

        internal static ShaderProgram Generate(
            IReadOnlyList<Function> funcs,
            AttributeUsage attributeUsage,
            ShaderDefinitions definitions,
            ResourceManager resourceManager,
            IGpuAccessor gpuAccessor,
            FeatureFlags usedFeatures,
            byte clipDistancesWritten,
            TranslationOptions options,
            ShaderIdentification identification = ShaderIdentification.None,
            int layerInputAttr = 0)
        {
            var sInfo = StructuredProgram.MakeStructuredProgram(
                funcs,
                attributeUsage,
                definitions,
                resourceManager,
                options.Flags.HasFlag(TranslationFlags.DebugMode));

            var info = new ShaderProgramInfo(
                resourceManager.GetConstantBufferDescriptors(),
                resourceManager.GetStorageBufferDescriptors(),
                resourceManager.GetTextureDescriptors(),
                resourceManager.GetImageDescriptors(),
                identification,
                layerInputAttr,
                definitions.Stage,
                usedFeatures.HasFlag(FeatureFlags.FragCoordXY),
                usedFeatures.HasFlag(FeatureFlags.InstanceId),
                usedFeatures.HasFlag(FeatureFlags.DrawParameters),
                usedFeatures.HasFlag(FeatureFlags.RtLayer),
                clipDistancesWritten,
                definitions.OmapTargets);

            var hostCapabilities = new HostCapabilities(
                gpuAccessor.QueryHostReducedPrecision(),
                gpuAccessor.QueryHostSupportsFragmentShaderInterlock(),
                gpuAccessor.QueryHostSupportsFragmentShaderOrderingIntel(),
                gpuAccessor.QueryHostSupportsGeometryShaderPassthrough(),
                gpuAccessor.QueryHostSupportsShaderBallot(),
                gpuAccessor.QueryHostSupportsShaderBarrierDivergence(),
                gpuAccessor.QueryHostSupportsTextureShadowLod(),
                gpuAccessor.QueryHostSupportsViewportMask());

            return options.TargetLanguage switch
            {
                TargetLanguage.Glsl => new ShaderProgram(info, TargetLanguage.Glsl, GlslGenerator.Generate(
                    sInfo,
                    attributeUsage,
                    definitions,
                    resourceManager.Properties,
                    hostCapabilities,
                    options.TargetApi)),
                TargetLanguage.Spirv => new ShaderProgram(info, TargetLanguage.Spirv, SpirvGenerator.Generate(
                    sInfo,
                    attributeUsage,
                    definitions,
                    resourceManager.Properties,
                    hostCapabilities,
                    options.TargetApi)),
                _ => throw new NotImplementedException(options.TargetLanguage.ToString()),
            };
        }

        private static TranslatorContext DecodeShader(ulong address, IGpuAccessor gpuAccessor, TranslationOptions options)
        {
            int localMemorySize;
            ShaderDefinitions definitions;
            DecodedProgram program;

            if (options.Flags.HasFlag(TranslationFlags.Compute))
            {
                definitions = CreateComputeDefinitions(gpuAccessor);
                localMemorySize = gpuAccessor.QueryComputeLocalMemorySize();

                program = Decoder.Decode(definitions, gpuAccessor, address);
            }
            else
            {
                ShaderHeader header = new ShaderHeader(gpuAccessor, address);

                definitions = CreateGraphicsDefinitions(gpuAccessor, header, options);
                localMemorySize = GetLocalMemorySize(header);

                program = Decoder.Decode(definitions, gpuAccessor, address + HeaderSize);
            }

            ulong maxEndAddress = 0;

            foreach (DecodedFunction function in program)
            {
                foreach (Block block in function.Blocks)
                {
                    if (maxEndAddress < block.EndAddress)
                    {
                        maxEndAddress = block.EndAddress;
                    }
                }
            }

            int size = (int)maxEndAddress + (options.Flags.HasFlag(TranslationFlags.Compute) ? 0 : HeaderSize);

            return new TranslatorContext(address, size, localMemorySize, definitions, gpuAccessor, options, program);
        }

        private static ShaderDefinitions CreateComputeDefinitions(IGpuAccessor gpuAccessor)
        {
            return new ShaderDefinitions(
                ShaderStage.Compute,
                gpuAccessor.QueryComputeLocalSizeX(),
                gpuAccessor.QueryComputeLocalSizeY(),
                gpuAccessor.QueryComputeLocalSizeZ());
        }

        private static ShaderDefinitions CreateGraphicsDefinitions(IGpuAccessor gpuAccessor, ShaderHeader header, TranslationOptions options)
        {
            bool transformFeedbackEnabled =
                gpuAccessor.QueryTransformFeedbackEnabled() &&
                gpuAccessor.QueryHostSupportsTransformFeedback();
            TransformFeedbackOutput[] transformFeedbackOutputs = null;
            ulong transformFeedbackVecMap = 0UL;

            if (transformFeedbackEnabled)
            {
                transformFeedbackOutputs = new TransformFeedbackOutput[0xc0];

                for (int tfbIndex = 0; tfbIndex < 4; tfbIndex++)
                {
                    var locations = gpuAccessor.QueryTransformFeedbackVaryingLocations(tfbIndex);
                    var stride = gpuAccessor.QueryTransformFeedbackStride(tfbIndex);

                    for (int i = 0; i < locations.Length; i++)
                    {
                        byte wordOffset = locations[i];
                        if (wordOffset < 0xc0)
                        {
                            transformFeedbackOutputs[wordOffset] = new TransformFeedbackOutput(tfbIndex, i * 4, stride);
                            transformFeedbackVecMap |= 1UL << (wordOffset / 4);
                        }
                    }
                }
            }

            bool tessCw = false;
            TessPatchType tessPatchType = default;
            TessSpacing tessSpacing = default;

            AttributeType[] attributeTypes = null;
            AttributeType[] fragmentOutputTypes = null;

            InputTopology inputTopology = default;
            OutputTopology outputTopology = default;
            int maxOutputVertexCount = 0;

            bool dualSourceBlend = false;
            bool earlyZForce = false;

            switch (header.Stage)
            {
                case ShaderStage.Vertex:
                    attributeTypes = new AttributeType[32];

                    for (int location = 0; location < attributeTypes.Length; location++)
                    {
                        attributeTypes[location] = gpuAccessor.QueryAttributeType(location);
                    }
                    break;
                case ShaderStage.TessellationEvaluation:
                    tessCw = gpuAccessor.QueryTessCw();
                    tessPatchType = gpuAccessor.QueryTessPatchType();
                    tessSpacing = gpuAccessor.QueryTessSpacing();
                    break;
                case ShaderStage.Geometry:
                    inputTopology = gpuAccessor.QueryPrimitiveTopology();
                    outputTopology = header.OutputTopology;
                    maxOutputVertexCount = header.MaxOutputVertexCount;
                    break;
                case ShaderStage.Fragment:
                    dualSourceBlend = gpuAccessor.QueryDualSourceBlendEnable();
                    earlyZForce = gpuAccessor.QueryEarlyZForce();

                    fragmentOutputTypes = new AttributeType[8];

                    for (int location = 0; location < fragmentOutputTypes.Length; location++)
                    {
                        fragmentOutputTypes[location] = gpuAccessor.QueryFragmentOutputType(location);
                    }
                    break;
            }

            return new ShaderDefinitions(
                header.Stage,
                tessCw,
                tessPatchType,
                tessSpacing,
                header.Stage == ShaderStage.Geometry && header.GpPassthrough,
                header.ThreadsPerInputPrimitive,
                inputTopology,
                outputTopology,
                maxOutputVertexCount,
                dualSourceBlend,
                earlyZForce,
                header.ImapTypes,
                header.OmapTargets,
                header.OmapSampleMask,
                header.OmapDepth,
                options.TargetApi == TargetApi.Vulkan || gpuAccessor.QueryYNegateEnabled(),
                transformFeedbackEnabled,
                transformFeedbackVecMap,
                transformFeedbackOutputs,
                attributeTypes,
                fragmentOutputTypes);
        }

        private static int GetLocalMemorySize(ShaderHeader header)
        {
            return header.ShaderLocalMemoryLowSize + header.ShaderLocalMemoryHighSize + (header.ShaderLocalMemoryCrsSize / ThreadsPerWarp);
        }

        internal static FunctionCode[] EmitShader(
            TranslatorContext translatorContext,
            ResourceManager resourceManager,
            DecodedProgram program,
            bool initializeOutputs,
            out int initializationOperations)
        {
            initializationOperations = 0;

            FunctionMatch.RunPass(program);

            foreach (DecodedFunction function in program.Where(x => !x.IsCompilerGenerated).OrderBy(x => x.Address))
            {
                program.AddFunctionAndSetId(function);
            }

            FunctionCode[] functions = new FunctionCode[program.FunctionsWithIdCount];

            for (int index = 0; index < functions.Length; index++)
            {
                EmitterContext context = new(translatorContext, resourceManager, program, index != 0);

                if (initializeOutputs && index == 0)
                {
                    EmitOutputsInitialization(context, translatorContext.AttributeUsage, translatorContext.GpuAccessor, translatorContext.Stage);
                    initializationOperations = context.OperationsCount;
                }

                DecodedFunction function = program.GetFunctionById(index);

                foreach (Block block in function.Blocks)
                {
                    context.CurrBlock = block;

                    context.EnterBlock(block.Address);

                    EmitOps(context, block);
                }

                functions[index] = new FunctionCode(context.GetOperations());
            }

            return functions;
        }

        private static void EmitOutputsInitialization(EmitterContext context, AttributeUsage attributeUsage, IGpuAccessor gpuAccessor, ShaderStage stage)
        {
            // Compute has no output attributes, and fragment is the last stage, so we
            // don't need to initialize outputs on those stages.
            if (stage == ShaderStage.Compute || stage == ShaderStage.Fragment)
            {
                return;
            }

            if (stage == ShaderStage.Vertex)
            {
                InitializePositionOutput(context);
            }

            UInt128 usedAttributes = context.TranslatorContext.AttributeUsage.NextInputAttributesComponents;
            while (usedAttributes != UInt128.Zero)
            {
                int index = (int)UInt128.TrailingZeroCount(usedAttributes);
                int vecIndex = index / 4;

                usedAttributes &= ~(UInt128.One << index);

                // We don't need to initialize passthrough attributes.
                if ((context.TranslatorContext.AttributeUsage.PassthroughAttributes & (1 << vecIndex)) != 0)
                {
                    continue;
                }

                InitializeOutputComponent(context, vecIndex, index & 3, perPatch: false);
            }

            if (context.TranslatorContext.AttributeUsage.NextUsedInputAttributesPerPatch != null)
            {
                foreach (int vecIndex in context.TranslatorContext.AttributeUsage.NextUsedInputAttributesPerPatch.Order())
                {
                    InitializeOutput(context, vecIndex, perPatch: true);
                }
            }

            if (attributeUsage.NextUsesFixedFuncAttributes)
            {
                bool supportsLayerFromVertexOrTess = gpuAccessor.QueryHostSupportsLayerVertexTessellation();
                int fixedStartAttr = supportsLayerFromVertexOrTess ? 0 : 1;

                for (int i = fixedStartAttr; i < fixedStartAttr + 5 + AttributeConsts.TexCoordCount; i++)
                {
                    int index = attributeUsage.GetFreeUserAttribute(isOutput: true, i);
                    if (index < 0)
                    {
                        break;
                    }

                    InitializeOutput(context, index, perPatch: false);

                    attributeUsage.SetOutputUserAttributeFixedFunc(index);
                }
            }
        }

        private static void InitializePositionOutput(EmitterContext context)
        {
            for (int c = 0; c < 4; c++)
            {
                context.Store(StorageKind.Output, IoVariable.Position, null, Const(c), ConstF(c == 3 ? 1f : 0f));
            }
        }

        private static void InitializeOutput(EmitterContext context, int location, bool perPatch)
        {
            for (int c = 0; c < 4; c++)
            {
                InitializeOutputComponent(context, location, c, perPatch);
            }
        }

        private static void InitializeOutputComponent(EmitterContext context, int location, int c, bool perPatch)
        {
            StorageKind storageKind = perPatch ? StorageKind.OutputPerPatch : StorageKind.Output;

            if (context.TranslatorContext.Definitions.OaIndexing)
            {
                Operand invocationId = null;

                if (context.TranslatorContext.Definitions.Stage == ShaderStage.TessellationControl && !perPatch)
                {
                    invocationId = context.Load(StorageKind.Input, IoVariable.InvocationId);
                }

                int index = location * 4 + c;

                context.Store(storageKind, IoVariable.UserDefined, invocationId, Const(index), ConstF(c == 3 ? 1f : 0f));
            }
            else
            {
                if (context.TranslatorContext.Definitions.Stage == ShaderStage.TessellationControl && !perPatch)
                {
                    Operand invocationId = context.Load(StorageKind.Input, IoVariable.InvocationId);
                    context.Store(storageKind, IoVariable.UserDefined, Const(location), invocationId, Const(c), ConstF(c == 3 ? 1f : 0f));
                }
                else
                {
                    context.Store(storageKind, IoVariable.UserDefined, null, Const(location), Const(c), ConstF(c == 3 ? 1f : 0f));
                }
            }
        }

        private static void EmitOps(EmitterContext context, Block block)
        {
            for (int opIndex = 0; opIndex < block.OpCodes.Count; opIndex++)
            {
                InstOp op = block.OpCodes[opIndex];

                if (context.TranslatorContext.Options.Flags.HasFlag(TranslationFlags.DebugMode))
                {
                    string instName;

                    if (op.Emitter != null)
                    {
                        instName = op.Name.ToString();
                    }
                    else
                    {
                        instName = "???";

                        context.TranslatorContext.GpuAccessor.Log($"Invalid instruction at 0x{op.Address:X6} (0x{op.RawOpCode:X16}).");
                    }

                    string dbgComment = $"0x{op.Address:X6}: 0x{op.RawOpCode:X16} {instName}";

                    context.Add(new CommentNode(dbgComment));
                }

                InstConditional opConditional = new(op.RawOpCode);

                bool noPred = op.Props.HasFlag(InstProps.NoPred);
                if (!noPred && opConditional.Pred == RegisterConsts.PredicateTrueIndex && opConditional.PredInv)
                {
                    continue;
                }

                Operand predSkipLbl = null;

                if (Decoder.IsPopBranch(op.Name))
                {
                    // If the instruction is a SYNC or BRK instruction with only one
                    // possible target address, then the instruction is basically
                    // just a simple branch, we can generate code similar to branch
                    // instructions, with the condition check on the branch itself.
                    noPred = block.SyncTargets.Count <= 1;
                }
                else if (op.Name == InstName.Bra)
                {
                    noPred = true;
                }

                if (!(opConditional.Pred == RegisterConsts.PredicateTrueIndex || noPred))
                {
                    Operand label;

                    if (opIndex == block.OpCodes.Count - 1 && block.HasNext())
                    {
                        label = context.GetLabel(block.Successors[0].Address);
                    }
                    else
                    {
                        label = Label();

                        predSkipLbl = label;
                    }

                    Operand pred = Register(opConditional.Pred, RegisterType.Predicate);

                    if (opConditional.PredInv)
                    {
                        context.BranchIfTrue(label, pred);
                    }
                    else
                    {
                        context.BranchIfFalse(label, pred);
                    }
                }

                context.CurrOp = op;

                op.Emitter?.Invoke(context);

                if (predSkipLbl != null)
                {
                    context.MarkLabel(predSkipLbl);
                }
            }
        }
    }
}
