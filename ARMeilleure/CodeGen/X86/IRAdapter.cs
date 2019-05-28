using ARMeilleure.CodeGen.Optimizations;
using ARMeilleure.IntermediateRepresentation;
using ARMeilleure.Memory;
using ARMeilleure.State;
using ARMeilleure.Translation;
using System.Collections.Generic;

using static ARMeilleure.IntermediateRepresentation.OperandHelper;

namespace ARMeilleure.CodeGen.X86
{
    static class IRAdapter
    {
        private class IRModContext
        {
            private BasicBlock           _block;
            private LinkedListNode<Node> _node;

            public IRModContext(BasicBlock block, LinkedListNode<Node> node)
            {
                _block = block;
                _node  = node.Previous;
            }

            public Operand Append(Instruction inst, Operand src1, Operand src2)
            {
                Operand destSrc = AppendCopy(src1);

                Operation operation = new Operation(inst, destSrc, destSrc, src2);

                if (src2 is X86MemoryOperand memOp)
                {
                    AddMemoryOperandUse(memOp, operation);
                }

                Append(operation);

                return destSrc;
            }

            public Operand AppendCopy(Operand src)
            {
                Operation operation = new Operation(Instruction.Copy, Local(OperandType.I64), src);

                if (src is X86MemoryOperand memOp)
                {
                    AddMemoryOperandUse(memOp, operation);
                }

                Append(operation);

                return operation.Dest;
            }

            private void Append(Operation operation)
            {
                if (_node != null)
                {
                    _node = _block.Operations.AddAfter(_node, operation);
                }
                else
                {
                    _node = _block.Operations.AddFirst(operation);
                }
            }
        }

