using Ryujinx.Graphics.Shader.Decoders;
using Ryujinx.Graphics.Shader.IntermediateRepresentation;
using Ryujinx.Graphics.Shader.StructuredIr;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using static Ryujinx.Graphics.Shader.IntermediateRepresentation.OperandHelper;
using static Ryujinx.Graphics.Shader.Translation.Translator;

namespace Ryujinx.Graphics.Shader.Translation
{
    public class TranslatorContext
    {
        private readonly DecodedProgram _program;
        private readonly int _localMemorySize;

        public ulong Address { get; }
        public int Size { get; }
        public int Cb1DataSize => _program.Cb1DataSize;

        internal bool HasLayerInputAttribute { get; private set; }
        internal int GpLayerInputAttribute { get; private set; }

        internal AttributeUsage AttributeUsage => _program.AttributeUsage;

        internal ShaderDefinitions Definitions { get; }

        public ShaderStage Stage => Definitions.Stage;

        internal IGpuAccessor GpuAccessor { get; }

        internal TranslationOptions Options { get; }

        internal ResourceManager ResourceManager { get; set; }

        internal byte ClipDistancesWritten { get; private set; }

        internal FeatureFlags UsedFeatures { get; private set; }

        public bool LayerOutputWritten { get; private set; }
        public int LayerOutputAttribute { get; private set; }

        internal TranslatorContext(
            ulong address,
            int size,
            int localMemorySize,
            ShaderDefinitions definitions,
            IGpuAccessor gpuAccessor,
            TranslationOptions options,
            DecodedProgram program)
        {
            Address = address;
            Size = size;
            _program = program;
            _localMemorySize = localMemorySize;
            Definitions = definitions;
            GpuAccessor = gpuAccessor;
            Options = options;

            SetUsedFeature(program.UsedFeatures);

            ResourceManager = new ResourceManager(definitions.Stage, gpuAccessor);

            if (!gpuAccessor.QueryHostSupportsTransformFeedback() && gpuAccessor.QueryTransformFeedbackEnabled())
            {
                StructureType tfeInfoStruct = new StructureType(new StructureField[]
                {
                    new StructureField(AggregateType.Array | AggregateType.U32, "base_offset", 4),
                    new StructureField(AggregateType.U32, "vertex_count")
                });

                BufferDefinition tfeInfoBuffer = new BufferDefinition(BufferLayout.Std430, 1, Constants.TfeInfoBinding, "tfe_info", tfeInfoStruct);

                ResourceManager.Properties.AddOrUpdateStorageBuffer(Constants.TfeInfoBinding, tfeInfoBuffer);

                StructureType tfeDataStruct = new StructureType(new StructureField[]
                {
                    new StructureField(AggregateType.Array | AggregateType.U32, "data", 0)
                });

                for (int i = 0; i < Constants.TfeBuffersCount; i++)
                {
                    int binding = Constants.TfeBufferBaseBinding + i;
                    BufferDefinition tfeDataBuffer = new BufferDefinition(BufferLayout.Std430, 1, binding, $"tfe_data{i}", tfeDataStruct);
                    ResourceManager.Properties.AddOrUpdateStorageBuffer(binding, tfeDataBuffer);
                }
            }
        }

        private static bool IsLoadUserDefined(Operation operation)
        {
            // TODO: Check if sources count match and all sources are constant.
            return operation.Inst == Instruction.Load && (IoVariable)operation.GetSource(0).Value == IoVariable.UserDefined;
        }

        private static bool IsStoreUserDefined(Operation operation)
        {
            // TODO: Check if sources count match and all sources are constant.
            return operation.Inst == Instruction.Store && (IoVariable)operation.GetSource(0).Value == IoVariable.UserDefined;
        }

