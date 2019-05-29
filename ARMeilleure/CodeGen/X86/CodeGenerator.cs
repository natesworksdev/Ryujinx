using ARMeilleure.CodeGen.RegisterAllocators;
using ARMeilleure.Common;
using ARMeilleure.IntermediateRepresentation;
using ARMeilleure.Memory;
using ARMeilleure.State;
using ARMeilleure.Translation;
using System;
using System.IO;

namespace ARMeilleure.CodeGen.X86
{
    static class CodeGenerator
    {
        private static Action<CodeGenContext, Operation>[] _instTable;

        static CodeGenerator()
        {
            _instTable = new Action<CodeGenContext, Operation>[(int)Instruction.Count];

            Add(Instruction.Add,                     GenerateAdd);
            Add(Instruction.BitwiseAnd,              GenerateBitwiseAnd);
            Add(Instruction.BitwiseExclusiveOr,      GenerateBitwiseExclusiveOr);
            Add(Instruction.BitwiseNot,              GenerateBitwiseNot);
            Add(Instruction.BitwiseOr,               GenerateBitwiseOr);
            Add(Instruction.Branch,                  GenerateBranch);
            Add(Instruction.BranchIfFalse,           GenerateBranchIfFalse);
            Add(Instruction.BranchIfTrue,            GenerateBranchIfTrue);
            Add(Instruction.ByteSwap,                GenerateByteSwap);
            Add(Instruction.CompareEqual,            GenerateCompareEqual);
            Add(Instruction.CompareGreater,          GenerateCompareGreater);
            Add(Instruction.CompareGreaterOrEqual,   GenerateCompareGreaterOrEqual);
            Add(Instruction.CompareGreaterOrEqualUI, GenerateCompareGreaterOrEqualUI);
            Add(Instruction.CompareGreaterUI,        GenerateCompareGreaterUI);
            Add(Instruction.CompareLess,             GenerateCompareLess);
            Add(Instruction.CompareLessOrEqual,      GenerateCompareLessOrEqual);
            Add(Instruction.CompareLessOrEqualUI,    GenerateCompareLessOrEqualUI);
            Add(Instruction.CompareLessUI,           GenerateCompareLessUI);
            Add(Instruction.CompareNotEqual,         GenerateCompareNotEqual);
            Add(Instruction.ConditionalSelect,       GenerateConditionalSelect);
            Add(Instruction.Copy,                    GenerateCopy);
            Add(Instruction.CountLeadingZeros,       GenerateCountLeadingZeros);
            Add(Instruction.Divide,                  GenerateDivide);
            Add(Instruction.Fill,                    GenerateFill);
            Add(Instruction.Load,                    GenerateLoad);
            Add(Instruction.LoadFromContext,         GenerateLoadFromContext);
            Add(Instruction.LoadSx16,                GenerateLoadSx16);
            Add(Instruction.LoadSx32,                GenerateLoadSx32);
            Add(Instruction.LoadSx8,                 GenerateLoadSx8);
            Add(Instruction.LoadZx16,                GenerateLoadZx16);
            Add(Instruction.LoadZx8,                 GenerateLoadZx8);
            Add(Instruction.Multiply,                GenerateMultiply);
            Add(Instruction.Negate,                  GenerateNegate);
            Add(Instruction.Return,                  GenerateReturn);
            Add(Instruction.RotateRight,             GenerateRotateRight);
            Add(Instruction.ShiftLeft,               GenerateShiftLeft);
            Add(Instruction.ShiftRightSI,            GenerateShiftRightSI);
            Add(Instruction.ShiftRightUI,            GenerateShiftRightUI);
            Add(Instruction.SignExtend16,            GenerateSignExtend16);
            Add(Instruction.SignExtend32,            GenerateSignExtend32);
            Add(Instruction.SignExtend8,             GenerateSignExtend8);
            Add(Instruction.Spill,                   GenerateSpill);
            Add(Instruction.Store,                   GenerateStore);
            Add(Instruction.Store16,                 GenerateStore16);
            Add(Instruction.Store8,                  GenerateStore8);
            Add(Instruction.StoreToContext,          GenerateStoreToContext);
            Add(Instruction.Subtract,                GenerateSubtract);
        }

