using ARMeilleure.Decoders;
using ARMeilleure.IntermediateRepresentation;
using ARMeilleure.State;
using System;
using System.Collections.Generic;
using System.Reflection;

using static ARMeilleure.IntermediateRepresentation.OperandHelper;

namespace ARMeilleure.Translation
{
    class EmitterContext
    {
        public Block  CurrBlock { get; set; }
        public OpCode CurrOp    { get; set; }

        public Aarch32Mode Mode { get; }

        private Dictionary<ulong, Operand> _labels;

        private Dictionary<Operand, BasicBlock> _irLabels;

        private LinkedList<BasicBlock> _irBlocks;

        private BasicBlock _irBlock;

        private bool _needsNewBlock;

        public EmitterContext()
        {
            _labels = new Dictionary<ulong, Operand>();

            _irLabels = new Dictionary<Operand, BasicBlock>();

            _irBlocks = new LinkedList<BasicBlock>();

            _needsNewBlock = true;
        }

        public Operand Add(Operand a, Operand b)
        {
            return Add(Instruction.Add, Local(a.Type), a, b);
        }

        public Operand BitwiseAnd(Operand a, Operand b)
        {
            return Add(Instruction.BitwiseAnd, Local(a.Type), a, b);
        }

        public Operand BitwiseExclusiveOr(Operand a, Operand b)
        {
            return Add(Instruction.BitwiseExclusiveOr, Local(a.Type), a, b);
        }

        public Operand BitwiseNot(Operand a)
        {
            return Add(Instruction.BitwiseNot, Local(a.Type), a);
        }

        public Operand BitwiseOr(Operand a, Operand b)
        {
            return Add(Instruction.BitwiseOr, Local(a.Type), a, b);
        }

        public void Branch(Operand label)
        {
            Add(Instruction.Branch, null);

            BranchToLabel(label);
        }

        public void BranchIfFalse(Operand label, Operand a)
        {
            Add(Instruction.BranchIfFalse, null, a);

            BranchToLabel(label);
        }

        public void BranchIfTrue(Operand label, Operand a)
        {
            Add(Instruction.BranchIfTrue, null, a);

            BranchToLabel(label);
        }

        public Operand ByteSwap(Operand a)
        {
            return Add(Instruction.ByteSwap, Local(a.Type), a);
        }

        public Operand Call(MethodInfo info, params Operand[] callArgs)
        {
            long methodPtr = info.MethodHandle.GetFunctionPointer().ToInt64();

            Operand[] args = new Operand[callArgs.Length + 1];

            args[0] = Const(methodPtr);

            Array.Copy(callArgs, 0, args, 1, callArgs.Length);

            if (info.ReturnType != typeof(void))
            {
                OperandType returnType = GetOperandType(info.ReturnType);

                return Add(Instruction.Call, Local(returnType), args);
            }
            else
            {
                return Add(Instruction.Call, null, args);
            }
        }

        private static OperandType GetOperandType(Type type)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Boolean:
                case TypeCode.Char:
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.Int16:
                case TypeCode.UInt32:
                case TypeCode.Int32:
                    return OperandType.I32;

                case TypeCode.UInt64:
                case TypeCode.Int64:
                    return OperandType.I64;

                case TypeCode.Single:
                    return OperandType.FP32;

                case TypeCode.Double:
                    return OperandType.FP64;
            }

