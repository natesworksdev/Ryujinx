using ARMeilleure.Decoders;
using ARMeilleure.IntermediateRepresentation;
using ARMeilleure.Translation;

using static ARMeilleure.Instructions.InstEmitHelper;
using static ARMeilleure.IntermediateRepresentation.OperandHelper;

namespace ARMeilleure.Instructions
{
    static partial class InstEmit
    {
        public static void Bfm(EmitterContext context)
        {
            OpCodeBfm op = (OpCodeBfm)context.CurrOp;

            Operand d = GetIntOrZR(op, op.Rd);
            Operand n = GetIntOrZR(op, op.Rn);

            Operand res;

            if (op.Pos < op.Shift)
            {
                //BFI.
                int shift = op.GetBitsCount() - op.Shift;

                int width = op.Pos + 1;

                ulong mask = ulong.MaxValue >> (64 - width);

                res = context.ShiftLeft(context.BitwiseAnd(n, Const(mask)), Const(shift));

                res = context.BitwiseOr(res, context.BitwiseAnd(d, Const(~(mask << shift))));
            }
            else
            {
                //BFXIL.
                int shift = op.Shift;

                int width = op.Pos - shift + 1;

                ulong mask = ulong.MaxValue >> (64 - width);

                res = context.BitwiseAnd(context.ShiftRightUI(n, Const(shift)), Const(mask));

                res = context.BitwiseOr(res, context.BitwiseAnd(d, Const(~mask)));
            }

            context.Copy(d, res);
        }

        public static void Sbfm(EmitterContext context)
        {
            OpCodeBfm op = (OpCodeBfm)context.CurrOp;

            int bitsCount = op.GetBitsCount();

            if (op.Pos + 1 == bitsCount)
            {
                EmitSbfmShift(context);
            }
            else if (op.Pos < op.Shift)
            {
                EmitSbfiz(context);
            }
            else if (op.Pos == 7 && op.Shift == 0)
            {
                Operand n = GetIntOrZR(op, op.Rn);

                SetIntOrZR(context, op.Rd, context.SignExtend8(n));
            }
            else if (op.Pos == 15 && op.Shift == 0)
            {
                Operand n = GetIntOrZR(op, op.Rn);

                SetIntOrZR(context, op.Rd, context.SignExtend16(n));
            }
            else if (op.Pos == 31 && op.Shift == 0)
            {
                Operand n = GetIntOrZR(op, op.Rn);

                SetIntOrZR(context, op.Rd, context.SignExtend32(n));
            }
            else
            {
                Operand res = GetIntOrZR(op, op.Rn);

                res = context.ShiftLeft   (res, Const(bitsCount - 1 - op.Pos));
                res = context.ShiftRightSI(res, Const(bitsCount - 1));
                res = context.BitwiseAnd  (res, Const(~op.TMask));

                Operand n2 = GetBfmN(context);

                SetIntOrZR(context, op.Rd, context.BitwiseOr(res, n2));
            }
        }

        public static void Ubfm(EmitterContext context)
        {
            OpCodeBfm op = (OpCodeBfm)context.CurrOp;

            if (op.Pos + 1 == op.GetBitsCount())
            {
                EmitUbfmShift(context);
            }
            else if (op.Pos < op.Shift)
            {
                EmitUbfiz(context);
            }
            else if (op.Pos + 1 == op.Shift)
            {
                EmitBfmLsl(context);
            }
            else if (op.Pos == 7 && op.Shift == 0)
            {
                Operand n = GetIntOrZR(op, op.Rn);

                SetIntOrZR(context, op.Rd, context.BitwiseAnd(n, Const(0xff)));
            }
            else if (op.Pos == 15 && op.Shift == 0)
            {
                Operand n = GetIntOrZR(op, op.Rn);

                SetIntOrZR(context, op.Rd, context.BitwiseAnd(n, Const(0xffff)));
            }
            else
            {
                SetIntOrZR(context, op.Rd, GetBfmN(context));
            }
        }

        private static void EmitSbfiz(EmitterContext context) => EmitBfiz(context, signed: true);
        private static void EmitUbfiz(EmitterContext context) => EmitBfiz(context, signed: false);

        private static void EmitBfiz(EmitterContext context, bool signed)
        {
            OpCodeBfm op = (OpCodeBfm)context.CurrOp;

            int width = op.Pos + 1;

            Operand res = GetIntOrZR(op, op.Rn);

            res = context.ShiftLeft(res, Const(op.GetBitsCount() - width));

            res = signed
                ? context.ShiftRightSI(res, Const(op.Shift - width))
                : context.ShiftRightUI(res, Const(op.Shift - width));

            SetIntOrZR(context, op.Rd, res);
        }

        private static void EmitSbfmShift(EmitterContext context)
        {
            EmitBfmShift(context, signed: true);
        }

        private static void EmitUbfmShift(EmitterContext context)
        {
            EmitBfmShift(context, signed: false);
        }

        private static void EmitBfmShift(EmitterContext context, bool signed)
        {
            OpCodeBfm op = (OpCodeBfm)context.CurrOp;

            Operand res = GetIntOrZR(op, op.Rn);

            res = signed
                ? context.ShiftRightSI(res, Const(op.Shift))
                : context.ShiftRightUI(res, Const(op.Shift));

            SetIntOrZR(context, op.Rd, res);
        }

        private static void EmitBfmLsl(EmitterContext context)
        {
            OpCodeBfm op = (OpCodeBfm)context.CurrOp;

            Operand res = GetIntOrZR(op, op.Rn);

            int shift = op.GetBitsCount() - op.Shift;

            SetIntOrZR(context, op.Rd, context.ShiftLeft(res, Const(shift)));
        }

        private static Operand GetBfmN(EmitterContext context)
        {
            OpCodeBfm op = (OpCodeBfm)context.CurrOp;

            Operand res = GetIntOrZR(op, op.Rn);

            long mask = op.WMask & op.TMask;

            return context.BitwiseAnd(context.RotateRight(res, Const(op.Shift)), Const(mask));
        }
    }
}