        private static void Add(Instruction inst, Action<CodeGenContext, Operation> func)
        {
            _instTable[(int)inst] = func;
        }

        public static byte[] Generate(ControlFlowGraph cfg, MemoryManager memory)
        {
            PreAllocator.RunPass(cfg, memory);

            LinearScan regAlloc = new LinearScan();

            RegisterMasks regMasks = new RegisterMasks(
                CallingConvention.GetIntAvailableRegisters(),
                CallingConvention.GetIntCalleeSavedRegisters());

            RAReport raReport = regAlloc.RunPass(cfg, regMasks);

            using (MemoryStream stream = new MemoryStream())
            {
                CodeGenContext context = new CodeGenContext(stream, raReport, cfg.Blocks.Count);

                WritePrologue(context);

                context.Assembler.Mov(Register(X86Register.Rbp), Register(X86Register.Rcx));

                foreach (BasicBlock block in cfg.Blocks)
                {
                    context.EnterBlock(block);

                    foreach (Node node in block.Operations)
                    {
                        if (node is Operation operation)
                        {
                            GenerateOperation(context, operation);
                        }
                    }
                }

                return context.GetCode();
            }
        }

        private static void GenerateOperation(CodeGenContext context, Operation operation)
        {
            Action<CodeGenContext, Operation> func = _instTable[(int)operation.Inst];

            if (func != null)
            {
                func(context, operation);
            }
            else
            {
                throw new ArgumentException($"Invalid operation instruction \"{operation.Inst}\".");
            }
        }

        private static void GenerateAdd(CodeGenContext context, Operation operation)
        {
            ValidateDestSrc1(operation);

            context.Assembler.Add(operation.Dest, operation.GetSource(1));
        }

        private static void GenerateBitwiseAnd(CodeGenContext context, Operation operation)
        {
            ValidateDestSrc1(operation);

            context.Assembler.And(operation.Dest, operation.GetSource(1));
        }

        private static void GenerateBitwiseExclusiveOr(CodeGenContext context, Operation operation)
        {
            ValidateDestSrc1(operation);

            context.Assembler.Xor(operation.Dest, operation.GetSource(1));
        }

        private static void GenerateBitwiseNot(CodeGenContext context, Operation operation)
        {
            context.Assembler.Not(operation.Dest);
        }

        private static void GenerateBitwiseOr(CodeGenContext context, Operation operation)
        {
            ValidateDestSrc1(operation);

            context.Assembler.Or(operation.Dest, operation.GetSource(1));
        }

        private static void GenerateBranch(CodeGenContext context, Operation operation)
        {
            context.JumpTo(context.CurrBlock.Branch);
        }

        private static void GenerateBranchIfFalse(CodeGenContext context, Operation operation)
        {
            context.Assembler.Test(operation.GetSource(0), operation.GetSource(0));

            context.JumpTo(X86Condition.Equal, context.CurrBlock.Branch);
        }

        private static void GenerateBranchIfTrue(CodeGenContext context, Operation operation)
        {
            context.Assembler.Test(operation.GetSource(0), operation.GetSource(0));

            context.JumpTo(X86Condition.NotEqual, context.CurrBlock.Branch);
        }

        private static void GenerateByteSwap(CodeGenContext context, Operation operation)
        {
            context.Assembler.Bswap(operation.Dest);
        }

        private static void GenerateCompareEqual(CodeGenContext context, Operation operation)
        {
            GenerateCompare(context, operation, X86Condition.Equal);
        }

        private static void GenerateCompareGreater(CodeGenContext context, Operation operation)
        {
            GenerateCompare(context, operation, X86Condition.Greater);
        }

        private static void GenerateCompareGreaterOrEqual(CodeGenContext context, Operation operation)
        {
            GenerateCompare(context, operation, X86Condition.GreaterOrEqual);
        }

        private static void GenerateCompareGreaterOrEqualUI(CodeGenContext context, Operation operation)
        {
            GenerateCompare(context, operation, X86Condition.AboveOrEqual);
        }