            throw new ArgumentException($"Invalid type \"{type.Name}\".");
        }

        public Operand Call(OperandType returnType, params Operand[] callArgs)
        {
            return Add(Instruction.Call, Local(returnType), callArgs);
        }

        public Operand ConditionalSelect(Operand a, Operand b, Operand c)
        {
            return Add(Instruction.ConditionalSelect, Local(b.Type), a, b, c);
        }

        public Operand Copy(Operand a)
        {
            return Add(Instruction.Copy, Local(a.Type), a);
        }

        public Operand Copy(Operand d, Operand a)
        {
            return Add(Instruction.Copy, d, a);
        }

        public Operand CountLeadingZeros(Operand a)
        {
            return Add(Instruction.CountLeadingZeros, Local(a.Type), a);
        }

        public Operand Divide(Operand a, Operand b)
        {
            return Add(Instruction.Divide, Local(a.Type), a, b);
        }

        public Operand DivideUI(Operand a, Operand b)
        {
            return Add(Instruction.DivideUI, Local(a.Type), a, b);
        }

        public Operand ICompareEqual(Operand a, Operand b)
        {
            return Add(Instruction.CompareEqual, Local(OperandType.I32), a, b);
        }

        public Operand ICompareGreater(Operand a, Operand b)
        {
            return Add(Instruction.CompareGreater, Local(OperandType.I32), a, b);
        }

        public Operand ICompareGreaterOrEqual(Operand a, Operand b)
        {
            return Add(Instruction.CompareGreaterOrEqual, Local(OperandType.I32), a, b);
        }

        public Operand ICompareGreaterOrEqualUI(Operand a, Operand b)
        {
            return Add(Instruction.CompareGreaterOrEqualUI, Local(OperandType.I32), a, b);
        }

        public Operand ICompareGreaterUI(Operand a, Operand b)
        {
            return Add(Instruction.CompareGreaterUI, Local(OperandType.I32), a, b);
        }

        public Operand ICompareLess(Operand a, Operand b)
        {
            return Add(Instruction.CompareLess, Local(OperandType.I32), a, b);
        }

        public Operand ICompareLessOrEqual(Operand a, Operand b)
        {
            return Add(Instruction.CompareLessOrEqual, Local(OperandType.I32), a, b);
        }

        public Operand ICompareLessOrEqualUI(Operand a, Operand b)
        {
            return Add(Instruction.CompareLessOrEqualUI, Local(OperandType.I32), a, b);
        }

        public Operand ICompareLessUI(Operand a, Operand b)
        {
            return Add(Instruction.CompareLessUI, Local(OperandType.I32), a, b);
        }

        public Operand ICompareNotEqual(Operand a, Operand b)
        {
            return Add(Instruction.CompareNotEqual, Local(OperandType.I32), a, b);
        }

        public Operand Load(Operand value, Operand address)
        {
            return Add(Instruction.Load, value, address);
        }

        public Operand LoadSx16(Operand value, Operand address)
        {
            return Add(Instruction.LoadSx16, value, address);
        }

        public Operand LoadSx32(Operand value, Operand address)
        {
            return Add(Instruction.LoadSx32, value, address);
        }

        public Operand LoadSx8(Operand value, Operand address)
        {
            return Add(Instruction.LoadSx8, value, address);
        }

        public Operand LoadZx16(Operand value, Operand address)
        {
            return Add(Instruction.LoadZx16, value, address);
        }

        public Operand LoadZx8(Operand value, Operand address)
        {
            return Add(Instruction.LoadZx8, value, address);
        }

        public Operand Multiply(Operand a, Operand b)
        {
            return Add(Instruction.Multiply, Local(a.Type), a, b);
        }

        public Operand Multiply64HighSI(Operand a, Operand b)
        {
            return Add(Instruction.Multiply64HighSI, Local(OperandType.I64), a, b);
        }

        public Operand Multiply64HighUI(Operand a, Operand b)
        {
            return Add(Instruction.Multiply64HighUI, Local(OperandType.I64), a, b);
        }

        public Operand Negate(Operand a)
        {
            return Add(Instruction.Negate, Local(a.Type), a);
        }

        public Operand Return()
        {
            return Add(Instruction.Return);
        }

        public Operand Return(Operand a)
        {
            return Add(Instruction.Return, null, a);
        }

        public Operand RotateRight(Operand a, Operand b)
        {
            return Add(Instruction.RotateRight, Local(a.Type), a, b);
        }

        public Operand ShiftLeft(Operand a, Operand b)
        {
            return Add(Instruction.ShiftLeft, Local(a.Type), a, b);
        }

        public Operand ShiftRightSI(Operand a, Operand b)
        {
            return Add(Instruction.ShiftRightSI, Local(a.Type), a, b);
        }

        public Operand ShiftRightUI(Operand a, Operand b)
        {
            return Add(Instruction.ShiftRightUI, Local(a.Type), a, b);
        }

        public Operand SignExtend8(Operand a)
        {
            return Add(Instruction.SignExtend8, Local(a.Type), a);
        }

        public Operand SignExtend16(Operand a)
        {
            return Add(Instruction.SignExtend16, Local(a.Type), a);
        }

        public Operand SignExtend32(Operand a)
        {
            return Add(Instruction.SignExtend32, Local(a.Type), a);
        }

        public void Store(Operand address, Operand value)
        {
            Add(Instruction.Store, null, address, value);
        }

        public void Store16(Operand address, Operand value)
        {
            Add(Instruction.Store16, null, address, value);
        }

        public void Store8(Operand address, Operand value)
        {
            Add(Instruction.Store8, null, address, value);
        }

        public Operand Subtract(Operand a, Operand b)
        {
            return Add(Instruction.Subtract, Local(a.Type), a, b);
        }

        public Operand VectorExtract(Operand vector, Operand value, int index)
        {
            return Add(Instruction.VectorExtract, value, vector, Const(index));
        }

        public Operand VectorExtract16(Operand vector, Operand value, int index)
        {
            return Add(Instruction.VectorExtract16, value, vector, Const(index));
        }

        public Operand VectorExtract8(Operand vector, Operand value, int index)
        {
            return Add(Instruction.VectorExtract8, value, vector, Const(index));
        }

        public Operand VectorInsert(Operand vector, Operand value, int index)
        {
            return Add(Instruction.VectorInsert, Local(OperandType.V128), vector, value, Const(index));
        }

        public Operand VectorInsert16(Operand vector, Operand value, int index)
        {
            return Add(Instruction.VectorInsert16, Local(OperandType.V128), vector, value, Const(index));
        }

        public Operand VectorInsert8(Operand vector, Operand value, int index)
        {
            return Add(Instruction.VectorInsert8, Local(OperandType.V128), vector, value, Const(index));
        }

        public Operand VectorZero()
        {
            return Add(Instruction.VectorZero, Local(OperandType.V128));
        }

        public Operand VectorZeroUpper64(Operand vector)
        {
            return Add(Instruction.VectorZeroUpper64, Local(OperandType.V128), vector);
        }

        public Operand VectorZeroUpper96(Operand vector)
        {
            return Add(Instruction.VectorZeroUpper96, Local(OperandType.V128), vector);
        }

        public Operand AddIntrinsic(Instruction inst, params Operand[] args)
        {
            return Add(inst, Local(OperandType.V128), args);
        }

        private Operand Add(Instruction inst, Operand dest = null, params Operand[] sources)
        {
            if (_needsNewBlock)
            {
                NewNextBlock();
            }

            Operation operation = new Operation(inst, dest, sources);

            _irBlock.Operations.AddLast(operation);

            return dest;
        }

        private void BranchToLabel(Operand label)
        {
            if (!_irLabels.TryGetValue(label, out BasicBlock branchBlock))
            {
                branchBlock = new BasicBlock();

                _irLabels.Add(label, branchBlock);
            }

            _irBlock.Branch = branchBlock;

            _needsNewBlock = true;
        }

        public void Synchronize()
        {

        }

        public void MarkLabel(Operand label)
        {
            if (_irLabels.TryGetValue(label, out BasicBlock nextBlock))
            {
                nextBlock.Index = _irBlocks.Count;
                nextBlock.Node  = _irBlocks.AddLast(nextBlock);

                NextBlock(nextBlock);
            }
            else
            {
                NewNextBlock();

                _irLabels.Add(label, _irBlock);
            }
        }

        public Operand GetLabel(ulong address)
        {
            if (!_labels.TryGetValue(address, out Operand label))
            {
                label = Label();

                _labels.Add(address, label);
            }

            return label;
        }

        private void NewNextBlock()
        {
            BasicBlock block = new BasicBlock(_irBlocks.Count);

            block.Node = _irBlocks.AddLast(block);

            NextBlock(block);
        }

        private void NextBlock(BasicBlock nextBlock)
        {
            if (_irBlock != null && !EndsWithUnconditional(_irBlock))
            {
                _irBlock.Next = nextBlock;
            }

            _irBlock = nextBlock;

            _needsNewBlock = false;
        }

        private static bool EndsWithUnconditional(BasicBlock block)
        {
            Operation lastOp = block.GetLastOp() as Operation;

            if (lastOp == null)
            {
                return false;
            }

            return lastOp.Inst == Instruction.Branch ||
                   lastOp.Inst == Instruction.Return;
        }

        public ControlFlowGraph GetControlFlowGraph()
        {
            return new ControlFlowGraph(_irBlocks.First.Value, _irBlocks);
        }
    }
}