using Ryujinx.Graphics.Shader.StructuredIr;
using Ryujinx.Graphics.Shader.Translation;
using Spv.Generator;
using System;
using System.Collections.Generic;
using System.Linq;
using static Spv.Specification;

namespace Ryujinx.Graphics.Shader.CodeGen.Spirv
{
    using IrConsts = IntermediateRepresentation.IrConsts;
    using IrOperandType = IntermediateRepresentation.OperandType;

    partial class CodeGenContext : Module
    {
        private readonly StructuredProgramInfo _info;

        public ShaderConfig Config { get; }

        public int InputVertices { get; }

        public Dictionary<int, Instruction> UniformBuffers { get; } = new Dictionary<int, Instruction>();
        public Instruction UniformBuffersArray { get; set; }
        public Instruction StorageBuffersArray { get; set; }
        public Instruction LocalMemory { get; set; }
        public Instruction SharedMemory { get; set; }
        public Instruction InputsArray { get; set; }
        public Instruction OutputsArray { get; set; }
        public Dictionary<TextureMeta, SamplerType> SamplersTypes { get; } = new Dictionary<TextureMeta, SamplerType>();
        public Dictionary<TextureMeta, (Instruction, Instruction, Instruction)> Samplers { get; } = new Dictionary<TextureMeta, (Instruction, Instruction, Instruction)>();
        public Dictionary<TextureMeta, (Instruction, Instruction)> Images { get; } = new Dictionary<TextureMeta, (Instruction, Instruction)>();
        public Dictionary<int, Instruction> Inputs { get; } = new Dictionary<int, Instruction>();
        public Dictionary<int, Instruction> Outputs { get; } = new Dictionary<int, Instruction>();

        private readonly Dictionary<AstOperand, Instruction> _locals = new Dictionary<AstOperand, Instruction>();
        private readonly Dictionary<int, Instruction[]> _localForArgs = new Dictionary<int, Instruction[]>();
        private readonly Dictionary<int, Instruction> _funcArgs = new Dictionary<int, Instruction>();
        private readonly Dictionary<int, (StructuredFunction, Instruction)> _functions = new Dictionary<int, (StructuredFunction, Instruction)>();

        private class BlockState
        {
            private int _entryCount;
            private readonly List<Instruction> _labels = new List<Instruction>();

            public Instruction GetNextLabel(CodeGenContext context)
            {
                return GetLabel(context, _entryCount);
            }

            public Instruction GetNextLabelAutoIncrement(CodeGenContext context)
            {
                return GetLabel(context, _entryCount++);
            }

            public Instruction GetLabel(CodeGenContext context, int index)
            {
                while (index >= _labels.Count)
                {
                    _labels.Add(context.Label());
                }

                return _labels[index];
            }
        }

        private readonly Dictionary<AstBlock, BlockState> _labels = new Dictionary<AstBlock, BlockState>();

        public Dictionary<AstBlock, (Instruction, Instruction)> LoopTargets { get; set; }

        public AstBlock CurrentBlock { get; private set; }

        public SpirvDelegates Delegates { get; }

        public CodeGenContext(
            StructuredProgramInfo info,
            ShaderConfig config,
            GeneratorPool<Instruction> instPool,
            GeneratorPool<LiteralInteger> integerPool) : base(0x00010300, instPool, integerPool)
        {
            _info = info;
            Config = config;

            if (config.Stage == ShaderStage.Geometry)
            {
                InputTopology inPrimitive = config.GpuAccessor.QueryPrimitiveTopology();

                InputVertices = inPrimitive switch
                {
                    InputTopology.Points => 1,
                    InputTopology.Lines => 2,
                    InputTopology.LinesAdjacency => 2,
                    InputTopology.Triangles => 3,
                    InputTopology.TrianglesAdjacency => 3,
                    _ => throw new InvalidOperationException($"Invalid input topology \"{inPrimitive}\".")
                };
            }

            AddCapability(Capability.Shader);
            AddCapability(Capability.Float64);

            SetMemoryModel(AddressingModel.Logical, MemoryModel.GLSL450);

            Delegates = new SpirvDelegates(this);
        }

        public void StartFunction()
        {
            _locals.Clear();
            _localForArgs.Clear();
            _funcArgs.Clear();
        }

