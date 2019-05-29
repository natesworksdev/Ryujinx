using ARMeilleure.Decoders;
using ARMeilleure.IntermediateRepresentation;
using ARMeilleure.State;
using ARMeilleure.Translation;
using System;

using static ARMeilleure.Instructions.InstEmitAluHelper;
using static ARMeilleure.Instructions.InstEmitHelper;
using static ARMeilleure.IntermediateRepresentation.OperandHelper;

namespace ARMeilleure.Instructions
{
    static partial class InstEmit
    {
        public static void Adc(EmitterContext context)  => EmitAdc(context, setFlags: false);
        public static void Adcs(EmitterContext context) => EmitAdc(context, setFlags: true);

        private static void EmitAdc(EmitterContext context, bool setFlags)
        {
            Operand n = GetAluN(context);
            Operand m = GetAluM(context);

            Operand d = context.IAdd(n, m);

            d = context.IAdd(d, GetFlag(PState.CFlag));

            if (setFlags)
            {
                EmitNZFlagsCheck(context, d);

                EmitAdcsCCheck(context, n, d);
                EmitAddsVCheck(context, n, m, d);
            }

            SetAluDOrZR(context, d);
        }

        public static void Add(EmitterContext context)
        {
            SetAluD(context, context.IAdd(GetAluN(context), GetAluM(context)));
        }

        public static void Adds(EmitterContext context)
        {
            Operand n = GetAluN(context);
            Operand m = GetAluM(context);

            Operand d = context.IAdd(n, m);

            EmitNZFlagsCheck(context, d);

            EmitAddsCCheck(context, n, d);
            EmitAddsVCheck(context, n, m, d);

            SetAluDOrZR(context, d);
        }

        public static void And(EmitterContext context)
        {
            SetAluD(context, context.BitwiseAnd(GetAluN(context), GetAluM(context)));
        }

        public static void Ands(EmitterContext context)
        {
            Operand n = GetAluN(context);
            Operand m = GetAluM(context);

            Operand d = context.BitwiseAnd(n, m);

            EmitNZFlagsCheck(context, d);
            EmitCVFlagsClear(context);

            SetAluDOrZR(context, d);
        }

        public static void Asrv(EmitterContext context)
        {
            SetAluDOrZR(context, context.ShiftRightSI(GetAluN(context), GetAluMShift(context)));
        }

        public static void Bic(EmitterContext context)  => EmitBic(context, setFlags: false);
        public static void Bics(EmitterContext context) => EmitBic(context, setFlags: true);

        private static void EmitBic(EmitterContext context, bool setFlags)
        {
            Operand n = GetAluN(context);
            Operand m = GetAluM(context);

            Operand d = context.BitwiseAnd(n, context.BitwiseNot(m));

            if (setFlags)
            {
                EmitNZFlagsCheck(context, d);
                EmitCVFlagsClear(context);
            }

            SetAluD(context, d, setFlags);
        }

        public static void Cls(EmitterContext context)
        {
            OpCodeAlu op = (OpCodeAlu)context.CurrOp;

            Operand n = GetIntOrZR(op, op.Rn);

            ulong mask = ulong.MaxValue >> ((64 - op.GetBitsCount()) + 1);

            n = context.BitwiseExclusiveOr(context.BitwiseAnd(n, Const(mask << 1)),
                                           context.BitwiseAnd(n, Const(mask)));

            Operand d = context.CountLeadingZeros(n);

            SetAluDOrZR(context, d);
        }

        public static void Clz(EmitterContext context)
        {
            OpCodeAlu op = (OpCodeAlu)context.CurrOp;

            Operand n = GetIntOrZR(op, op.Rn);

            Operand d = context.CountLeadingZeros(n);

            SetAluDOrZR(context, d);
        }

        public static void Eon(EmitterContext context)
        {
            Operand n = GetAluN(context);
            Operand m = GetAluM(context);

            Operand d = context.BitwiseExclusiveOr(n, context.BitwiseNot(m));

            SetAluD(context, d);
        }

        public static void Eor(EmitterContext context)
        {
            SetAluD(context, context.BitwiseExclusiveOr(GetAluN(context), GetAluM(context)));
        }

        public static void Extr(EmitterContext context)
        {
            OpCodeAluRs op = (OpCodeAluRs)context.CurrOp;

            Operand res = GetIntOrZR(op, op.Rm);

            if (op.Shift != 0)
            {
                if (op.Rn == op.Rm)
                {
                    res = context.RotateRight(res, Const(op.Shift));
                }
                else
                {
                    res = context.ShiftRightUI(res, Const(op.Shift));

                    Operand n = GetIntOrZR(op, op.Rn);

                    int invShift = op.GetBitsCount() - op.Shift;

                    res = context.BitwiseOr(res, context.ShiftLeft(n, Const(invShift)));
                }
            }

            SetAluDOrZR(context, res);
        }

        public static void Lslv(EmitterContext context)
        {
            SetAluDOrZR(context, context.ShiftLeft(GetAluN(context), GetAluMShift(context)));
        }

        public static void Lsrv(EmitterContext context)
        {
            SetAluDOrZR(context, context.ShiftRightUI(GetAluN(context), GetAluMShift(context)));
        }

        public static void Sbc(EmitterContext context)  => EmitSbc(context, setFlags: false);
        public static void Sbcs(EmitterContext context) => EmitSbc(context, setFlags: true);

