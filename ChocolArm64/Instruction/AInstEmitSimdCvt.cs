using ChocolArm64.Decoder;
using ChocolArm64.State;
using ChocolArm64.Translation;
using System;
using System.Reflection.Emit;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

using static ChocolArm64.Instruction.AInstEmitSimdHelper;

namespace ChocolArm64.Instruction
{
    internal static partial class AInstEmit
    {
        public static void Fcvt_S(AILEmitterCtx context)
        {
            AOpCodeSimd op = (AOpCodeSimd)context.CurrOp;

            if (AOptimizations.UseSse2)
            {
                if (op.Size == 1 && op.Opc == 0)
                {
                    //Double -> Single.
                    AVectorHelper.EmitCall(context, nameof(AVectorHelper.VectorSingleZero));

                    EmitLdvecWithCastToDouble(context, op.Rn);

                    Type[] types = new Type[] { typeof(Vector128<float>), typeof(Vector128<double>) };

                    context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.ConvertScalarToVector128Single), types));

                    context.EmitStvec(op.Rd);
                }
                else if (op.Size == 0 && op.Opc == 1)
                {
                    //Single -> Double.
                    AVectorHelper.EmitCall(context, nameof(AVectorHelper.VectorDoubleZero));

                    context.EmitLdvec(op.Rn);

                    Type[] types = new Type[] { typeof(Vector128<double>), typeof(Vector128<float>) };

                    context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.ConvertScalarToVector128Double), types));

                    EmitStvecWithCastFromDouble(context, op.Rd);
                }
                else
                {
                    //Invalid encoding.
                    throw new InvalidOperationException();
                }
            }
            else
            {
                EmitVectorExtractF(context, op.Rn, 0, op.Size);

                EmitFloatCast(context, op.Opc);

                EmitScalarSetF(context, op.Rd, op.Opc);
            }
        }

        public static void Fcvtas_Gp(AILEmitterCtx context)
        {
            EmitFcvt_s_Gp(context, () => EmitRoundMathCall(context, MidpointRounding.AwayFromZero));
        }

        public static void Fcvtau_Gp(AILEmitterCtx context)
        {
            EmitFcvt_u_Gp(context, () => EmitRoundMathCall(context, MidpointRounding.AwayFromZero));
        }

        public static void Fcvtl_V(AILEmitterCtx context)
        {
            AOpCodeSimd op = (AOpCodeSimd)context.CurrOp;

            int sizeF = op.Size & 1;

            int elems = 4 >> sizeF;

            int part = op.RegisterSize == ARegisterSize.Simd128 ? elems : 0;

            for (int index = 0; index < elems; index++)
            {
                if (sizeF == 0)
                {
                    EmitVectorExtractZx(context, op.Rn, part + index, 1);
                    context.Emit(OpCodes.Conv_U2);

                    context.EmitLdarg(ATranslatedSub.StateArgIdx);

                    context.EmitCall(typeof(ASoftFloat16_32), nameof(ASoftFloat16_32.FpConvert));
                }
                else /* if (SizeF == 1) */
                {
                    EmitVectorExtractF(context, op.Rn, part + index, 0);

                    context.Emit(OpCodes.Conv_R8);
                }

                EmitVectorInsertTmpF(context, index, sizeF);
            }

            context.EmitLdvectmp();
            context.EmitStvec(op.Rd);
        }

        public static void Fcvtms_Gp(AILEmitterCtx context)
        {
            EmitFcvt_s_Gp(context, () => EmitUnaryMathCall(context, nameof(Math.Floor)));
        }

        public static void Fcvtmu_Gp(AILEmitterCtx context)
        {
            EmitFcvt_u_Gp(context, () => EmitUnaryMathCall(context, nameof(Math.Floor)));
        }

        public static void Fcvtn_V(AILEmitterCtx context)
        {
            AOpCodeSimd op = (AOpCodeSimd)context.CurrOp;

            int sizeF = op.Size & 1;

            int elems = 4 >> sizeF;

            int part = op.RegisterSize == ARegisterSize.Simd128 ? elems : 0;

            if (part != 0)
            {
                context.EmitLdvec(op.Rd);
                context.EmitStvectmp();
            }

            for (int index = 0; index < elems; index++)
            {
                EmitVectorExtractF(context, op.Rn, index, sizeF);

                if (sizeF == 0)
                {
                    context.EmitLdarg(ATranslatedSub.StateArgIdx);

                    context.EmitCall(typeof(ASoftFloat32_16), nameof(ASoftFloat32_16.FpConvert));

                    context.Emit(OpCodes.Conv_U8);
                    EmitVectorInsertTmp(context, part + index, 1);
                }
                else /* if (SizeF == 1) */
                {
                    context.Emit(OpCodes.Conv_R4);

                    EmitVectorInsertTmpF(context, part + index, 0);
                }
            }

            context.EmitLdvectmp();
            context.EmitStvec(op.Rd);

            if (part == 0)
            {
                EmitVectorZeroUpper(context, op.Rd);
            }
        }

        public static void Fcvtns_S(AILEmitterCtx context)
        {
            EmitFcvtn(context, true, true);
        }

        public static void Fcvtns_V(AILEmitterCtx context)
        {
            EmitFcvtn(context, true, false);
        }

        public static void Fcvtnu_S(AILEmitterCtx context)
        {
            EmitFcvtn(context, false, true);
        }

        public static void Fcvtnu_V(AILEmitterCtx context)
        {
            EmitFcvtn(context, false, false);
        }

        public static void Fcvtps_Gp(AILEmitterCtx context)
        {
            EmitFcvt_s_Gp(context, () => EmitUnaryMathCall(context, nameof(Math.Ceiling)));
        }

        public static void Fcvtpu_Gp(AILEmitterCtx context)
        {
            EmitFcvt_u_Gp(context, () => EmitUnaryMathCall(context, nameof(Math.Ceiling)));
        }

        public static void Fcvtzs_Gp(AILEmitterCtx context)
        {
            EmitFcvt_s_Gp(context, () => { });
        }

        public static void Fcvtzs_Gp_Fix(AILEmitterCtx context)
        {
            EmitFcvtzs_Gp_Fix(context);
        }

        public static void Fcvtzs_S(AILEmitterCtx context)
        {
            EmitScalarFcvtzs(context);
        }

        public static void Fcvtzs_V(AILEmitterCtx context)
        {
            EmitVectorFcvtzs(context);
        }

        public static void Fcvtzu_Gp(AILEmitterCtx context)
        {
            EmitFcvt_u_Gp(context, () => { });
        }

        public static void Fcvtzu_Gp_Fix(AILEmitterCtx context)
        {
            EmitFcvtzu_Gp_Fix(context);
        }

        public static void Fcvtzu_S(AILEmitterCtx context)
        {
            EmitScalarFcvtzu(context);
        }

        public static void Fcvtzu_V(AILEmitterCtx context)
        {
            EmitVectorFcvtzu(context);
        }

        public static void Scvtf_Gp(AILEmitterCtx context)
        {
            AOpCodeSimdCvt op = (AOpCodeSimdCvt)context.CurrOp;

            context.EmitLdintzr(op.Rn);

            if (context.CurrOp.RegisterSize == ARegisterSize.Int32)
            {
                context.Emit(OpCodes.Conv_U4);
            }

            EmitFloatCast(context, op.Size);

            EmitScalarSetF(context, op.Rd, op.Size);
        }

        public static void Scvtf_S(AILEmitterCtx context)
        {
            AOpCodeSimd op = (AOpCodeSimd)context.CurrOp;

            EmitVectorExtractSx(context, op.Rn, 0, op.Size + 2);

            EmitFloatCast(context, op.Size);

            EmitScalarSetF(context, op.Rd, op.Size);
        }

        public static void Scvtf_V(AILEmitterCtx context)
        {
            EmitVectorCvtf(context, true);
        }

        public static void Ucvtf_Gp(AILEmitterCtx context)
        {
            AOpCodeSimdCvt op = (AOpCodeSimdCvt)context.CurrOp;

            context.EmitLdintzr(op.Rn);

            if (context.CurrOp.RegisterSize == ARegisterSize.Int32)
            {
                context.Emit(OpCodes.Conv_U4);
            }

            context.Emit(OpCodes.Conv_R_Un);

            EmitFloatCast(context, op.Size);

            EmitScalarSetF(context, op.Rd, op.Size);
        }

        public static void Ucvtf_S(AILEmitterCtx context)
        {
            AOpCodeSimd op = (AOpCodeSimd)context.CurrOp;

            EmitVectorExtractZx(context, op.Rn, 0, op.Size + 2);

            context.Emit(OpCodes.Conv_R_Un);

            EmitFloatCast(context, op.Size);

            EmitScalarSetF(context, op.Rd, op.Size);
        }

        public static void Ucvtf_V(AILEmitterCtx context)
        {
            EmitVectorCvtf(context, false);
        }

        private static int GetFBits(AILEmitterCtx context)
        {
            if (context.CurrOp is AOpCodeSimdShImm op)
            {
                return GetImmShr(op);
            }

            return 0;
        }

        private static void EmitFloatCast(AILEmitterCtx context, int size)
        {
            if (size == 0)
            {
                context.Emit(OpCodes.Conv_R4);
            }
            else if (size == 1)
            {
                context.Emit(OpCodes.Conv_R8);
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(size));
            }
        }

        private static void EmitFcvtn(AILEmitterCtx context, bool signed, bool scalar)
        {
            AOpCodeSimd op = (AOpCodeSimd)context.CurrOp;

            int sizeF = op.Size & 1;
            int sizeI = sizeF + 2;

            int bytes = op.GetBitsCount() >> 3;
            int elems = !scalar ? bytes >> sizeI : 1;

            if (scalar && sizeF == 0)
            {
                EmitVectorZeroLowerTmp(context);
            }

            for (int index = 0; index < elems; index++)
            {
                EmitVectorExtractF(context, op.Rn, index, sizeF);

                EmitRoundMathCall(context, MidpointRounding.ToEven);

                if (sizeF == 0)
                {
                    AVectorHelper.EmitCall(context, signed
                        ? nameof(AVectorHelper.SatF32ToS32)
                        : nameof(AVectorHelper.SatF32ToU32));

                    context.Emit(OpCodes.Conv_U8);
                }
                else /* if (SizeF == 1) */
                {
                    AVectorHelper.EmitCall(context, signed
                        ? nameof(AVectorHelper.SatF64ToS64)
                        : nameof(AVectorHelper.SatF64ToU64));
                }

                EmitVectorInsertTmp(context, index, sizeI);
            }

            context.EmitLdvectmp();
            context.EmitStvec(op.Rd);

            if (op.RegisterSize == ARegisterSize.Simd64 || scalar)
            {
                EmitVectorZeroUpper(context, op.Rd);
            }
        }

        private static void EmitFcvt_s_Gp(AILEmitterCtx context, Action emit)
        {
            EmitFcvt___Gp(context, emit, true);
        }

        private static void EmitFcvt_u_Gp(AILEmitterCtx context, Action emit)
        {
            EmitFcvt___Gp(context, emit, false);
        }

        private static void EmitFcvt___Gp(AILEmitterCtx context, Action emit, bool signed)
        {
            AOpCodeSimdCvt op = (AOpCodeSimdCvt)context.CurrOp;

            EmitVectorExtractF(context, op.Rn, 0, op.Size);

            emit();

            if (signed)
            {
                EmitScalarFcvts(context, op.Size, 0);
            }
            else
            {
                EmitScalarFcvtu(context, op.Size, 0);
            }

            if (context.CurrOp.RegisterSize == ARegisterSize.Int32)
            {
                context.Emit(OpCodes.Conv_U8);
            }

            context.EmitStintzr(op.Rd);
        }

        private static void EmitFcvtzs_Gp_Fix(AILEmitterCtx context)
        {
            EmitFcvtz__Gp_Fix(context, true);
        }

        private static void EmitFcvtzu_Gp_Fix(AILEmitterCtx context)
        {
            EmitFcvtz__Gp_Fix(context, false);
        }

        private static void EmitFcvtz__Gp_Fix(AILEmitterCtx context, bool signed)
        {
            AOpCodeSimdCvt op = (AOpCodeSimdCvt)context.CurrOp;

            EmitVectorExtractF(context, op.Rn, 0, op.Size);

            if (signed)
            {
                EmitScalarFcvts(context, op.Size, op.FBits);
            }
            else
            {
                EmitScalarFcvtu(context, op.Size, op.FBits);
            }

            if (context.CurrOp.RegisterSize == ARegisterSize.Int32)
            {
                context.Emit(OpCodes.Conv_U8);
            }

            context.EmitStintzr(op.Rd);
        }

        private static void EmitVectorScvtf(AILEmitterCtx context)
        {
            EmitVectorCvtf(context, true);
        }

        private static void EmitVectorUcvtf(AILEmitterCtx context)
        {
            EmitVectorCvtf(context, false);
        }

        private static void EmitVectorCvtf(AILEmitterCtx context, bool signed)
        {
            AOpCodeSimd op = (AOpCodeSimd)context.CurrOp;

            int sizeF = op.Size & 1;
            int sizeI = sizeF + 2;

            int fBits = GetFBits(context);

            int bytes = op.GetBitsCount() >> 3;
            int elems = bytes >> sizeI;

            for (int index = 0; index < elems; index++)
            {
                EmitVectorExtract(context, op.Rn, index, sizeI, signed);

                if (!signed)
                {
                    context.Emit(OpCodes.Conv_R_Un);
                }

                context.Emit(sizeF == 0
                    ? OpCodes.Conv_R4
                    : OpCodes.Conv_R8);

                EmitI2fFBitsMul(context, sizeF, fBits);

                EmitVectorInsertF(context, op.Rd, index, sizeF);
            }

            if (op.RegisterSize == ARegisterSize.Simd64)
            {
                EmitVectorZeroUpper(context, op.Rd);
            }
        }

        private static void EmitScalarFcvtzs(AILEmitterCtx context)
        {
            EmitScalarFcvtz(context, true);
        }

        private static void EmitScalarFcvtzu(AILEmitterCtx context)
        {
            EmitScalarFcvtz(context, false);
        }

        private static void EmitScalarFcvtz(AILEmitterCtx context, bool signed)
        {
            AOpCodeSimd op = (AOpCodeSimd)context.CurrOp;

            int sizeF = op.Size & 1;
            int sizeI = sizeF + 2;

            int fBits = GetFBits(context);

            EmitVectorExtractF(context, op.Rn, 0, sizeF);

            EmitF2iFBitsMul(context, sizeF, fBits);

            if (sizeF == 0)
            {
                AVectorHelper.EmitCall(context, signed
                    ? nameof(AVectorHelper.SatF32ToS32)
                    : nameof(AVectorHelper.SatF32ToU32));
            }
            else /* if (SizeF == 1) */
            {
                AVectorHelper.EmitCall(context, signed
                    ? nameof(AVectorHelper.SatF64ToS64)
                    : nameof(AVectorHelper.SatF64ToU64));
            }

            if (sizeF == 0)
            {
                context.Emit(OpCodes.Conv_U8);
            }

            EmitScalarSet(context, op.Rd, sizeI);
        }

        private static void EmitVectorFcvtzs(AILEmitterCtx context)
        {
            EmitVectorFcvtz(context, true);
        }

        private static void EmitVectorFcvtzu(AILEmitterCtx context)
        {
            EmitVectorFcvtz(context, false);
        }

        private static void EmitVectorFcvtz(AILEmitterCtx context, bool signed)
        {
            AOpCodeSimd op = (AOpCodeSimd)context.CurrOp;

            int sizeF = op.Size & 1;
            int sizeI = sizeF + 2;

            int fBits = GetFBits(context);

            int bytes = op.GetBitsCount() >> 3;
            int elems = bytes >> sizeI;

            for (int index = 0; index < elems; index++)
            {
                EmitVectorExtractF(context, op.Rn, index, sizeF);

                EmitF2iFBitsMul(context, sizeF, fBits);

                if (sizeF == 0)
                {
                    AVectorHelper.EmitCall(context, signed
                        ? nameof(AVectorHelper.SatF32ToS32)
                        : nameof(AVectorHelper.SatF32ToU32));
                }
                else /* if (SizeF == 1) */
                {
                    AVectorHelper.EmitCall(context, signed
                        ? nameof(AVectorHelper.SatF64ToS64)
                        : nameof(AVectorHelper.SatF64ToU64));
                }

                if (sizeF == 0)
                {
                    context.Emit(OpCodes.Conv_U8);
                }

                EmitVectorInsert(context, op.Rd, index, sizeI);
            }

            if (op.RegisterSize == ARegisterSize.Simd64)
            {
                EmitVectorZeroUpper(context, op.Rd);
            }
        }

        private static void EmitScalarFcvts(AILEmitterCtx context, int size, int fBits)
        {
            if (size < 0 || size > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(size));
            }

            EmitF2iFBitsMul(context, size, fBits);

            if (context.CurrOp.RegisterSize == ARegisterSize.Int32)
            {
                if (size == 0)
                {
                    AVectorHelper.EmitCall(context, nameof(AVectorHelper.SatF32ToS32));
                }
                else /* if (Size == 1) */
                {
                    AVectorHelper.EmitCall(context, nameof(AVectorHelper.SatF64ToS32));
                }
            }
            else
            {
                if (size == 0)
                {
                    AVectorHelper.EmitCall(context, nameof(AVectorHelper.SatF32ToS64));
                }
                else /* if (Size == 1) */
                {
                    AVectorHelper.EmitCall(context, nameof(AVectorHelper.SatF64ToS64));
                }
            }
        }

        private static void EmitScalarFcvtu(AILEmitterCtx context, int size, int fBits)
        {
            if (size < 0 || size > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(size));
            }

            EmitF2iFBitsMul(context, size, fBits);

            if (context.CurrOp.RegisterSize == ARegisterSize.Int32)
            {
                if (size == 0)
                {
                    AVectorHelper.EmitCall(context, nameof(AVectorHelper.SatF32ToU32));
                }
                else /* if (Size == 1) */
                {
                    AVectorHelper.EmitCall(context, nameof(AVectorHelper.SatF64ToU32));
                }
            }
            else
            {
                if (size == 0)
                {
                    AVectorHelper.EmitCall(context, nameof(AVectorHelper.SatF32ToU64));
                }
                else /* if (Size == 1) */
                {
                    AVectorHelper.EmitCall(context, nameof(AVectorHelper.SatF64ToU64));
                }
            }
        }

        private static void EmitF2iFBitsMul(AILEmitterCtx context, int size, int fBits)
        {
            if (fBits != 0)
            {
                if (size == 0)
                {
                    context.EmitLdc_R4(MathF.Pow(2f, fBits));
                }
                else if (size == 1)
                {
                    context.EmitLdc_R8(Math.Pow(2d, fBits));
                }
                else
                {
                    throw new ArgumentOutOfRangeException(nameof(size));
                }

                context.Emit(OpCodes.Mul);
            }
        }

        private static void EmitI2fFBitsMul(AILEmitterCtx context, int size, int fBits)
        {
            if (fBits != 0)
            {
                if (size == 0)
                {
                    context.EmitLdc_R4(1f / MathF.Pow(2f, fBits));
                }
                else if (size == 1)
                {
                    context.EmitLdc_R8(1d / Math.Pow(2d, fBits));
                }
                else
                {
                    throw new ArgumentOutOfRangeException(nameof(size));
                }

                context.Emit(OpCodes.Mul);
            }
        }
    }
}