        public void EnterBlock(AstBlock block)
        {
            CurrentBlock = block;
            AddLabel(GetBlockStateLazy(block).GetNextLabelAutoIncrement(this));
        }

        public Instruction GetFirstLabel(AstBlock block)
        {
            return GetBlockStateLazy(block).GetLabel(this, 0);
        }

        public Instruction GetNextLabel(AstBlock block)
        {
            return GetBlockStateLazy(block).GetNextLabel(this);
        }

        private BlockState GetBlockStateLazy(AstBlock block)
        {
            if (!_labels.TryGetValue(block, out var blockState))
            {
                blockState = new BlockState();

                _labels.Add(block, blockState);
            }

            return blockState;
        }

        public Instruction NewBlock()
        {
            var label = Label();
            Branch(label);
            AddLabel(label);
            return label;
        }

        public Instruction[] GetMainInterface()
        {
            var mainInterface = Inputs.Values.Concat(Outputs.Values);

            if (InputsArray != null)
            {
                mainInterface = mainInterface.Append(InputsArray);
            }

            if (OutputsArray != null)
            {
                mainInterface = mainInterface.Append(OutputsArray);
            }

            return mainInterface.ToArray();
        }

        public void DeclareLocal(AstOperand local, Instruction spvLocal)
        {
            _locals.Add(local, spvLocal);
        }

        public void DeclareLocalForArgs(int funcIndex, Instruction[] spvLocals)
        {
            _localForArgs.Add(funcIndex, spvLocals);
        }

        public void DeclareArgument(int argIndex, Instruction spvLocal)
        {
            _funcArgs.Add(argIndex, spvLocal);
        }

        public void DeclareFunction(int funcIndex, StructuredFunction function, Instruction spvFunc)
        {
            _functions.Add(funcIndex, (function, spvFunc));
        }

        public Instruction GetFP32(IAstNode node)
        {
            return Get(AggregateType.FP32, node);
        }

        public Instruction GetFP64(IAstNode node)
        {
            return Get(AggregateType.FP64, node);
        }

        public Instruction GetS32(IAstNode node)
        {
            return Get(AggregateType.S32, node);
        }

        public Instruction GetU32(IAstNode node)
        {
            return Get(AggregateType.U32, node);
        }

        public Instruction Get(AggregateType type, IAstNode node)
        {
            if (node is AstOperation operation)
            {
                var opResult = Instructions.Generate(this, operation);
                return BitcastIfNeeded(type, opResult.Type, opResult.Value);
            }
            else if (node is AstOperand operand)
            {
                return operand.Type switch
                {
                    IrOperandType.Argument => GetArgument(type, operand),
                    IrOperandType.Attribute => GetAttribute(type, operand.Value & AttributeConsts.Mask, (operand.Value & AttributeConsts.LoadOutputMask) != 0),
                    IrOperandType.Constant => GetConstant(type, operand),
                    IrOperandType.ConstantBuffer => GetConstantBuffer(type, operand),
                    IrOperandType.LocalVariable => GetLocal(type, operand),
                    IrOperandType.Undefined => Undef(GetType(type)),
                    _ => throw new ArgumentException($"Invalid operand type \"{operand.Type}\".")
                };
            }

            throw new NotImplementedException(node.GetType().Name);
        }