        private static void EmitSbc(EmitterContext context, bool setFlags)
        {
            Operand n = GetAluN(context);
            Operand m = GetAluM(context);

            Operand d = context.ISubtract(n, m);

            Operand borrow = context.BitwiseExclusiveOr(GetFlag(PState.CFlag), Const(1));

            d = context.ISubtract(d, borrow);

            if (setFlags)
            {
                EmitNZFlagsCheck(context, d);

                EmitSbcsCCheck(context, n, m);
                EmitSubsVCheck(context, n, m, d);
            }

            SetAluDOrZR(context, d);
        }

        public static void Sub(EmitterContext context)
        {
            SetAluD(context, context.ISubtract(GetAluN(context), GetAluM(context)));
        }

        public static void Subs(EmitterContext context)
        {
            Operand n = GetAluN(context);
            Operand m = GetAluM(context);

            Operand d = context.ISubtract(n, m);

            EmitNZFlagsCheck(context, d);

            EmitSubsCCheck(context, n, m);
            EmitSubsVCheck(context, n, m, d);

            SetAluDOrZR(context, d);
        }

        public static void Orn(EmitterContext context)
        {
            Operand n = GetAluN(context);
            Operand m = GetAluM(context);

            Operand d = context.BitwiseOr(n, context.BitwiseNot(m));

            SetAluD(context, d);
        }

        public static void Orr(EmitterContext context)
        {
            SetAluD(context, context.BitwiseOr(GetAluN(context), GetAluM(context)));
        }

        public static void Rev32(EmitterContext context)
        {
            OpCodeAlu op = (OpCodeAlu)context.CurrOp;

            if (op.RegisterSize == RegisterSize.Int32)
            {
                SetAluDOrZR(context, context.ByteSwap(GetIntOrZR(op, op.Rn)));
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public static void Rev64(EmitterContext context)
        {
            OpCodeAlu op = (OpCodeAlu)context.CurrOp;

            SetAluDOrZR(context, context.ByteSwap(GetIntOrZR(op, op.Rn)));
        }

        public static void Rorv(EmitterContext context)
        {
            SetAluDOrZR(context, context.RotateRight(GetAluN(context), GetAluMShift(context)));
        }

        public static void Sdiv(EmitterContext context) => EmitDiv(context, unsigned: false);
        public static void Udiv(EmitterContext context) => EmitDiv(context, unsigned: true);

        private static void EmitDiv(EmitterContext context, bool unsigned)
        {
            OpCodeMul op = (OpCodeMul)context.CurrOp;

            //If Rm == 0, Rd = 0 (division by zero).
            Operand n = GetIntOrZR(op, op.Rn);
            Operand m = GetIntOrZR(op, op.Rm);

            Operand divisorIsZero = context.ICompareEqual(m, Const(op.GetOperandType(), 0));

            Operand lblBadDiv = Label();
            Operand lblEnd    = Label();

            context.BranchIfTrue(lblBadDiv, divisorIsZero);

            if (!unsigned)
            {
                //If Rn == INT_MIN && Rm == -1, Rd = INT_MIN (overflow).
                bool is32Bits = op.RegisterSize == RegisterSize.Int32;

                Operand intMin = is32Bits ? Const(int.MinValue) : Const(long.MinValue);
                Operand minus1 = is32Bits ? Const(-1)           : Const(-1L);

                Operand nIsIntMin = context.ICompareEqual(n, intMin);
                Operand mIsMinus1 = context.ICompareEqual(n, minus1);

                Operand lblGoodDiv = Label();

                context.BranchIfFalse(lblGoodDiv, context.BitwiseAnd(nIsIntMin, mIsMinus1));

                SetAluDOrZR(context, intMin);

                context.Branch(lblEnd);

                context.MarkLabel(lblGoodDiv);
            }

            Operand d = unsigned
                ? context.IDivideUI(n, m)
                : context.IDivide  (n, m);

            SetAluDOrZR(context, d);

            context.Branch(lblEnd);

            context.MarkLabel(lblBadDiv);

            SetAluDOrZR(context, Const(op.GetOperandType(), 0));

            context.MarkLabel(lblEnd);
        }

        private static Operand GetAluMShift(EmitterContext context)
        {
            IOpCodeAluRs op = (IOpCodeAluRs)context.CurrOp;

            Operand m = GetIntOrZR(op, op.Rm);

            return context.BitwiseAnd(m, Const(context.CurrOp.GetBitsCount() - 1));
        }

        private static void EmitNZFlagsCheck(EmitterContext context, Operand d)
        {
            context.Copy(GetFlag(PState.NFlag), context.ICompareLess (d, Const(d.Type, 0)));
            context.Copy(GetFlag(PState.ZFlag), context.ICompareEqual(d, Const(d.Type, 0)));
        }

        private static void EmitCVFlagsClear(EmitterContext context)
        {
            context.Copy(GetFlag(PState.CFlag), Const(0));
            context.Copy(GetFlag(PState.VFlag), Const(0));
        }

        public static void SetAluD(EmitterContext context, Operand d)
        {
            SetAluD(context, d, x31IsZR: false);
        }

        public static void SetAluDOrZR(EmitterContext context, Operand d)
        {
            SetAluD(context, d, x31IsZR: true);
        }

        public static void SetAluD(EmitterContext context, Operand d, bool x31IsZR)
        {
            IOpCodeAlu op = (IOpCodeAlu)context.CurrOp;

            if ((x31IsZR || op is IOpCodeAluRs) && op.Rd == RegisterConsts.ZeroIndex)
            {
                return;
            }

            context.Copy(GetIntOrSP(op, op.Rd), d);
        }
    }
}