        public static void Adapt(ControlFlowGraph cfg, MemoryManager memory)
        {
            Optimizer.Optimize(cfg);

            foreach (BasicBlock block in cfg.Blocks)
            {
                for (LinkedListNode<Node> node = block.Operations.First; node != null; node = node.Next)
                {
                    if (!(node.Value is Operation operation))
                    {
                        continue;
                    }

                    void Clobber(X86Register register)
                    {
                        Operand reg = Gpr(register, OperandType.I32);

                        Operation copyOp = new Operation(Instruction.Copy, reg, reg);

                        block.Operations.AddBefore(node, copyOp);
                    }

                    Operand AddCopy(Operand source)
                    {
                        Operand temp = Local(source.Type);

                        Operation copyOp = new Operation(Instruction.Copy, temp, source);

                        block.Operations.AddBefore(node, copyOp);

                        return temp;
                    }

                    Instruction inst = operation.Inst;

                    if (inst.IsMemory())
                    {
                        IRModContext context = new IRModContext(block, node);

                        Operand va = operation.GetSource(0);

                        OperandType valueType = inst == Instruction.Store   ||
                                                inst == Instruction.Store16 ||
                                                inst == Instruction.Store8 ? operation.GetSource(1).Type : operation.Dest.Type;

                        X86MemoryOperand hostAddr = GuestToHostAddress(context, memory, valueType, va);

                        operation.SetSource(0, hostAddr);

                        AddMemoryOperandUse(hostAddr, operation);
                    }

                    if (IsRMOnly(inst))
                    {
                        Operand src = operation.GetSource(0);

                        if (src.Kind == OperandKind.Constant)
                        {
                            src = AddCopy(src);

                            operation.SetSource(0, src);
                        }
                    }

                    if (operation.Dest == null || operation.SourcesCount == 0)
                    {
                        continue;
                    }

                    Operand dest = operation.Dest;
                    Operand src1 = operation.GetSource(0);

                    bool isBinary = operation.SourcesCount == 2;

                    bool isRMOnly = IsRMOnly(inst);

                    if (operation.Inst != Instruction.Copy)
                    {
                        if ((src1.Kind == OperandKind.Constant && isBinary) || isRMOnly || IsLongConst(dest.Type, src1))
                        {
                            if (IsComutative(inst))
                            {
                                Operand src2 = operation.GetSource(1);
                                Operand temp = src1;

                                src1 = src2;
                                src2 = temp;

                                operation.SetSource(0, src1);
                                operation.SetSource(1, src2);
                            }

                            if (src1.Kind == OperandKind.Constant)
                            {
                                src1 = AddCopy(src1);

                                operation.SetSource(0, src1);
                            }
                        }
                    }

                    if (isBinary)
                    {
                        Operand src2 = operation.GetSource(1);

                        //Comparison instructions uses CMOVcc, which does not zero the
                        //upper bits of the register (since it's R8), we need to ensure it
                        //is zero by zeroing it beforehand.
                        if (inst.IsComparison())
                        {
                            Operation copyOp = new Operation(Instruction.Copy, dest, Const(0));

                            block.Operations.AddBefore(node, copyOp);
                        }

                        //64-bits immediates are only supported by the MOV instruction.
                        if (isRMOnly || IsLongConst(dest.Type, src2))
                        {
                            src2 = AddCopy(src2);

                            operation.SetSource(1, src2);
                        }

                        //Handle the many restrictions of the division instructions:
                        //- The dividend is always in RDX:RAX.
                        //- The result is always in RAX.
                        //- Additionally it also writes the remainder in RDX.
                        if (inst == Instruction.Divide || inst == Instruction.DivideUI)
                        {
                            Operand rax = Gpr(X86Register.Rax, src1.Type);
                            Operand rdx = Gpr(X86Register.Rdx, src1.Type);

                            Operation srcCopyOp = new Operation(Instruction.Copy, rax, src1);

                            block.Operations.AddBefore(node, srcCopyOp);

                            src1 = rax;

                            operation.SetSource(0, src1);

                            Clobber(X86Register.Rdx);

                            Operation destCopyOp = new Operation(Instruction.Copy, dest, rax);

                            block.Operations.AddAfter(node, destCopyOp);

                            dest = rax;

                            operation.Dest = dest;
                        }

                        //The only allowed shift register is CL.
                        if (inst.IsShift() && src2.Kind == OperandKind.LocalVariable)
                        {
                            Operand rcx = Gpr(X86Register.Rcx, OperandType.I32);

                            Operation copyOp = new Operation(Instruction.Copy, rcx, src2);

                            block.Operations.AddBefore(node, copyOp);

                            src2 = rcx;

                            operation.SetSource(1, src2);
                        }
                    }

                    //The multiply instruction (that maps to IMUL) is somewhat special, it has
                    //a three operand form where the second source is a immediate value.
                    bool threeOperandForm = inst == Instruction.Multiply && operation.GetSource(1).Kind == OperandKind.Constant;

                    if (IsSameOperandDestSrc1(inst) && src1.Kind == OperandKind.LocalVariable && !threeOperandForm)
                    {
                        Operation copyOp = new Operation(Instruction.Copy, dest, src1);

                        block.Operations.AddBefore(node, copyOp);

                        operation.SetSource(0, dest);

                        src1 = dest;
                    }
                    else if (inst == Instruction.ConditionalSelect)
                    {
                        Operand src3 = operation.GetSource(2);

                        Operation copyOp = new Operation(Instruction.Copy, dest, src3);

                        block.Operations.AddBefore(node, copyOp);

                        operation.SetSource(2, dest);
                    }

                    if (inst == Instruction.LoadFromContext ||
                        inst == Instruction.StoreToContext)
                    {
                        if (inst == Instruction.LoadFromContext)
                        {
                            src1 = GetContextMemoryOperand(src1.GetRegister());

                            operation.Dest = null;
                        }
                        else /* if (inst == Instruction.StoreToContext) */
                        {
                            dest = GetContextMemoryOperand(dest.GetRegister());

                            operation.SetSource(0, null);
                        }

                        operation = new Operation(Instruction.Copy, dest, src1);

                        LinkedListNode<Node> temp = block.Operations.AddBefore(node, operation);

                        block.Operations.Remove(node);

                        node = temp;
                    }
                }
            }
        }

