using ARMeilleure.Decoders;
using ARMeilleure.IntermediateRepresentation;
using ARMeilleure.State;
using ARMeilleure.Translation;
using System;

using static ARMeilleure.Instructions.InstEmitHelper;
using static ARMeilleure.Instructions.InstEmitSimdHelper;
using static ARMeilleure.IntermediateRepresentation.OperandHelper;

namespace ARMeilleure.Instructions
{
    using Func2I = Func<Operand, Operand, Operand>;

    static partial class InstEmit
    {
        public static void Cmeq_S(EmitterContext context)
        {
            EmitCmpOp(context, (op1, op2) => context.ICompareEqual(op1, op2), scalar: true);
        }

        public static void Cmeq_V(EmitterContext context)
        {
            if (Optimizations.UseSse41)
            {
                OpCodeSimd op = (OpCodeSimd)context.CurrOp;

                Operand n = GetVec(op.Rn);
                Operand m;

                if (op is OpCodeSimdReg binOp)
                {
                    m = GetVec(binOp.Rm);
                }
                else
                {
                    m = context.VectorZero();
                }

                Instruction cmpInst = X86PcmpeqInstruction[op.Size];

                Operand res = context.AddIntrinsic(cmpInst, n, m);

                if (op.RegisterSize == RegisterSize.Simd64)
                {
                    res = context.VectorZeroUpper64(res);
                }

                context.Copy(GetVec(op.Rd), res);
            }
            else
            {
                EmitCmpOp(context, (op1, op2) => context.ICompareEqual(op1, op2), scalar: false);
            }
        }

        public static void Cmge_S(EmitterContext context)
        {
            EmitCmpOp(context, (op1, op2) => context.ICompareGreaterOrEqual(op1, op2), scalar: true);
        }

        public static void Cmge_V(EmitterContext context)
        {
            if (Optimizations.UseSse42)
            {
                OpCodeSimd op = (OpCodeSimd)context.CurrOp;

                Operand n = GetVec(op.Rn);
                Operand m;

                if (op is OpCodeSimdReg binOp)
                {
                    m = GetVec(binOp.Rm);
                }
                else
                {
                    m = context.VectorZero();
                }

                Instruction cmpInst = X86PcmpgtInstruction[op.Size];

                Operand res = context.AddIntrinsic(cmpInst, m, n);

                Operand mask = X86GetAllElements(context, -1L);

                res = context.AddIntrinsic(Instruction.X86Pandn, res, mask);

                if (op.RegisterSize == RegisterSize.Simd64)
                {
                    res = context.VectorZeroUpper64(res);
                }

                context.Copy(GetVec(op.Rd), res);
            }
            else
            {
                EmitCmpOp(context, (op1, op2) => context.ICompareGreaterOrEqual(op1, op2), scalar: false);
            }
        }

        public static void Cmgt_S(EmitterContext context)
        {
            EmitCmpOp(context, (op1, op2) => context.ICompareGreater(op1, op2), scalar: true);
        }

        public static void Cmgt_V(EmitterContext context)
        {
            if (Optimizations.UseSse42)
            {
                OpCodeSimd op = (OpCodeSimd)context.CurrOp;

                Operand n = GetVec(op.Rn);
                Operand m;

                if (op is OpCodeSimdReg binOp)
                {
                    m = GetVec(binOp.Rm);
                }
                else
                {
                    m = context.VectorZero();
                }

                Instruction cmpInst = X86PcmpgtInstruction[op.Size];

                Operand res = context.AddIntrinsic(cmpInst, n, m);

                if (op.RegisterSize == RegisterSize.Simd64)
                {
                    res = context.VectorZeroUpper64(res);
                }

                context.Copy(GetVec(op.Rd), res);
            }
            else
            {
                EmitCmpOp(context, (op1, op2) => context.ICompareGreater(op1, op2), scalar: false);
            }
        }

        public static void Cmhi_S(EmitterContext context)
        {
            EmitCmpOp(context, (op1, op2) => context.ICompareGreaterUI(op1, op2), scalar: true);
        }