        private static void GenerateCompareGreaterUI(CodeGenContext context, Operation operation)
        {
            GenerateCompare(context, operation, X86Condition.Above);
        }

        private static void GenerateCompareLess(CodeGenContext context, Operation operation)
        {
            GenerateCompare(context, operation, X86Condition.Less);
        }

        private static void GenerateCompareLessOrEqual(CodeGenContext context, Operation operation)
        {
            GenerateCompare(context, operation, X86Condition.LessOrEqual);
        }

        private static void GenerateCompareLessOrEqualUI(CodeGenContext context, Operation operation)
        {
            GenerateCompare(context, operation, X86Condition.BelowOrEqual);
        }

        private static void GenerateCompareLessUI(CodeGenContext context, Operation operation)
        {
            GenerateCompare(context, operation, X86Condition.Below);
        }

        private static void GenerateCompareNotEqual(CodeGenContext context, Operation operation)
        {
            GenerateCompare(context, operation, X86Condition.NotEqual);
        }

        private static void GenerateConditionalSelect(CodeGenContext context, Operation operation)
        {
            context.Assembler.Test(operation.GetSource(0), operation.GetSource(0));
            context.Assembler.Cmovcc(operation.Dest, operation.GetSource(1), X86Condition.NotEqual);
        }

        private static void GenerateCopy(CodeGenContext context, Operation operation)
        {
            Operand dest   = operation.Dest;
            Operand source = operation.GetSource(0);

            //Moves to the same register/memory location with the same type
            //are useless, we don't need to emit any code in this case.
            if (dest.Kind  == source.Kind &&
                dest.Type  == source.Type &&
                dest.Value == source.Value)
            {
                return;
            }

            if (dest.Kind   == OperandKind.Register &&
                source.Kind == OperandKind.Constant && source.Value == 0)
            {
                //Assemble "mov reg, 0" as "xor reg, reg" as the later is more efficient.
                dest = Get32BitsRegister(dest.GetRegister());

                context.Assembler.Xor(dest, dest);
            }
            else if (dest.Type == OperandType.I64 && source.Type == OperandType.I32)
            {
                //I32 -> I64 zero-extension.
                if (dest.Kind == OperandKind.Register && source.Kind == OperandKind.Register)
                {
                    dest = Get32BitsRegister(dest.GetRegister());
                }
                else if (source.Kind == OperandKind.Constant)
                {
                    source = new Operand(source.Value);
                }

                context.Assembler.Mov(dest, source);
            }
            else
            {
                context.Assembler.Mov(dest, source);
            }
        }

        private static void GenerateCountLeadingZeros(CodeGenContext context, Operation operation)
        {
            Operand dest = operation.Dest;

            Operand dest32 = Get32BitsRegister(dest.GetRegister());

            context.Assembler.Bsr(dest, operation.GetSource(0));

            int operandSize = dest.Type == OperandType.I32 ? 32 : 64;
            int operandMask = operandSize - 1;

            //When the input operand is 0, the result is undefined, however the
            //ZF flag is set. We are supposed to return the operand size on that
            //case. So, add an additional jump to handle that case, by moving the
            //operand size constant to the destination register.
            context.JumpToNear(X86Condition.NotEqual);

            context.Assembler.Mov(dest32, new Operand(operandSize | operandMask));

            context.JumpHere();

            //BSR returns the zero based index of the last bit set on the operand,
            //starting from the least significant bit. However we are supposed to
            //return the number of 0 bits on the high end. So, we invert the result
            //of the BSR using XOR to get the correct value.
            context.Assembler.Xor(dest32, new Operand(operandMask));
        }

        private static void GenerateDivide(CodeGenContext context, Operation operation)
        {
            Operand divisor = operation.GetSource(1);

            if (divisor.Type == OperandType.I32)
            {
                context.Assembler.Cdq();
            }
            else
            {
                context.Assembler.Cqo();
            }

            context.Assembler.Idiv(divisor);
        }

        private static void GenerateFill(CodeGenContext context, Operation operation)
        {
            Operand dest   = operation.Dest;
            Operand offset = operation.GetSource(0);

            if (offset.Kind != OperandKind.Constant)
            {
                throw new InvalidOperationException("Fill has non-constant stack offset.");
            }

            X86MemoryOperand memOp = new X86MemoryOperand(dest.Type, Register(X86Register.Rsp), null, Scale.x1, offset.AsInt32());

            context.Assembler.Mov(dest, memOp);
        }

