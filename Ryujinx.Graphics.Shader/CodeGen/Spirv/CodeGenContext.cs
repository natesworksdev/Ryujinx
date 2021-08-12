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
        public ShaderConfig Config { get; }

        public Instruction ExtSet { get; }

        public Dictionary<int, Instruction> UniformBuffers { get; } = new Dictionary<int, Instruction>();
        public Instruction StorageBuffersArray { get; set; }
        public Instruction LocalMemory { get; set; }
        public Instruction SharedMemory { get; set; }
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

        public AstBlock CurrentBlock { get; private set; }

        public CodeGenContext(ShaderConfig config) : base(0x00010300)
        {
            Config = config;

            AddCapability(Capability.Shader);
            AddCapability(Capability.Float64);

            ExtSet = AddExtInstImport("GLSL.std.450");

            SetMemoryModel(AddressingModel.Logical, MemoryModel.GLSL450);
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
            return Inputs.Values.Concat(Outputs.Values).ToArray();
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
                    IrOperandType.Attribute => GetAttribute(type, operand, false),
                    IrOperandType.Constant => GetConstant(type, operand),
                    IrOperandType.ConstantBuffer => GetConstantBuffer(type, operand),
                    IrOperandType.LocalVariable => GetLocal(type, operand),
                    IrOperandType.Undefined => Undef(GetType(type)),
                    _ => throw new ArgumentException($"Invalid operand type \"{operand.Type}\".")
                };
            }

            throw new NotImplementedException(node.GetType().Name);
        }

        public Instruction GetAttributeVectorPointer(AstOperand operand, bool isOutAttr)
        {
            var attrInfo = AttributeInfo.From(Config, operand.Value);

            return isOutAttr ? Outputs[attrInfo.BaseValue] : Inputs[attrInfo.BaseValue];
        }

        public Instruction GetAttributeElemPointer(AstOperand operand, bool isOutAttr, out AggregateType elemType)
        {
            var attrInfo = AttributeInfo.From(Config, operand.Value);
            if (attrInfo.BaseValue == AttributeConsts.PositionX && Config.Stage != ShaderStage.Fragment)
            {
                isOutAttr = true;
            }

            elemType = attrInfo.Type & AggregateType.ElementTypeMask;

            var ioVariable = isOutAttr ? Outputs[attrInfo.BaseValue] : Inputs[attrInfo.BaseValue];

            if ((attrInfo.Type & (AggregateType.Array | AggregateType.Vector)) == 0)
            {
                return ioVariable;
            }

            var storageClass = isOutAttr ? StorageClass.Output : StorageClass.Input;

            var elemIndex = Constant(TypeU32(), attrInfo.GetInnermostIndex());
            return AccessChain(TypePointer(storageClass, GetType(elemType)), ioVariable, elemIndex);
        }

        public Instruction GetAttribute(AggregateType type, AstOperand operand, bool isOutAttr)
        {
            var elemPointer = GetAttributeElemPointer(operand, isOutAttr, out var elemType);
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
            var ubVariable = UniformBuffers[operand.CbufSlot];
            var i0 = Constant(TypeS32(), 0);
            var i1 = Constant(TypeS32(), operand.CbufOffset >> 2);
            var i2 = Constant(TypeU32(), operand.CbufOffset & 3);

            var elemPointer = AccessChain(TypePointer(StorageClass.Uniform, TypeFP32()), ubVariable, i0, i1, i2);
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

        protected override void Construct()
        {
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