        public static void Cmhi_V(EmitterContext context)
        {
            OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

            if (Optimizations.UseSse41 && op.Size < 3)
            {
                Operand n = GetVec(op.Rn);
                Operand m = GetVec(op.Rm);

                Instruction maxInst = X86PmaxuInstruction[op.Size];

                Operand res = context.AddIntrinsic(maxInst, m, n);

                Instruction cmpInst = X86PcmpeqInstruction[op.Size];

                res = context.AddIntrinsic(cmpInst, res, m);

                Operand mask = X86GetAllElements(context, -1L);

                res = context.AddIntrinsic(Instruction.X86Pandn, res, mask);

                if (op.RegisterSize == RegisterSize.Simd64)
                {
                    res = context.VectorZeroUpper64(res);
                }

                context.Copy(GetVec(op.Rd), res);
            }
            else
            {
                EmitCmpOp(context, (op1, op2) => context.ICompareGreaterUI(op1, op2), scalar: false);
            }
        }

        public static void Cmhs_S(EmitterContext context)
        {
            EmitCmpOp(context, (op1, op2) => context.ICompareGreaterOrEqualUI(op1, op2), scalar: true);
        }

        public static void Cmhs_V(EmitterContext context)
        {
            OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

            if (Optimizations.UseSse41 && op.Size < 3)
            {
                Operand n = GetVec(op.Rn);
                Operand m = GetVec(op.Rm);

                Instruction maxInst = X86PmaxuInstruction[op.Size];

                Operand res = context.AddIntrinsic(maxInst, n, m);

                Instruction cmpInst = X86PcmpeqInstruction[op.Size];

                res = context.AddIntrinsic(cmpInst, res, n);

                if (op.RegisterSize == RegisterSize.Simd64)
                {
                    res = context.VectorZeroUpper64(res);
                }

                context.Copy(GetVec(op.Rd), res);
            }
            else
            {
                EmitCmpOp(context, (op1, op2) => context.ICompareGreaterOrEqualUI(op1, op2), scalar: false);
            }
        }

        public static void Cmle_S(EmitterContext context)
        {
            EmitCmpOp(context, (op1, op2) => context.ICompareLessOrEqual(op1, op2), scalar: true);
        }

        public static void Cmle_V(EmitterContext context)
        {
            if (Optimizations.UseSse42)
            {
                OpCodeSimd op = (OpCodeSimd)context.CurrOp;

                Operand n = GetVec(op.Rn);

                Instruction cmpInst = X86PcmpgtInstruction[op.Size];

                Operand res = context.AddIntrinsic(cmpInst, n, context.VectorZero());

                Operand mask = X86GetAllElements(context, -1L);

                res = context.AddIntrinsic(Instruction.X86Pandn, res, mask);

                if (op.RegisterSize == RegisterSize.Simd64)
                {
                    res = context.VectorZeroUpper64(res);
                }

                context.Copy(GetVec(op.Rd), res);
            }
            else
            {
                EmitCmpOp(context, (op1, op2) => context.ICompareLessOrEqual(op1, op2), scalar: false);
            }
        }

        public static void Cmlt_S(EmitterContext context)
        {
            EmitCmpOp(context, (op1, op2) => context.ICompareLess(op1, op2), scalar: true);
        }

        public static void Cmlt_V(EmitterContext context)
        {
            if (Optimizations.UseSse42)
            {
                OpCodeSimd op = (OpCodeSimd)context.CurrOp;

                Operand n = GetVec(op.Rn);

                Instruction cmpInst = X86PcmpgtInstruction[op.Size];

                Operand res = context.AddIntrinsic(cmpInst, context.VectorZero(), n);

                if (op.RegisterSize == RegisterSize.Simd64)
                {
                    res = context.VectorZeroUpper64(res);
                }

                context.Copy(GetVec(op.Rd), res);
            }
            else
            {
                EmitCmpOp(context, (op1, op2) => context.ICompareLess(op1, op2), scalar: false);
            }
        }

        public static void Cmtst_S(EmitterContext context)
        {
            EmitCmtstOp(context, scalar: true);
        }

        public static void Cmtst_V(EmitterContext context)
        {
            EmitCmtstOp(context, scalar: false);
        }

        public static void Fccmp_S(EmitterContext context)
        {
            EmitFccmpOrFccmpe(context, signalNaNs: false);
        }

        public static void Fccmpe_S(EmitterContext context)
        {
            EmitFccmpOrFccmpe(context, signalNaNs: true);
        }

        public static void Fcmeq_S(EmitterContext context)
        {
            if (Optimizations.FastFP && Optimizations.UseSse2)
            {
                EmitCmpSseOrSse2OpF(context, CmpCondition.Equal, scalar: true);
            }
            else
            {
                EmitCmpOpF(context, nameof(SoftFloat32.FPCompareEQ), scalar: true);
            }
        }

