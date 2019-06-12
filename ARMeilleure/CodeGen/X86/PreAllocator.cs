using ARMeilleure.CodeGen.Optimizations;
using ARMeilleure.IntermediateRepresentation;
using ARMeilleure.Memory;
using ARMeilleure.Translation;
using System.Collections.Generic;

using static ARMeilleure.IntermediateRepresentation.OperandHelper;

namespace ARMeilleure.CodeGen.X86
{
    static class PreAllocator
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

        public static void RunPass(ControlFlowGraph cfg, MemoryManager memory)
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

                    AddConstantCopy(node, operation);

                    //Comparison instructions uses CMOVcc, which does not zero the
                    //upper bits of the register (since it's R8), we need to ensure it
                    //is zero by zeroing it beforehand.
                    if (inst.IsComparison())
                    {
                        Operation copyOp = new Operation(Instruction.Copy, operation.Dest, Const(0));

                        block.Operations.AddBefore(node, copyOp);
                    }

                    AddFixedRegisterCopy(node, operation);

                    AddSameDestSrc1Copy(node, operation);
                }
            }
        }

        private static void AddConstantCopy(LinkedListNode<Node> node, Operation operation)
        {
            if (operation.SourcesCount == 0 || HasFixedConst(operation.Inst))
            {
                return;
            }

            Instruction inst = operation.Inst;

            Operand dest = operation.Dest;
            Operand src1 = operation.GetSource(0);
            Operand src2;

            if (src1.Type.IsInteger())
            {
                //Handle integer types.
                //Most ALU instructions accepts a 32-bits immediate on the second operand.
                //We need to ensure the following:
                //- If the constant is on operand 1, we need to move it.
                //-- But first, we try to swap operand 1 and 2 if the instruction is commutative.
                //-- Doing so may allow us to encode the constant as operand 2 and avoid a copy.
                //- If the constant is on operand 2, we check if the instruction supports it,
                //if not, we also add a copy. 64-bits constants are usually not supported.
                bool isVecCopy = inst == Instruction.Copy && !dest.Type.IsInteger();

                if (src1.Kind == OperandKind.Constant && (!HasConstSrc1(inst) || isVecCopy))
                {
                    if (IsCommutative(inst))
                    {
                        src2 = operation.GetSource(1);

                        Operand temp = src1;

                        src1 = src2;
                        src2 = temp;

                        operation.SetSource(0, src1);
                        operation.SetSource(1, src2);
                    }

                    if (src1.Kind == OperandKind.Constant)
                    {
                        src1 = AddCopy(node, src1);

                        operation.SetSource(0, src1);
                    }
                }

                if (operation.SourcesCount < 2)
                {
                    return;
                }

                src2 = operation.GetSource(1);

                if (src2.Kind == OperandKind.Constant && (!HasConstSrc2(inst) || IsLongConst(src2)))
                {
                    src2 = AddCopy(node, src2);

                    operation.SetSource(1, src2);
                }
            }
            else
            {
                //Handle non-integer types (FP32, FP64 and V128).
                //For instructions without an immediate operand, we do the following:
                //- Insert a copy with the constant value (as integer) to a GPR.
                //- Insert a copy from the GPR to a XMM register.
                //- Replace the constant use with the XMM register.
                if (src1.Kind == OperandKind.Constant && src1.Type.IsInteger())
                {
                    src1 = AddXmmCopy(node, src1);

                    operation.SetSource(0, src1);
                }

                if (operation.SourcesCount < 2)
                {
                    return;
                }

                src2 = operation.GetSource(1);

                if (src2.Kind == OperandKind.Constant && src2.Type.IsInteger())
                {
                    src2 = AddXmmCopy(node, src2);

                    operation.SetSource(1, src2);
                }
            }
        }

        private static void AddFixedRegisterCopy(LinkedListNode<Node> node, Operation operation)
        {
            if (operation.SourcesCount == 0)
            {
                return;
            }

            Instruction inst = operation.Inst;

            Operand dest = operation.Dest;
            Operand src1 = operation.GetSource(0);

            //Handle the many restrictions of the division instructions:
            //- The dividend is always in RDX:RAX.
            //- The result is always in RAX.
            //- Additionally it also writes the remainder in RDX.
            if (inst == Instruction.Divide || inst == Instruction.DivideUI)
            {
                Operand rax = Gpr(X86Register.Rax, src1.Type);
                Operand rdx = Gpr(X86Register.Rdx, src1.Type);

                Operation srcCopyOp = new Operation(Instruction.Copy, rax, src1);

                node.List.AddBefore(node, srcCopyOp);

                operation.SetSource(0, rax);

                Operation clobberCopyOp = new Operation(Instruction.Copy, rdx, rdx);

                node.List.AddBefore(node, clobberCopyOp);

                Operation destCopyOp = new Operation(Instruction.Copy, dest, rax);

                node.List.AddAfter(node, destCopyOp);

                operation.Dest = rax;
            }

            //Handle the many restrictions of the i64 * i64 = i128 multiply instructions:
            //- The multiplicand is always in RAX.
            //- The lower 64-bits of the result is always in RAX.
            //- The higher 64-bits of the result is always in RDX.
            if (inst == Instruction.Multiply64HighSI || inst == Instruction.Multiply64HighUI)
            {
                Operand rax = Gpr(X86Register.Rax, src1.Type);
                Operand rdx = Gpr(X86Register.Rdx, src1.Type);

                Operation srcCopyOp = new Operation(Instruction.Copy, rax, src1);

                node.List.AddBefore(node, srcCopyOp);

                operation.SetSource(0, rax);

                Operation destCopyOp = new Operation(Instruction.Copy, dest, rdx);

                node.List.AddAfter(node, destCopyOp);

                Operation clobberCopyOp = new Operation(Instruction.Copy, rax, rax);

                node.List.AddAfter(node, clobberCopyOp);

                operation.Dest = rdx;
            }

            //The only allowed shift register is CL.
            if (inst.IsShift() && operation.GetSource(1).Kind == OperandKind.LocalVariable)
            {
                Operand rcx = Gpr(X86Register.Rcx, OperandType.I32);

                Operation copyOp = new Operation(Instruction.Copy, rcx, operation.GetSource(1));

                node.List.AddBefore(node, copyOp);

                operation.SetSource(1, rcx);
            }

            //Copy values to registers expected by the function being called,
            //as mandated by the ABI.
            if (inst == Instruction.Call)
            {
                int argsCount = operation.SourcesCount;

                int maxArgs = CallingConvention.GetArgumentsOnRegsCount();

                if (argsCount > maxArgs + 1)
                {
                    argsCount = maxArgs + 1;
                }

                for (int index = 1; index < argsCount; index++)
                {
                    Operand source = operation.GetSource(index);

                    RegisterType regType = source.Type.ToRegisterType();

                    Operand argReg;

                    if (regType == RegisterType.Integer)
                    {
                        argReg = Gpr(CallingConvention.GetIntArgumentRegister(index - 1), source.Type);
                    }
                    else /* if (regType == RegisterType.Vector) */
                    {
                        argReg = Xmm(CallingConvention.GetVecArgumentRegister(index - 1), source.Type);
                    }

                    Operation srcCopyOp = new Operation(Instruction.Copy, argReg, source);

                    node.List.AddBefore(node, srcCopyOp);

                    operation.SetSource(index, argReg);
                }

                //The remaining arguments (those that are not passed on registers)
                //should be passed on the stack, we write them to the stack with "SpillArg".
                for (int index = argsCount; index < operation.SourcesCount; index++)
                {
                    Operand source = operation.GetSource(index);

                    Operand offset = new Operand((index - 1) * 8);

                    Operation srcSpillOp = new Operation(Instruction.SpillArg, null, offset, source);

                    node.List.AddBefore(node, srcSpillOp);

                    operation.SetSource(index, new Operand(OperandKind.Undefined));
                }

                if (dest != null)
                {
                    RegisterType regType = dest.Type.ToRegisterType();

                    Operand retReg;

                    if (regType == RegisterType.Integer)
                    {
                        retReg = Gpr(CallingConvention.GetIntReturnRegister(), dest.Type);
                    }
                    else /* if (regType == RegisterType.Vector) */
                    {
                        retReg = Xmm(CallingConvention.GetVecReturnRegister(), dest.Type);
                    }

                    Operation destCopyOp = new Operation(Instruction.Copy, dest, retReg);

                    node.List.AddAfter(node, destCopyOp);

                    operation.Dest = retReg;
                }
            }
        }

        private static void AddSameDestSrc1Copy(LinkedListNode<Node> node, Operation operation)
        {
            if (operation.Dest == null || operation.SourcesCount == 0)
            {
                return;
            }

            Instruction inst = operation.Inst;

            Operand dest = operation.Dest;
            Operand src1 = operation.GetSource(0);

            //The multiply instruction (that maps to IMUL) is somewhat special, it has
            //a three operand form where the second source is a immediate value.
            bool threeOperandForm = inst == Instruction.Multiply && operation.GetSource(1).Kind == OperandKind.Constant;

            if (IsSameOperandDestSrc1(operation) && src1.Kind == OperandKind.LocalVariable && !threeOperandForm)
            {
                Operation copyOp = new Operation(Instruction.Copy, dest, src1);

                node.List.AddBefore(node, copyOp);

                operation.SetSource(0, dest);
            }
            else if (inst == Instruction.ConditionalSelect)
            {
                Operand src3 = operation.GetSource(2);

                Operation copyOp = new Operation(Instruction.Copy, dest, src3);

                node.List.AddBefore(node, copyOp);

                operation.SetSource(2, dest);
            }
        }

        private static Operand AddXmmCopy(LinkedListNode<Node> node, Operand source)
        {
            Operand temp = Local(source.Type);

            Operation copyOp = new Operation(Instruction.Copy, temp, AddCopy(node, GetIntConst(source)));

            node.List.AddBefore(node, copyOp);

            return temp;
        }

        private static Operand AddCopy(LinkedListNode<Node> node, Operand source)
        {
            Operand temp = Local(source.Type);

            Operation copyOp = new Operation(Instruction.Copy, temp, source);

            node.List.AddBefore(node, copyOp);

            return temp;
        }

        private static Operand GetIntConst(Operand value)
        {
            if (value.Type == OperandType.FP32)
            {
                return Const(value.AsInt32());
            }
            else if (value.Type == OperandType.FP64)
            {
                return Const(value.AsInt64());
            }

            return value;
        }

        private static bool IsLongConst(Operand operand)
        {
            long value = operand.Type == OperandType.I32 ? operand.AsInt32()
                                                         : operand.AsInt64();

            return !ConstFitsOnS32(value);
        }

        private static bool ConstFitsOnS32(long value)
        {
            return value == (int)value;
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

        private static Operand Xmm(X86Register register, OperandType type)
        {
            return Register((int)register, RegisterType.Vector, type);
        }

        private static bool IsSameOperandDestSrc1(Operation operation)
        {
            switch (operation.Inst)
            {
                case Instruction.Add:
                case Instruction.BitwiseAnd:
                case Instruction.BitwiseExclusiveOr:
                case Instruction.BitwiseNot:
                case Instruction.BitwiseOr:
                case Instruction.ByteSwap:
                case Instruction.Multiply:
                case Instruction.Negate:
                case Instruction.RotateRight:
                case Instruction.ShiftLeft:
                case Instruction.ShiftRightSI:
                case Instruction.ShiftRightUI:
                case Instruction.Subtract:
                    return true;

                case Instruction.VectorInsert:
                case Instruction.VectorInsert16:
                case Instruction.VectorInsert8:
                    return !HardwareCapabilities.SupportsVexEncoding;
            }

            return IsVexSameOperandDestSrc1(operation);
        }

        private static bool IsVexSameOperandDestSrc1(Operation operation)
        {
            if (IsIntrinsic(operation.Inst))
            {
                bool isUnary = operation.SourcesCount < 2;

                return !HardwareCapabilities.SupportsVexEncoding && !isUnary;
            }

            return false;
        }

        private static bool HasConstSrc1(Instruction inst)
        {
            switch (inst)
            {
                case Instruction.Copy:
                case Instruction.LoadFromContext:
                case Instruction.StoreToContext:
                    return true;
            }

            return false;
        }

        private static bool HasConstSrc2(Instruction inst)
        {
            switch (inst)
            {
                case Instruction.Add:
                case Instruction.BitwiseAnd:
                case Instruction.BitwiseExclusiveOr:
                case Instruction.BitwiseOr:
                case Instruction.CompareEqual:
                case Instruction.CompareGreater:
                case Instruction.CompareGreaterOrEqual:
                case Instruction.CompareGreaterOrEqualUI:
                case Instruction.CompareGreaterUI:
                case Instruction.CompareLess:
                case Instruction.CompareLessOrEqual:
                case Instruction.CompareLessOrEqualUI:
                case Instruction.CompareLessUI:
                case Instruction.CompareNotEqual:
                case Instruction.Multiply:
                case Instruction.ShiftLeft:
                case Instruction.ShiftRightSI:
                case Instruction.ShiftRightUI:
                case Instruction.Subtract:
                case Instruction.VectorExtract:
                case Instruction.VectorExtract16:
                case Instruction.VectorExtract8:
                    return true;
            }

            return false;
        }

        private static bool IsCommutative(Instruction inst)
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

        private static bool HasFixedConst(Instruction inst)
        {
            switch (inst)
            {
                case Instruction.LoadFromContext:
                case Instruction.StoreToContext:
                case Instruction.VectorExtract:
                case Instruction.VectorExtract16:
                case Instruction.VectorExtract8:
                case Instruction.VectorInsert:
                case Instruction.VectorInsert16:
                case Instruction.VectorInsert8:
                    return true;
            }

            return IsIntrinsic(inst);
        }

        private static bool IsIntrinsic(Instruction inst)
        {
            return inst > Instruction.X86Intrinsic_Start &&
                   inst < Instruction.X86Intrinsic_End;
        }
    }
}