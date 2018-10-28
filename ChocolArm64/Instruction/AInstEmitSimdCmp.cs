using ChocolArm64.Decoder;
using ChocolArm64.State;
using ChocolArm64.Translation;
using System;
using System.Reflection.Emit;
using System.Runtime.Intrinsics.X86;

using static ChocolArm64.Instruction.AInstEmitAluHelper;
using static ChocolArm64.Instruction.AInstEmitSimdHelper;

namespace ChocolArm64.Instruction
{
    static partial class AInstEmit
    {
        public static void Cmeq_S(AilEmitterCtx context)
        {
            EmitCmp(context, OpCodes.Beq_S, scalar: true);
        }

        public static void Cmeq_V(AilEmitterCtx context)
        {
            if (context.CurrOp is AOpCodeSimdReg op)
            {
                if (op.Size < 3 && AOptimizations.UseSse2)
                {
                    EmitSse2Op(context, nameof(Sse2.CompareEqual));
                }
                else if (op.Size == 3 && AOptimizations.UseSse41)
                {
                    EmitSse41Op(context, nameof(Sse41.CompareEqual));
                }
                else
                {
                    EmitCmp(context, OpCodes.Beq_S, scalar: false);
                }
            }
            else
            {
                EmitCmp(context, OpCodes.Beq_S, scalar: false);
            }
        }

        public static void Cmge_S(AilEmitterCtx context)
        {
            EmitCmp(context, OpCodes.Bge_S, scalar: true);
        }

        public static void Cmge_V(AilEmitterCtx context)
        {
            EmitCmp(context, OpCodes.Bge_S, scalar: false);
        }

        public static void Cmgt_S(AilEmitterCtx context)
        {
            EmitCmp(context, OpCodes.Bgt_S, scalar: true);
        }

        public static void Cmgt_V(AilEmitterCtx context)
        {
            if (context.CurrOp is AOpCodeSimdReg op)
            {
                if (op.Size < 3 && AOptimizations.UseSse2)
                {
                    EmitSse2Op(context, nameof(Sse2.CompareGreaterThan));
                }
                else if (op.Size == 3 && AOptimizations.UseSse42)
                {
                    EmitSse42Op(context, nameof(Sse42.CompareGreaterThan));
                }
                else
                {
                    EmitCmp(context, OpCodes.Bgt_S, scalar: false);
                }
            }
            else
            {
                EmitCmp(context, OpCodes.Bgt_S, scalar: false);
            }
        }

        public static void Cmhi_S(AilEmitterCtx context)
        {
            EmitCmp(context, OpCodes.Bgt_Un_S, scalar: true);
        }

        public static void Cmhi_V(AilEmitterCtx context)
        {
            EmitCmp(context, OpCodes.Bgt_Un_S, scalar: false);
        }

        public static void Cmhs_S(AilEmitterCtx context)
        {
            EmitCmp(context, OpCodes.Bge_Un_S, scalar: true);
        }

        public static void Cmhs_V(AilEmitterCtx context)
        {
            EmitCmp(context, OpCodes.Bge_Un_S, scalar: false);
        }

        public static void Cmle_S(AilEmitterCtx context)
        {
            EmitCmp(context, OpCodes.Ble_S, scalar: true);
        }

        public static void Cmle_V(AilEmitterCtx context)
        {
            EmitCmp(context, OpCodes.Ble_S, scalar: false);
        }

        public static void Cmlt_S(AilEmitterCtx context)
        {
            EmitCmp(context, OpCodes.Blt_S, scalar: true);
        }

        public static void Cmlt_V(AilEmitterCtx context)
        {
            EmitCmp(context, OpCodes.Blt_S, scalar: false);
        }

        public static void Cmtst_S(AilEmitterCtx context)
        {
            EmitCmtst(context, scalar: true);
        }

        public static void Cmtst_V(AilEmitterCtx context)
        {
            EmitCmtst(context, scalar: false);
        }

        public static void Fccmp_S(AilEmitterCtx context)
        {
            AOpCodeSimdFcond op = (AOpCodeSimdFcond)context.CurrOp;

            AilLabel lblTrue = new AilLabel();
            AilLabel lblEnd  = new AilLabel();

            context.EmitCondBranch(lblTrue, op.Cond);

            EmitSetNzcv(context, op.Nzcv);

            context.Emit(OpCodes.Br, lblEnd);

            context.MarkLabel(lblTrue);

            Fcmp_S(context);

            context.MarkLabel(lblEnd);
        }

        public static void Fccmpe_S(AilEmitterCtx context)
        {
            Fccmp_S(context);
        }

        public static void Fcmeq_S(AilEmitterCtx context)
        {
            if (context.CurrOp is AOpCodeSimdReg && AOptimizations.UseSse
                                                 && AOptimizations.UseSse2)
            {
                EmitScalarSseOrSse2OpF(context, nameof(Sse.CompareEqualScalar));
            }
            else
            {
                EmitScalarFcmp(context, OpCodes.Beq_S);
            }
        }