        public static void Fcmeq_V(EmitterContext context)
        {
            if (Optimizations.FastFP && Optimizations.UseSse2)
            {
                EmitCmpSseOrSse2OpF(context, CmpCondition.Equal, scalar: false);
            }
            else
            {
                EmitCmpOpF(context, nameof(SoftFloat32.FPCompareEQ), scalar: false);
            }
        }

        public static void Fcmge_S(EmitterContext context)
        {
            if (Optimizations.FastFP && Optimizations.UseSse2)
            {
                EmitCmpSseOrSse2OpF(context, CmpCondition.GreaterThanOrEqual, scalar: true);
            }
            else
            {
                EmitCmpOpF(context, nameof(SoftFloat32.FPCompareGE), scalar: true);
            }
        }

        public static void Fcmge_V(EmitterContext context)
        {
            if (Optimizations.FastFP && Optimizations.UseSse2)
            {
                EmitCmpSseOrSse2OpF(context, CmpCondition.GreaterThanOrEqual, scalar: false);
            }
            else
            {
                EmitCmpOpF(context, nameof(SoftFloat32.FPCompareGE), scalar: false);
            }
        }

        public static void Fcmgt_S(EmitterContext context)
        {
            if (Optimizations.FastFP && Optimizations.UseSse2)
            {
                EmitCmpSseOrSse2OpF(context, CmpCondition.GreaterThan, scalar: true);
            }
            else
            {
                EmitCmpOpF(context, nameof(SoftFloat32.FPCompareGT), scalar: true);
            }
        }

        public static void Fcmgt_V(EmitterContext context)
        {
            if (Optimizations.FastFP && Optimizations.UseSse2)
            {
                EmitCmpSseOrSse2OpF(context, CmpCondition.GreaterThan, scalar: false);
            }
            else
            {
                EmitCmpOpF(context, nameof(SoftFloat32.FPCompareGT), scalar: false);
            }
        }

        public static void Fcmle_S(EmitterContext context)
        {
            if (Optimizations.FastFP && Optimizations.UseSse2)
            {
                EmitCmpSseOrSse2OpF(context, CmpCondition.GreaterThanOrEqual, scalar: true, isLeOrLt: true);
            }
            else
            {
                EmitCmpOpF(context, nameof(SoftFloat32.FPCompareLE), scalar: true);
            }
        }

        public static void Fcmle_V(EmitterContext context)
        {
            if (Optimizations.FastFP && Optimizations.UseSse2)
            {
                EmitCmpSseOrSse2OpF(context, CmpCondition.GreaterThanOrEqual, scalar: false, isLeOrLt: true);
            }
            else
            {
                EmitCmpOpF(context, nameof(SoftFloat32.FPCompareLE), scalar: false);
            }
        }

        public static void Fcmlt_S(EmitterContext context)
        {
            if (Optimizations.FastFP && Optimizations.UseSse2)
            {
                EmitCmpSseOrSse2OpF(context, CmpCondition.GreaterThan, scalar: true, isLeOrLt: true);
            }
            else
            {
                EmitCmpOpF(context, nameof(SoftFloat32.FPCompareLT), scalar: true);
            }
        }

        public static void Fcmlt_V(EmitterContext context)
        {
            if (Optimizations.FastFP && Optimizations.UseSse2)
            {
                EmitCmpSseOrSse2OpF(context, CmpCondition.GreaterThan, scalar: false, isLeOrLt: true);
            }
            else
            {
                EmitCmpOpF(context, nameof(SoftFloat32.FPCompareLT), scalar: false);
            }
        }

        public static void Fcmp_S(EmitterContext context)
        {
            EmitFcmpOrFcmpe(context, signalNaNs: false);
        }

        public static void Fcmpe_S(EmitterContext context)
        {
            EmitFcmpOrFcmpe(context, signalNaNs: true);
        }

        public static void EmitFccmpOrFccmpe(EmitterContext context, bool signalNaNs)
        {
            OpCodeSimdFcond op = (OpCodeSimdFcond)context.CurrOp;

            Operand lblTrue = Label();
            Operand lblEnd  = Label();

            context.BranchIfTrue(lblTrue, InstEmitFlowHelper.GetCondTrue(context, op.Cond));

            EmitSetNzcv(context, Const(op.Nzcv));

            context.Branch(lblEnd);

            context.MarkLabel(lblTrue);

            EmitFcmpOrFcmpe(context, signalNaNs);

            context.MarkLabel(lblEnd);
        }