        public Instruction GetAttributeElemPointer(int attr, bool isOutAttr, Instruction index, out AggregateType elemType)
        {
            var storageClass = isOutAttr ? StorageClass.Output : StorageClass.Input;
            var attrInfo = AttributeInfo.From(Config, attr, isOutAttr);

            int attrOffset = attrInfo.BaseValue;
            AggregateType type = attrInfo.Type;

            Instruction ioVariable, elemIndex;

            bool isUserAttr = attr >= AttributeConsts.UserAttributeBase && attr < AttributeConsts.UserAttributeEnd;

            if (isUserAttr &&
                ((!isOutAttr && Config.UsedFeatures.HasFlag(FeatureFlags.IaIndexing)) ||
                (isOutAttr && Config.UsedFeatures.HasFlag(FeatureFlags.OaIndexing))))
            {
                elemType = AggregateType.FP32;
                ioVariable = isOutAttr ? OutputsArray : InputsArray;
                elemIndex = Constant(TypeU32(), attrInfo.GetInnermostIndex());
                var vecIndex = Constant(TypeU32(), (attr - AttributeConsts.UserAttributeBase) >> 4);

                if (Config.Stage == ShaderStage.Geometry && !isOutAttr)
                {
                    return AccessChain(TypePointer(storageClass, GetType(elemType)), ioVariable, index, vecIndex, elemIndex);
                }
                else
                {
                    return AccessChain(TypePointer(storageClass, GetType(elemType)), ioVariable, vecIndex, elemIndex);
                }
            }

            elemType = attrInfo.Type & AggregateType.ElementTypeMask;

            if (isUserAttr && Config.TransformFeedbackEnabled &&
                ((isOutAttr && Config.Stage != ShaderStage.Fragment) ||
                (!isOutAttr && Config.Stage != ShaderStage.Vertex)))
            {
                attrOffset = attr;
                type = elemType;
            }

            ioVariable = isOutAttr ? Outputs[attrOffset] : Inputs[attrOffset];

            if ((type & (AggregateType.Array | AggregateType.Vector)) == 0)
            {
                return ioVariable;
            }

            elemIndex = Constant(TypeU32(), attrInfo.GetInnermostIndex());

            if (Config.Stage == ShaderStage.Geometry && !isOutAttr && (!attrInfo.IsBuiltin || AttributeInfo.IsArrayBuiltIn(attr)))
            {
                return AccessChain(TypePointer(storageClass, GetType(elemType)), ioVariable, index, elemIndex);
            }
            else
            {
                return AccessChain(TypePointer(storageClass, GetType(elemType)), ioVariable, elemIndex);
            }
        }

        public Instruction GetAttributeElemPointer(Instruction attrIndex, bool isOutAttr, Instruction index, out AggregateType elemType)
        {
            var storageClass = isOutAttr ? StorageClass.Output : StorageClass.Input;

            elemType = AggregateType.FP32;
            var ioVariable = isOutAttr ? OutputsArray : InputsArray;
            var vecIndex = ShiftRightLogical(TypeS32(), attrIndex, Constant(TypeS32(), 2));
            var elemIndex = BitwiseAnd(TypeS32(), attrIndex, Constant(TypeS32(), 3));

            if (Config.Stage == ShaderStage.Geometry && !isOutAttr)
            {
                return AccessChain(TypePointer(storageClass, GetType(elemType)), ioVariable, index, vecIndex, elemIndex);
            }
            else
            {
                return AccessChain(TypePointer(storageClass, GetType(elemType)), ioVariable, vecIndex, elemIndex);
            }
        }

        public Instruction GetAttribute(AggregateType type, int attr, bool isOutAttr, Instruction index = null)
        {
            var elemPointer = GetAttributeElemPointer(attr, isOutAttr, index, out var elemType);
            return BitcastIfNeeded(type, elemType, Load(GetType(elemType), elemPointer));
        }

        public Instruction GetAttribute(AggregateType type, Instruction attr, bool isOutAttr, Instruction index = null)
        {
            var elemPointer = GetAttributeElemPointer(attr, isOutAttr, index, out var elemType);
            return BitcastIfNeeded(type, elemType, Load(GetType(elemType), elemPointer));
        }

        public Instruction GetConstant(AggregateType type, AstOperand operand)
        {
            return type switch
            {
                AggregateType.Bool => operand.Value != 0 ? ConstantTrue(TypeBool()) : ConstantFalse(TypeBool()),
                AggregateType.FP32 => Constant(TypeFP32(), BitConverter.Int32BitsToSingle(operand.Value)),
                AggregateType.FP64 => Constant(TypeFP64(), (double)BitConverter.Int32BitsToSingle(operand.Value)),
                AggregateType.S32 => Constant(TypeS32(), operand.Value),
                AggregateType.U32 => Constant(TypeU32(), (uint)operand.Value),
                _ => throw new ArgumentException($"Invalid type \"{type}\".")
            };
        }

