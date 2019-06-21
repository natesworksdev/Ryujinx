using ARMeilleure.Decoders;
using ARMeilleure.IntermediateRepresentation;
using ARMeilleure.Translation;

using static ARMeilleure.Instructions.InstEmitHelper;
using static ARMeilleure.Instructions.InstEmitMemoryHelper;
using static ARMeilleure.IntermediateRepresentation.OperandHelper;

namespace ARMeilleure.Instructions
{
    static partial class InstEmit
    {
        public static void Adr(EmitterContext context)
        {
            OpCodeAdr op = (OpCodeAdr)context.CurrOp;

            SetIntOrZR(context, op.Rd, Const(op.Address + (ulong)op.Immediate));
        }

        public static void Adrp(EmitterContext context)
        {
            OpCodeAdr op = (OpCodeAdr)context.CurrOp;

            ulong address = (op.Address & ~0xfffUL) + ((ulong)op.Immediate << 12);

            SetIntOrZR(context, op.Rd, Const(address));
        }

        public static void Ldr(EmitterContext context)  => EmitLdr(context, signed: false);
        public static void Ldrs(EmitterContext context) => EmitLdr(context, signed: true);

        private static void EmitLdr(EmitterContext context, bool signed)
        {
            OpCodeMem op = (OpCodeMem)context.CurrOp;

            Operand address = GetAddress(context);

            if (signed && op.Extend64)
            {
                EmitLoadSx64(context, address, op.Rt, op.Size);
            }
            else if (signed)
            {
                EmitLoadSx32(context, address, op.Rt, op.Size);
            }
            else
            {
                EmitLoadZx(context, address, op.Rt, op.Size);
            }

            EmitWBackIfNeeded(context, address);
        }

        public static void Ldr_Literal(EmitterContext context)
        {
            IOpCodeLit op = (IOpCodeLit)context.CurrOp;

            if (op.Prefetch)
            {
                return;
            }

            if (op.Signed)
            {
                EmitLoadSx64(context, Const(op.Immediate), op.Rt, op.Size);
            }
            else
            {
                EmitLoadZx(context, Const(op.Immediate), op.Rt, op.Size);
            }
        }

        public static void Ldp(EmitterContext context)
        {
            OpCodeMemPair op = (OpCodeMemPair)context.CurrOp;

            void EmitLoad(int rt, Operand ldAddr)
            {
                if (op.Extend64)
                {
                    EmitLoadSx64(context, ldAddr, rt, op.Size);
                }
                else
                {
                    EmitLoadZx(context, ldAddr, rt, op.Size);
                }
            }

            Operand address = GetAddress(context);

            Operand address2 = context.Add(address, Const(1L << op.Size));

            EmitLoad(op.Rt,  address);
            EmitLoad(op.Rt2, address2);

            EmitWBackIfNeeded(context, address);
        }

        public static void Str(EmitterContext context)
        {
            OpCodeMem op = (OpCodeMem)context.CurrOp;

            Operand address = GetAddress(context);

            InstEmitMemoryHelper.EmitStore(context, address, op.Rt, op.Size);

            EmitWBackIfNeeded(context, address);
        }

        public static void Stp(EmitterContext context)
        {
            OpCodeMemPair op = (OpCodeMemPair)context.CurrOp;

            Operand address = GetAddress(context);

            Operand address2 = context.Add(address, Const(1L << op.Size));

            InstEmitMemoryHelper.EmitStore(context, address,  op.Rt,  op.Size);
            InstEmitMemoryHelper.EmitStore(context, address2, op.Rt2, op.Size);

            EmitWBackIfNeeded(context, address);
        }

        private static Operand GetAddress(EmitterContext context)
        {
            Operand address = null;

            switch (context.CurrOp)
            {
                case OpCodeMemImm op:
                {
                    address = context.Copy(GetIntOrSP(op, op.Rn));

                    //Pre-indexing.
                    if (!op.PostIdx)
                    {
                        address = context.Add(address, Const(op.Immediate));
                    }

                    break;
                }

                case OpCodeMemReg op:
                {
                    Operand n = GetIntOrSP(op, op.Rn);

                    Operand m = GetExtendedM(context, op.Rm, op.IntType);

                    if (op.Shift)
                    {
                        m = context.ShiftLeft(m, Const(op.Size));
                    }

                    address = context.Add(n, m);

                    break;
                }
            }

            return address;
        }

        private static void EmitWBackIfNeeded(EmitterContext context, Operand address)
        {
            //Check whenever the current OpCode has post-indexed write back, if so write it.
            if (context.CurrOp is OpCodeMemImm op && op.WBack)
            {
                if (op.PostIdx)
                {
                    address = context.Add(address, Const(op.Immediate));
                }

                context.Copy(GetIntOrSP(op, op.Rn), address);
            }
        }
    }
}