        private static void EmitFcmpOrFcmpe(EmitterContext context, bool signalNaNs)
        {
            OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

            const int cmpOrdered = 7;

            bool cmpWithZero = !(op is OpCodeSimdFcond) ? op.Bit3 : false;

            if (Optimizations.FastFP && Optimizations.UseSse2)
            {
                Operand n = GetVec(op.Rn);
                Operand m = cmpWithZero ? context.VectorZero() : GetVec(op.Rm);

                Operand lblNaN = Label();
                Operand lblEnd = Label();

                if (op.Size == 0)
                {
                    Operand ordMask = context.AddIntrinsic(Instruction.X86Cmpss, n, m, Const(cmpOrdered));

                    Operand isOrdered = context.VectorExtract16(ordMask, Local(OperandType.I32), 0);

                    context.BranchIfFalse(lblNaN, isOrdered);

                    Operand cf = context.AddIntrinsicInt(Instruction.X86Comissge, n, m);
                    Operand zf = context.AddIntrinsicInt(Instruction.X86Comisseq, n, m);
                    Operand nf = context.AddIntrinsicInt(Instruction.X86Comisslt, n, m);

                    context.Copy(GetFlag(PState.VFlag), Const(0));
                    context.Copy(GetFlag(PState.CFlag), cf);
                    context.Copy(GetFlag(PState.ZFlag), zf);
                    context.Copy(GetFlag(PState.NFlag), nf);
                }
                else /* if (op.Size == 1) */
                {
                    Operand ordMask = context.AddIntrinsic(Instruction.X86Cmpsd, n, m, Const(cmpOrdered));

                    Operand isOrdered = context.VectorExtract16(ordMask, Local(OperandType.I32), 0);

                    context.BranchIfFalse(lblNaN, isOrdered);

                    Operand cf = context.AddIntrinsicInt(Instruction.X86Comisdge, n, m);
                    Operand zf = context.AddIntrinsicInt(Instruction.X86Comisdeq, n, m);
                    Operand nf = context.AddIntrinsicInt(Instruction.X86Comisdlt, n, m);

                    context.Copy(GetFlag(PState.VFlag), Const(0));
                    context.Copy(GetFlag(PState.CFlag), cf);
                    context.Copy(GetFlag(PState.ZFlag), zf);
                    context.Copy(GetFlag(PState.NFlag), nf);
                }

                context.Branch(lblEnd);

                context.MarkLabel(lblNaN);

                context.Copy(GetFlag(PState.VFlag), Const(1));
                context.Copy(GetFlag(PState.CFlag), Const(1));
                context.Copy(GetFlag(PState.ZFlag), Const(0));
                context.Copy(GetFlag(PState.NFlag), Const(0));

                context.MarkLabel(lblEnd);
            }
            else
            {
                OperandType type = op.Size != 0 ? OperandType.FP64 : OperandType.FP32;

                Operand ne = context.VectorExtract(GetVec(op.Rn), Local(type), 0);
                Operand me;

                if (cmpWithZero)
                {
                    me = op.Size == 0 ? ConstF(0f) : ConstF(0d);
                }
                else
                {
                    me = context.VectorExtract(GetVec(op.Rm), Local(type), 0);
                }

                Operand nzcv = EmitSoftFloatCall(context, nameof(SoftFloat32.FPCompare), ne, me, Const(signalNaNs));

                EmitSetNzcv(context, nzcv);
            }
        }

        private static void EmitSetNzcv(EmitterContext context, Operand nzcv)
        {
            Operand Extract(Operand value, int bit)
            {
                if (bit != 0)
                {
                    value = context.ShiftRightUI(value, Const(bit));
                }

                value = context.BitwiseAnd(value, Const(1));

                return value;
            }

            context.Copy(GetFlag(PState.VFlag), Extract(nzcv, 0));
            context.Copy(GetFlag(PState.CFlag), Extract(nzcv, 1));
            context.Copy(GetFlag(PState.ZFlag), Extract(nzcv, 2));
            context.Copy(GetFlag(PState.NFlag), Extract(nzcv, 3));
        }