        private static FunctionCode[] Combine(FunctionCode[] a, FunctionCode[] b, int aStart)
        {
            // Here we combine two shaders.
            // For shader A:
            // - All user attribute stores on shader A are turned into copies to a
            // temporary variable. It's assumed that shader B will consume them.
            // - All return instructions are turned into branch instructions, the
            // branch target being the start of the shader B code.
            // For shader B:
            // - All user attribute loads on shader B are turned into copies from a
            // temporary variable, as long that attribute is written by shader A.
            FunctionCode[] output = new FunctionCode[a.Length + b.Length - 1];

            List<Operation> ops = new(a.Length + b.Length);

            Operand[] temps = new Operand[AttributeConsts.UserAttributesCount * 4];

            Operand lblB = Label();

            for (int index = aStart; index < a[0].Code.Length; index++)
            {
                Operation operation = a[0].Code[index];

                if (IsStoreUserDefined(operation))
                {
                    int tIndex = operation.GetSource(1).Value * 4 + operation.GetSource(2).Value;

                    Operand temp = temps[tIndex];

                    if (temp == null)
                    {
                        temp = Local();

                        temps[tIndex] = temp;
                    }

                    operation.Dest = temp;
                    operation.TurnIntoCopy(operation.GetSource(operation.SourcesCount - 1));
                }

                if (operation.Inst == Instruction.Return)
                {
                    ops.Add(new Operation(Instruction.Branch, lblB));
                }
                else
                {
                    ops.Add(operation);
                }
            }

            ops.Add(new Operation(Instruction.MarkLabel, lblB));

            for (int index = 0; index < b[0].Code.Length; index++)
            {
                Operation operation = b[0].Code[index];

                if (IsLoadUserDefined(operation))
                {
                    int tIndex = operation.GetSource(1).Value * 4 + operation.GetSource(2).Value;

                    Operand temp = temps[tIndex];

                    if (temp != null)
                    {
                        operation.TurnIntoCopy(temp);
                    }
                }

                ops.Add(operation);
            }

            output[0] = new FunctionCode(ops.ToArray());

            for (int i = 1; i < a.Length; i++)
            {
                output[i] = a[i];
            }

            for (int i = 1; i < b.Length; i++)
            {
                output[a.Length + i - 1] = b[i];
            }

            return output;
        }

        public int GetDepthRegister()
        {
            // The depth register is always two registers after the last color output.
            return BitOperations.PopCount((uint)Definitions.OmapTargets) + 1;
        }

        public void InheritFrom(TranslatorContext other)
        {
            ClipDistancesWritten |= other.ClipDistancesWritten;
            UsedFeatures |= other.UsedFeatures;

            AttributeUsage.InheritFrom(other.AttributeUsage);
        }

        public void SetLayerOutputAttribute(int attr)
        {
            LayerOutputWritten = true;
            LayerOutputAttribute = attr;
        }

        public void SetGeometryShaderLayerInputAttribute(int attr)
        {
            HasLayerInputAttribute = true;
            GpLayerInputAttribute = attr;
        }

        public void SetLastInVertexPipeline()
        {
            Definitions.LastInVertexPipeline = true;
        }

        public void SetNextStage(TranslatorContext nextStage)
        {
            AttributeUsage.MergeFromtNextStage(Definitions.GpPassthrough, nextStage.UsedFeatures.HasFlag(FeatureFlags.FixedFuncAttr), nextStage.AttributeUsage);

            // We don't consider geometry shaders using the geometry shader passthrough feature
            // as being the last because when this feature is used, it can't actually modify any of the outputs,
            // so the stage that comes before it is the last one that can do modifications.
            if (nextStage.Definitions.Stage != ShaderStage.Fragment && (nextStage.Definitions.Stage != ShaderStage.Geometry || !nextStage.Definitions.GpPassthrough))
            {
                Definitions.LastInVertexPipeline = false;
            }
        }

        public void MergeOutputUserAttributes(int mask, IEnumerable<int> perPatch)
        {
            AttributeUsage.MergeOutputUserAttributes(Definitions.GpPassthrough, mask, perPatch);
        }

        public void SetClipDistanceWritten(int index)
        {
            ClipDistancesWritten |= (byte)(1 << index);
        }

        public void SetUsedFeature(FeatureFlags flags)
        {
            UsedFeatures |= flags;
        }

        public ShaderProgram Translate()
        {
            bool usesLocalMemory = _program.UsedFeatures.HasFlag(FeatureFlags.LocalMemory);

            ResourceManager.SetCurrentLocalMemory(_localMemorySize, usesLocalMemory);

            if (Stage == ShaderStage.Compute)
            {
                bool usesSharedMemory = _program.UsedFeatures.HasFlag(FeatureFlags.SharedMemory);

                ResourceManager.SetCurrentSharedMemory(GpuAccessor.QueryComputeSharedMemorySize(), usesSharedMemory);
            }

            FunctionCode[] code = EmitShader(this, _program, initializeOutputs: true, out _);

            return Translator.Translate(
                code,
                AttributeUsage,
                Definitions,
                ResourceManager,
                GpuAccessor,
                Options,
                UsedFeatures,
                ClipDistancesWritten);
        }