        private static void GenerateLoad(CodeGenContext context, Operation operation)
        {
            context.Assembler.Mov(operation.Dest, operation.GetSource(0));
        }

        private static void GenerateLoadFromContext(CodeGenContext context, Operation operation)
        {
            Operand dest   = operation.Dest;
            Operand offset = operation.GetSource(0);

            if (offset.Kind != OperandKind.Constant)
            {
                throw new InvalidOperationException("LoadFromContext has non-constant context offset.");
            }

            Operand rbp = Register(X86Register.Rbp);

            X86MemoryOperand memOp = new X86MemoryOperand(dest.Type, rbp, null, Scale.x1, offset.AsInt32());

            context.Assembler.Mov(dest, memOp);
        }

        private static void GenerateLoadSx16(CodeGenContext context, Operation operation)
        {
            context.Assembler.Movsx16(operation.Dest, operation.GetSource(0));
        }

        private static void GenerateLoadSx32(CodeGenContext context, Operation operation)
        {
            context.Assembler.Movsx32(operation.Dest, operation.GetSource(0));
        }

        private static void GenerateLoadSx8(CodeGenContext context, Operation operation)
        {
            context.Assembler.Movsx8(operation.Dest, operation.GetSource(0));
        }

        private static void GenerateLoadZx16(CodeGenContext context, Operation operation)
        {
            context.Assembler.Movzx16(operation.Dest, operation.GetSource(0));
        }

        private static void GenerateLoadZx8(CodeGenContext context, Operation operation)
        {
            context.Assembler.Movzx8(operation.Dest, operation.GetSource(0));
        }

        private static void GenerateMultiply(CodeGenContext context, Operation operation)
        {
            Operand dest = operation.Dest;
            Operand src1 = operation.GetSource(0);
            Operand src2 = operation.GetSource(1);

            if (src2.Kind == OperandKind.Constant)
            {
                context.Assembler.Imul(dest, src1, src2);
            }
            else
            {
                context.Assembler.Imul(dest, src2);
            }
        }

        private static void GenerateNegate(CodeGenContext context, Operation operation)
        {
            context.Assembler.Neg(operation.Dest);
        }

        private static void GenerateReturn(CodeGenContext context, Operation operation)
        {
            if (operation.SourcesCount != 0)
            {
                Operand returnReg = Register(CallingConvention.GetIntReturnRegister());

                Operand sourceReg = operation.GetSource(0);

                if (returnReg.GetRegister() != sourceReg.GetRegister())
                {
                    context.Assembler.Mov(returnReg, sourceReg);
                }
            }

            WriteEpilogue(context);

            context.Assembler.Return();
        }

        private static void GenerateRotateRight(CodeGenContext context, Operation operation)
        {
            context.Assembler.Ror(operation.Dest, operation.GetSource(1));
        }

        private static void GenerateShiftLeft(CodeGenContext context, Operation operation)
        {
            context.Assembler.Shl(operation.Dest, operation.GetSource(1));
        }

        private static void GenerateShiftRightSI(CodeGenContext context, Operation operation)
        {
            context.Assembler.Sar(operation.Dest, operation.GetSource(1));
        }

        private static void GenerateShiftRightUI(CodeGenContext context, Operation operation)
        {
            context.Assembler.Shr(operation.Dest, operation.GetSource(1));
        }

        private static void GenerateSignExtend16(CodeGenContext context, Operation operation)
        {
            context.Assembler.Movsx16(operation.Dest, operation.GetSource(0));
        }

        private static void GenerateSignExtend32(CodeGenContext context, Operation operation)
        {
            context.Assembler.Movsx32(operation.Dest, operation.GetSource(0));
        }

        private static void GenerateSignExtend8(CodeGenContext context, Operation operation)
        {
            context.Assembler.Movsx8(operation.Dest, operation.GetSource(0));
        }