        private static void EmitCmpOp(EmitterContext context, Func2I emitCmp, bool scalar)
        {
            OpCodeSimd op = (OpCodeSimd)context.CurrOp;

            Operand res = context.VectorZero();

            int elems = !scalar ? op.GetBytesCount() >> op.Size : 1;

            ulong szMask = ulong.MaxValue >> (64 - (8 << op.Size));

            for (int index = 0; index < elems; index++)
            {
                Operand ne = EmitVectorExtractSx(context, op.Rn, index, op.Size);
                Operand me;

                if (op is OpCodeSimdReg binOp)
                {
                    me = EmitVectorExtractSx(context, binOp.Rm, index, op.Size);
                }
                else
                {
                    me = Const(0L);
                }

                Operand isTrue = emitCmp(ne, me);

                Operand mask = context.ConditionalSelect(isTrue, Const(szMask), Const(0L));

                res = EmitVectorInsert(context, res, mask, index, op.Size);
            }

            context.Copy(GetVec(op.Rd), res);
        }

        private static void EmitCmtstOp(EmitterContext context, bool scalar)
        {
            OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

            Operand res = context.VectorZero();

            int elems = !scalar ? op.GetBytesCount() >> op.Size : 1;

            ulong szMask = ulong.MaxValue >> (64 - (8 << op.Size));

            for (int index = 0; index < elems; index++)
            {
                Operand ne = EmitVectorExtractZx(context, op.Rn, index, op.Size);
                Operand me = EmitVectorExtractZx(context, op.Rm, index, op.Size);

                Operand test = context.BitwiseAnd(ne, me);

                Operand isTrue = context.ICompareNotEqual(test, Const(0L));

                Operand mask = context.ConditionalSelect(isTrue, Const(szMask), Const(0L));

                res = EmitVectorInsert(context, res, mask, index, op.Size);
            }

            context.Copy(GetVec(op.Rd), res);
        }

        private static void EmitCmpOpF(EmitterContext context, string name, bool scalar)
        {
            OpCodeSimd op = (OpCodeSimd)context.CurrOp;

            Operand res = context.VectorZero();

            int sizeF = op.Size & 1;

            OperandType type = sizeF != 0 ? OperandType.FP64 : OperandType.FP32;

            int elems = !scalar ? op.GetBytesCount() >> sizeF + 2 : 1;

            for (int index = 0; index < elems; index++)
            {
                Operand ne = context.VectorExtract(GetVec(op.Rn), Local(type), index);
                Operand me;

                if (op is OpCodeSimdReg binOp)
                {
                    me = context.VectorExtract(GetVec(binOp.Rm), Local(type), index);
                }
                else
                {
                    me = sizeF == 0 ? ConstF(0f) : ConstF(0d);
                }

                Operand e = EmitSoftFloatCall(context, name, ne, me);

                res = context.VectorInsert(res, e, index);
            }

            context.Copy(GetVec(op.Rd), res);
        }

        private enum CmpCondition
        {
            Equal              = 0,
            GreaterThanOrEqual = 5,
            GreaterThan        = 6
        }

        private static void EmitCmpSseOrSse2OpF(
            EmitterContext context,
            CmpCondition cond,
            bool scalar,
            bool isLeOrLt = false)
        {
            OpCodeSimd op = (OpCodeSimd)context.CurrOp;

            Operand n = GetVec(op.Rn);
            Operand m = op is OpCodeSimdReg binOp ? GetVec(binOp.Rm) : context.VectorZero();

            int sizeF = op.Size & 1;

            if (sizeF == 0)
            {
                Instruction inst = scalar ? Instruction.X86Cmpss : Instruction.X86Cmpps;

                Operand res = isLeOrLt
                    ? context.AddIntrinsic(inst, m, n, Const((int)cond))
                    : context.AddIntrinsic(inst, n, m, Const((int)cond));

                if (scalar)
                {
                    res = context.VectorZeroUpper96(res);
                }
                else if (op.RegisterSize == RegisterSize.Simd64)
                {
                    res = context.VectorZeroUpper64(res);
                }

                context.Copy(GetVec(op.Rd), res);
            }
            else /* if (sizeF == 1) */
            {
                Instruction inst = scalar ? Instruction.X86Cmpsd : Instruction.X86Cmppd;

                Operand res = isLeOrLt
                    ? context.AddIntrinsic(inst, m, n, Const((int)cond))
                    : context.AddIntrinsic(inst, n, m, Const((int)cond));

                if (scalar)
                {
                    res = context.VectorZeroUpper64(res);
                }

                context.Copy(GetVec(op.Rd), res);
            }
        }
    }
}