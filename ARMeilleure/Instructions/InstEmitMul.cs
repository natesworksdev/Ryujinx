using ARMeilleure.Decoders;
using ARMeilleure.IntermediateRepresentation;
using ARMeilleure.Translation;
using System;

using static ARMeilleure.Instructions.InstEmitHelper;

namespace ARMeilleure.Instructions
{
    static partial class InstEmit
    {
        public static void Madd(EmitterContext context) => EmitMul(context, isAdd: true);
        public static void Msub(EmitterContext context) => EmitMul(context, isAdd: false);

        private static void EmitMul(EmitterContext context, bool isAdd)
        {
            OpCodeMul op = (OpCodeMul)context.CurrOp;

            Operand a = GetIntOrZR(op, op.Ra);
            Operand n = GetIntOrZR(op, op.Rn);
            Operand m = GetIntOrZR(op, op.Rm);

            Operand res = context.Multiply(n, m);

            res = isAdd ? context.Add(a, res) : context.Subtract(a, res);

            SetIntOrZR(context, op.Rd, res);
        }

        public static void Smaddl(EmitterContext context) => EmitMull(context, MullFlags.SignedAdd);
        public static void Smsubl(EmitterContext context) => EmitMull(context, MullFlags.SignedSubtract);
        public static void Umaddl(EmitterContext context) => EmitMull(context, MullFlags.Add);
        public static void Umsubl(EmitterContext context) => EmitMull(context, MullFlags.Subtract);

        [Flags]
        private enum MullFlags
        {
            Subtract = 0,
            Add      = 1 << 0,
            Signed   = 1 << 1,

            SignedAdd      = Signed | Add,
            SignedSubtract = Signed | Subtract
        }

        private static void EmitMull(EmitterContext context, MullFlags flags)
        {
            OpCodeMul op = (OpCodeMul)context.CurrOp;

            Operand GetExtendedRegister32(int index)
            {
                Operand value = GetIntOrZR(op, index);

                if ((flags & MullFlags.Signed) != 0)
                {
                    return context.SignExtend32(value);
                }
                else
                {
                    return ZeroExtend32(context, value);
                }
            }

            Operand a = GetIntOrZR(op, op.Ra);

            Operand n = GetExtendedRegister32(op.Rn);
            Operand m = GetExtendedRegister32(op.Rm);

            Operand res = context.Multiply(n, m);

            res = (flags & MullFlags.Add) != 0 ? context.Add(a, res) : context.Subtract(a, res);

            SetIntOrZR(context, op.Rd, res);
        }

        public static void Smulh(EmitterContext context)
        {
            OpCodeMul op = (OpCodeMul)context.CurrOp;

            Operand n = GetIntOrZR(op, op.Rn);
            Operand m = GetIntOrZR(op, op.Rm);

            Operand d = context.Multiply64HighSI(n, m);

            SetIntOrZR(context, op.Rd, d);
        }

        public static void Umulh(EmitterContext context)
        {
            OpCodeMul op = (OpCodeMul)context.CurrOp;

            Operand n = GetIntOrZR(op, op.Rn);
            Operand m = GetIntOrZR(op, op.Rm);

            Operand d = context.Multiply64HighUI(n, m);

            SetIntOrZR(context, op.Rd, d);
        }
    }
}