        public static void Fcmeq_V(AilEmitterCtx context)
        {
            if (context.CurrOp is AOpCodeSimdReg && AOptimizations.UseSse
                                                 && AOptimizations.UseSse2)
            {
                EmitVectorSseOrSse2OpF(context, nameof(Sse.CompareEqual));
            }
            else
            {
                EmitVectorFcmp(context, OpCodes.Beq_S);
            }
        }

        public static void Fcmge_S(AilEmitterCtx context)
        {
            if (context.CurrOp is AOpCodeSimdReg && AOptimizations.UseSse
                                                 && AOptimizations.UseSse2)
            {
                EmitScalarSseOrSse2OpF(context, nameof(Sse.CompareGreaterThanOrEqualScalar));
            }
            else
            {
                EmitScalarFcmp(context, OpCodes.Bge_S);
            }
        }

        public static void Fcmge_V(AilEmitterCtx context)
        {
            if (context.CurrOp is AOpCodeSimdReg && AOptimizations.UseSse
                                                 && AOptimizations.UseSse2)
            {
                EmitVectorSseOrSse2OpF(context, nameof(Sse.CompareGreaterThanOrEqual));
            }
            else
            {
                EmitVectorFcmp(context, OpCodes.Bge_S);
            }
        }

        public static void Fcmgt_S(AilEmitterCtx context)
        {
            if (context.CurrOp is AOpCodeSimdReg && AOptimizations.UseSse
                                                 && AOptimizations.UseSse2)
            {
                EmitScalarSseOrSse2OpF(context, nameof(Sse.CompareGreaterThanScalar));
            }
            else
            {
                EmitScalarFcmp(context, OpCodes.Bgt_S);
            }
        }

        public static void Fcmgt_V(AilEmitterCtx context)
        {
            if (context.CurrOp is AOpCodeSimdReg && AOptimizations.UseSse
                                                 && AOptimizations.UseSse2)
            {
                EmitVectorSseOrSse2OpF(context, nameof(Sse.CompareGreaterThan));
            }
            else
            {
                EmitVectorFcmp(context, OpCodes.Bgt_S);
            }
        }

        public static void Fcmle_S(AilEmitterCtx context)
        {
            EmitScalarFcmp(context, OpCodes.Ble_S);
        }

        public static void Fcmle_V(AilEmitterCtx context)
        {
            EmitVectorFcmp(context, OpCodes.Ble_S);
        }

        public static void Fcmlt_S(AilEmitterCtx context)
        {
            EmitScalarFcmp(context, OpCodes.Blt_S);
        }

        public static void Fcmlt_V(AilEmitterCtx context)
        {
            EmitVectorFcmp(context, OpCodes.Blt_S);
        }

        public static void Fcmp_S(AilEmitterCtx context)
        {
            AOpCodeSimdReg op = (AOpCodeSimdReg)context.CurrOp;

            bool cmpWithZero = !(op is AOpCodeSimdFcond) ? op.Bit3 : false;

            //Handle NaN case.
            //If any number is NaN, then NZCV = 0011.
            if (cmpWithZero)
            {
                EmitNaNCheck(context, op.Rn);
            }
            else
            {
                EmitNaNCheck(context, op.Rn);
                EmitNaNCheck(context, op.Rm);

                context.Emit(OpCodes.Or);
            }

            AilLabel lblNaN = new AilLabel();
            AilLabel lblEnd = new AilLabel();

            context.Emit(OpCodes.Brtrue_S, lblNaN);

            void EmitLoadOpers()
            {
                EmitVectorExtractF(context, op.Rn, 0, op.Size);

                if (cmpWithZero)
                {
                    if (op.Size == 0)
                    {
                        context.EmitLdc_R4(0f);
                    }
                    else /* if (Op.Size == 1) */
                    {
                        context.EmitLdc_R8(0d);
                    }
                }
                else
                {
                    EmitVectorExtractF(context, op.Rm, 0, op.Size);
                }
            }

            //Z = Rn == Rm
            EmitLoadOpers();

            context.Emit(OpCodes.Ceq);
            context.Emit(OpCodes.Dup);

            context.EmitStflg((int)ApState.ZBit);

            //C = Rn >= Rm
            EmitLoadOpers();

            context.Emit(OpCodes.Cgt);
            context.Emit(OpCodes.Or);

            context.EmitStflg((int)ApState.CBit);

            //N = Rn < Rm
            EmitLoadOpers();

            context.Emit(OpCodes.Clt);

            context.EmitStflg((int)ApState.NBit);

            //V = 0
            context.EmitLdc_I4(0);

            context.EmitStflg((int)ApState.VBit);

            context.Emit(OpCodes.Br_S, lblEnd);

            context.MarkLabel(lblNaN);

            EmitSetNzcv(context, 0b0011);

            context.MarkLabel(lblEnd);
        }

        public static void Fcmpe_S(AilEmitterCtx context)
        {
            Fcmp_S(context);
        }