        public Instruction GetConstantBuffer(AggregateType type, AstOperand operand)
        {
            var i1 = Constant(TypeS32(), 0);
            var i2 = Constant(TypeS32(), operand.CbufOffset >> 2);
            var i3 = Constant(TypeU32(), operand.CbufOffset & 3);

            Instruction elemPointer;

            if (UniformBuffersArray != null)
            {
                var ubVariable = UniformBuffersArray;
                var i0 = Constant(TypeS32(), operand.CbufSlot);

                elemPointer = AccessChain(TypePointer(StorageClass.Uniform, TypeFP32()), ubVariable, i0, i1, i2, i3);
            }
            else
            {
                var ubVariable = UniformBuffers[operand.CbufSlot];

                elemPointer = AccessChain(TypePointer(StorageClass.Uniform, TypeFP32()), ubVariable, i1, i2, i3);
            }

            return BitcastIfNeeded(type, AggregateType.FP32, Load(TypeFP32(), elemPointer));
        }

        public Instruction GetLocalPointer(AstOperand local)
        {
            return _locals[local];
        }

        public Instruction[] GetLocalForArgsPointers(int funcIndex)
        {
            return _localForArgs[funcIndex];
        }

        public Instruction GetArgumentPointer(AstOperand funcArg)
        {
            return _funcArgs[funcArg.Value];
        }

        public Instruction GetLocal(AggregateType dstType, AstOperand local)
        {
            var srcType = local.VarType.Convert();
            return BitcastIfNeeded(dstType, srcType, Load(GetType(srcType), GetLocalPointer(local)));
        }

        public Instruction GetArgument(AggregateType dstType, AstOperand funcArg)
        {
            var srcType = funcArg.VarType.Convert();
            return BitcastIfNeeded(dstType, srcType, Load(GetType(srcType), GetArgumentPointer(funcArg)));
        }

        public (StructuredFunction, Instruction) GetFunction(int funcIndex)
        {
            return _functions[funcIndex];
        }

        public TransformFeedbackOutput GetTransformFeedbackOutput(int location, int component)
        {
            int index = (AttributeConsts.UserAttributeBase / 4) + location * 4 + component;
            return _info.TransformFeedbackOutputs[index];
        }

        public TransformFeedbackOutput GetTransformFeedbackOutput(int location)
        {
            int index = location / 4;
            return _info.TransformFeedbackOutputs[index];
        }

        public Instruction GetType(AggregateType type, int length = 1)
        {
            if (type.HasFlag(AggregateType.Array))
            {
                return TypeArray(GetType(type & ~AggregateType.Array), Constant(TypeU32(), length));
            }
            else if (type.HasFlag(AggregateType.Vector))
            {
                return TypeVector(GetType(type & ~AggregateType.Vector), length);
            }

            return type switch
            {
                AggregateType.Void => TypeVoid(),
                AggregateType.Bool => TypeBool(),
                AggregateType.FP32 => TypeFP32(),
                AggregateType.FP64 => TypeFP64(),
                AggregateType.S32 => TypeS32(),
                AggregateType.U32 => TypeU32(),
                _ => throw new ArgumentException($"Invalid attribute type \"{type}\".")
            };
        }

        public Instruction BitcastIfNeeded(AggregateType dstType, AggregateType srcType, Instruction value)
        {
            if (dstType == srcType)
            {
                return value;
            }

            if (dstType == AggregateType.Bool)
            {
                return INotEqual(TypeBool(), BitcastIfNeeded(AggregateType.S32, srcType, value), Constant(TypeS32(), 0));
            }
            else if (srcType == AggregateType.Bool)
            {
                var intTrue  = Constant(TypeS32(), IrConsts.True);
                var intFalse = Constant(TypeS32(), IrConsts.False);

                return BitcastIfNeeded(dstType, AggregateType.S32, Select(TypeS32(), value, intTrue, intFalse));
            }
            else
            {
                return Bitcast(GetType(dstType, 1), value);
            }
        }

        public Instruction TypeS32()
        {
            return TypeInt(32, true);
        }

        public Instruction TypeU32()
        {
            return TypeInt(32, false);
        }

        public Instruction TypeFP32()
        {
            return TypeFloat(32);
        }

        public Instruction TypeFP64()
        {
            return TypeFloat(64);
        }
    }
}