        private static void GenerateSpill(CodeGenContext context, Operation operation)
        {
            Operand offset = operation.GetSource(0);
            Operand source = operation.GetSource(1);

            if (offset.Kind != OperandKind.Constant)
            {
                throw new InvalidOperationException("Spill has non-constant stack offset.");
            }

            X86MemoryOperand memOp = new X86MemoryOperand(source.Type, Register(X86Register.Rsp), null, Scale.x1, offset.AsInt32());

            context.Assembler.Mov(memOp, source);
        }

        private static void GenerateStore(CodeGenContext context, Operation operation)
        {
            context.Assembler.Mov(operation.GetSource(0), operation.GetSource(1));
        }

        private static void GenerateStore16(CodeGenContext context, Operation operation)
        {
            context.Assembler.Mov16(operation.GetSource(0), operation.GetSource(1));
        }

        private static void GenerateStore8(CodeGenContext context, Operation operation)
        {
            context.Assembler.Mov8(operation.GetSource(0), operation.GetSource(1));
        }

        private static void GenerateStoreToContext(CodeGenContext context, Operation operation)
        {
            Operand offset = operation.GetSource(0);
            Operand source = operation.GetSource(1);

            if (offset.Kind != OperandKind.Constant)
            {
                throw new InvalidOperationException("StoreToContext has non-constant context offset.");
            }

            Operand rbp = Register(X86Register.Rbp);

            X86MemoryOperand memOp = new X86MemoryOperand(source.Type, rbp, null, Scale.x1, offset.AsInt32());

            context.Assembler.Mov(memOp, source);
        }

        private static void GenerateSubtract(CodeGenContext context, Operation operation)
        {
            ValidateDestSrc1(operation);

            context.Assembler.Sub(operation.Dest, operation.GetSource(1));
        }

        private static void GenerateCompare(CodeGenContext context, Operation operation, X86Condition condition)
        {
            context.Assembler.Cmp(operation.GetSource(0), operation.GetSource(1));
            context.Assembler.Setcc(operation.Dest, condition);
        }

        private static void ValidateDestSrc1(Operation operation)
        {
            Operand dest = operation.Dest;
            Operand src1 = operation.GetSource(0);
            Operand src2 = operation.GetSource(1);

            if (dest.Kind != OperandKind.Register)
            {
                throw new InvalidOperationException($"Invalid destination type \"{dest.Kind}\".");
            }

            if (src1.Kind != OperandKind.Register)
            {
                throw new InvalidOperationException($"Invalid source 1 type \"{src1.Kind}\".");
            }

            if (src2.Kind != OperandKind.Register && src2.Kind != OperandKind.Constant)
            {
                throw new InvalidOperationException($"Invalid source 2 type \"{src2.Kind}\".");
            }

            if (dest.GetRegister() != src1.GetRegister())
            {
                throw new InvalidOperationException("Destination and source 1 register mismatch.");
            }

            if (dest.Type != src1.Type || dest.Type != src2.Type)
            {
                throw new InvalidOperationException("Operand types mismatch.");
            }
        }

        private static void WritePrologue(CodeGenContext context)
        {
            int mask = CallingConvention.GetIntCalleeSavedRegisters() & context.RAReport.UsedRegisters;

            mask |= 1 << (int)X86Register.Rbp;

            while (mask != 0)
            {
                int bit = BitUtils.LowestBitSet(mask);

                context.Assembler.Push(Register((X86Register)bit));

                mask &= ~(1 << bit);
            }
        }

        private static void WriteEpilogue(CodeGenContext context)
        {
            int mask = CallingConvention.GetIntCalleeSavedRegisters() & context.RAReport.UsedRegisters;

            mask |= 1 << (int)X86Register.Rbp;

            while (mask != 0)
            {
                int bit = BitUtils.HighestBitSet(mask);

                context.Assembler.Pop(Register((X86Register)bit));

                mask &= ~(1 << bit);
            }
        }

        private static Operand Get32BitsRegister(Register reg)
        {
            return new Operand(reg.Index, reg.Type, OperandType.I32);
        }

        private static Operand Register(X86Register register)
        {
            return new Operand((int)register, RegisterType.Integer, OperandType.I64);
        }
    }
}