        private static void EmitNaNCheck(AilEmitterCtx context, int reg)
        {
            IaOpCodeSimd op = (IaOpCodeSimd)context.CurrOp;

            EmitVectorExtractF(context, reg, 0, op.Size);

            if (op.Size == 0)
            {
                context.EmitCall(typeof(float), nameof(float.IsNaN));
            }
            else if (op.Size == 1)
            {
                context.EmitCall(typeof(double), nameof(double.IsNaN));
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        private static void EmitCmp(AilEmitterCtx context, OpCode ilOp, bool scalar)
        {
            AOpCodeSimd op = (AOpCodeSimd)context.CurrOp;

            int bytes = op.GetBitsCount() >> 3;
            int elems = !scalar ? bytes >> op.Size : 1;

            ulong szMask = ulong.MaxValue >> (64 - (8 << op.Size));

            for (int index = 0; index < elems; index++)
            {
                EmitVectorExtractSx(context, op.Rn, index, op.Size);

                if (op is AOpCodeSimdReg binOp)
                {
                    EmitVectorExtractSx(context, binOp.Rm, index, op.Size);
                }
                else
                {
                    context.EmitLdc_I8(0L);
                }

                AilLabel lblTrue = new AilLabel();
                AilLabel lblEnd  = new AilLabel();

                context.Emit(ilOp, lblTrue);

                EmitVectorInsert(context, op.Rd, index, op.Size, 0);

                context.Emit(OpCodes.Br_S, lblEnd);

                context.MarkLabel(lblTrue);

                EmitVectorInsert(context, op.Rd, index, op.Size, (long)szMask);

                context.MarkLabel(lblEnd);
            }

            if ((op.RegisterSize == ARegisterSize.Simd64) || scalar)
            {
                EmitVectorZeroUpper(context, op.Rd);
            }
        }

        private static void EmitCmtst(AilEmitterCtx context, bool scalar)
        {
            AOpCodeSimdReg op = (AOpCodeSimdReg)context.CurrOp;

            int bytes = op.GetBitsCount() >> 3;
            int elems = !scalar ? bytes >> op.Size : 1;

            ulong szMask = ulong.MaxValue >> (64 - (8 << op.Size));

            for (int index = 0; index < elems; index++)
            {
                EmitVectorExtractZx(context, op.Rn, index, op.Size);
                EmitVectorExtractZx(context, op.Rm, index, op.Size);

                AilLabel lblTrue = new AilLabel();
                AilLabel lblEnd  = new AilLabel();

                context.Emit(OpCodes.And);

                context.EmitLdc_I8(0L);

                context.Emit(OpCodes.Bne_Un_S, lblTrue);

                EmitVectorInsert(context, op.Rd, index, op.Size, 0);

                context.Emit(OpCodes.Br_S, lblEnd);

                context.MarkLabel(lblTrue);

                EmitVectorInsert(context, op.Rd, index, op.Size, (long)szMask);

                context.MarkLabel(lblEnd);
            }

            if ((op.RegisterSize == ARegisterSize.Simd64) || scalar)
            {
                EmitVectorZeroUpper(context, op.Rd);
            }
        }

        private static void EmitScalarFcmp(AilEmitterCtx context, OpCode ilOp)
        {
            EmitFcmp(context, ilOp, 0, scalar: true);
        }

        private static void EmitVectorFcmp(AilEmitterCtx context, OpCode ilOp)
        {
            AOpCodeSimd op = (AOpCodeSimd)context.CurrOp;

            int sizeF = op.Size & 1;

            int bytes = op.GetBitsCount() >> 3;
            int elems = bytes >> sizeF + 2;

            for (int index = 0; index < elems; index++)
            {
                EmitFcmp(context, ilOp, index, scalar: false);
            }

            if (op.RegisterSize == ARegisterSize.Simd64)
            {
                EmitVectorZeroUpper(context, op.Rd);
            }
        }

        private static void EmitFcmp(AilEmitterCtx context, OpCode ilOp, int index, bool scalar)
        {
            AOpCodeSimd op = (AOpCodeSimd)context.CurrOp;

            int sizeF = op.Size & 1;

            ulong szMask = ulong.MaxValue >> (64 - (32 << sizeF));

            EmitVectorExtractF(context, op.Rn, index, sizeF);

            if (op is AOpCodeSimdReg binOp)
            {
                EmitVectorExtractF(context, binOp.Rm, index, sizeF);
            }
            else if (sizeF == 0)
            {
                context.EmitLdc_R4(0f);
            }
            else /* if (SizeF == 1) */
            {
                context.EmitLdc_R8(0d);
            }

            AilLabel lblTrue = new AilLabel();
            AilLabel lblEnd  = new AilLabel();

            context.Emit(ilOp, lblTrue);

            if (scalar)
            {
                EmitVectorZeroAll(context, op.Rd);
            }
            else
            {
                EmitVectorInsert(context, op.Rd, index, sizeF + 2, 0);
            }

            context.Emit(OpCodes.Br_S, lblEnd);

            context.MarkLabel(lblTrue);

            if (scalar)
            {
                EmitVectorInsert(context, op.Rd, index, 3, (long)szMask);

                EmitVectorZeroUpper(context, op.Rd);
            }
            else
            {
                EmitVectorInsert(context, op.Rd, index, sizeF + 2, (long)szMask);
            }

            context.MarkLabel(lblEnd);
        }
    }
}