        public ShaderProgram Translate(TranslatorContext other)
        {
            bool usesLocalMemory = _program.UsedFeatures.HasFlag(FeatureFlags.LocalMemory);

            ResourceManager.SetCurrentLocalMemory(_localMemorySize, usesLocalMemory);

            FunctionCode[] code = EmitShader(this, _program, initializeOutputs: false, out _);

            if (other != null)
            {
                other.MergeOutputUserAttributes(AttributeUsage.UsedOutputAttributes, Enumerable.Empty<int>());

                // We need to share the resource manager since both shaders accesses the same constant buffers.
                other.ResourceManager = ResourceManager;

                bool otherUsesLocalMemory = other._program.UsedFeatures.HasFlag(FeatureFlags.LocalMemory);

                ResourceManager.SetCurrentLocalMemory(other._localMemorySize, otherUsesLocalMemory);

                FunctionCode[] otherCode = EmitShader(other, other._program, initializeOutputs: true, out int aStart);

                code = Combine(otherCode, code, aStart);

                InheritFrom(other);
            }

            return Translator.Translate(
                code,
                AttributeUsage,
                Definitions,
                ResourceManager,
                GpuAccessor,
                Options,
                UsedFeatures,
                ClipDistancesWritten);
        }

        public ShaderProgram GenerateGeometryPassthrough()
        {
            int outputAttributesMask = AttributeUsage.UsedOutputAttributes;
            int layerOutputAttr = LayerOutputAttribute;

            OutputTopology outputTopology;
            int maxOutputVertices;

            switch (GpuAccessor.QueryPrimitiveTopology())
            {
                case InputTopology.Points:
                    outputTopology = OutputTopology.PointList;
                    maxOutputVertices = 1;
                    break;
                case InputTopology.Lines:
                case InputTopology.LinesAdjacency:
                    outputTopology = OutputTopology.LineStrip;
                    maxOutputVertices = 2;
                    break;
                default:
                    outputTopology = OutputTopology.TriangleStrip;
                    maxOutputVertices = 3;
                    break;
            }

            var attributeUsage = new AttributeUsage(GpuAccessor);
            var resourceManager = new ResourceManager(ShaderStage.Geometry, GpuAccessor);

            var context = new EmitterContext();

            for (int v = 0; v < maxOutputVertices; v++)
            {
                int outAttrsMask = outputAttributesMask;

                while (outAttrsMask != 0)
                {
                    int attrIndex = BitOperations.TrailingZeroCount(outAttrsMask);

                    outAttrsMask &= ~(1 << attrIndex);

                    for (int c = 0; c < 4; c++)
                    {
                        int attr = AttributeConsts.UserAttributeBase + attrIndex * 16 + c * 4;

                        Operand value = context.Load(StorageKind.Input, IoVariable.UserDefined, Const(attrIndex), Const(v), Const(c));

                        if (attr == layerOutputAttr)
                        {
                            context.Store(StorageKind.Output, IoVariable.Layer, null, value);
                        }
                        else
                        {
                            context.Store(StorageKind.Output, IoVariable.UserDefined, null, Const(attrIndex), Const(c), value);
                            attributeUsage.SetOutputUserAttribute(attrIndex);
                        }

                        attributeUsage.SetInputUserAttribute(attrIndex, c);
                    }
                }

                for (int c = 0; c < 4; c++)
                {
                    Operand value = context.Load(StorageKind.Input, IoVariable.Position, Const(v), Const(c));

                    context.Store(StorageKind.Output, IoVariable.Position, null, Const(c), value);
                }

                context.EmitVertex();
            }

            context.EndPrimitive();

            var operations = context.GetOperations();
            var cfg = ControlFlowGraph.Create(operations);
            var function = new Function(cfg.Blocks, "main", false, 0, 0);

            var definitions = new ShaderDefinitions(
                ShaderStage.Geometry,
                false,
                1,
                GpuAccessor.QueryPrimitiveTopology(),
                outputTopology,
                maxOutputVertices);

            return Translator.Generate(
                new[] { function },
                attributeUsage,
                definitions,
                resourceManager,
                GpuAccessor,
                FeatureFlags.RtLayer,
                0,
                Options);
        }
    }
}