        private static bool IsLongConst(OperandType destType, Operand operand)
        {
            if (operand.Kind != OperandKind.Constant)
            {
                return false;
            }

            if (operand.Type == destType || destType == OperandType.I32)
            {
                return operand.AsInt32() != operand.AsInt64();
            }
            else
            {
                return operand.Value >> 31 != 0;
            }
        }

        private static X86MemoryOperand GuestToHostAddress(
            IRModContext  context,
            MemoryManager memory,
            OperandType   valueType,
            Operand       va)
        {
            Operand vaPageOffs = context.Append(Instruction.BitwiseAnd, va, Const((ulong)MemoryManager.PageMask));

            Operand ptBaseAddr = context.AppendCopy(Const(memory.PageTable.ToInt64()));

            int bit = MemoryManager.PageBits;

            do
            {
                va = context.Append(Instruction.ShiftRightUI, va, Const(bit));

                Operand ptOffs = va;

                bit += memory.PtLevelBits;

                if (bit < memory.AddressSpaceBits)
                {
                    ptOffs = context.Append(Instruction.BitwiseAnd, va, Const((ulong)memory.PtLevelMask));
                }

                X86MemoryOperand memOp = new X86MemoryOperand(OperandType.I64, ptBaseAddr, ptOffs, Scale.x8, 0);

                ptBaseAddr = context.AppendCopy(memOp);
            }
            while (bit < memory.AddressSpaceBits);

            return new X86MemoryOperand(valueType, ptBaseAddr, vaPageOffs, Scale.x1, 0);
        }

        private static X86MemoryOperand GetContextMemoryOperand(Register reg)
        {
            Operand baseReg = Register((int)X86Register.Rbp, RegisterType.Integer, OperandType.I64);

            int offset = NativeContext.GetRegisterOffset(reg);

            return new X86MemoryOperand(OperandType.I64, baseReg, null, Scale.x1, offset);
        }

        private static void AddMemoryOperandUse(X86MemoryOperand memOp, Operation operation)
        {
            memOp.BaseAddress.Uses.AddLast(operation);

            if (memOp.Index != null)
            {
                memOp.Index.Uses.AddLast(operation);
            }
        }

        private static Operand Gpr(X86Register register, OperandType type)
        {
            return Register((int)register, RegisterType.Integer, type);
        }

        private static bool IsSameOperandDestSrc1(Instruction inst)
        {
            switch (inst)
            {
                case Instruction.Add:
                case Instruction.BitwiseAnd:
                case Instruction.BitwiseExclusiveOr:
                case Instruction.BitwiseNot:
                case Instruction.BitwiseOr:
                case Instruction.Multiply:
                case Instruction.Negate:
                case Instruction.RotateRight:
                case Instruction.ShiftLeft:
                case Instruction.ShiftRightSI:
                case Instruction.ShiftRightUI:
                case Instruction.Subtract:
                    return true;
            }

            return false;
        }

        private static bool IsComutative(Instruction inst)
        {
            switch (inst)
            {
                case Instruction.Add:
                case Instruction.BitwiseAnd:
                case Instruction.BitwiseExclusiveOr:
                case Instruction.BitwiseOr:
                case Instruction.CompareEqual:
                case Instruction.CompareNotEqual:
                case Instruction.Multiply:
                    return true;
            }

            return false;
        }

        private static bool IsRMOnly(Instruction inst)
        {
            switch (inst)
            {
                case Instruction.BranchIfFalse:
                case Instruction.BranchIfTrue:
                case Instruction.ConditionalSelect:
                case Instruction.Divide:
                case Instruction.DivideUI:
                case Instruction.SignExtend16:
                case Instruction.SignExtend32:
                case Instruction.SignExtend8:
                    return true;
            }

            return false;
        }
    }
}