// https://github.com/intel/ARM_NEON_2_x86_SSE/blob/master/NEON_2_SSE.h

using ChocolArm64.Decoder;
using ChocolArm64.State;
using ChocolArm64.Translation;
using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

using static ChocolArm64.Instruction.AInstEmitSimdHelper;

namespace ChocolArm64.Instruction
{
    internal static partial class AInstEmit
    {
        public static void Abs_S(AILEmitterCtx context)
        {
            EmitScalarUnaryOpSx(context, () => EmitAbs(context));
        }

        public static void Abs_V(AILEmitterCtx context)
        {
            EmitVectorUnaryOpSx(context, () => EmitAbs(context));
        }

        public static void Add_S(AILEmitterCtx context)
        {
            EmitScalarBinaryOpZx(context, () => context.Emit(OpCodes.Add));
        }

        public static void Add_V(AILEmitterCtx context)
        {
            if (AOptimizations.UseSse2)
                EmitSse2Op(context, nameof(Sse2.Add));
            else
                EmitVectorBinaryOpZx(context, () => context.Emit(OpCodes.Add));
        }

        public static void Addhn_V(AILEmitterCtx context)
        {
            EmitHighNarrow(context, () => context.Emit(OpCodes.Add), false);
        }

        public static void Addp_S(AILEmitterCtx context)
        {
            AOpCodeSimd op = (AOpCodeSimd)context.CurrOp;

            EmitVectorExtractZx(context, op.Rn, 0, op.Size);
            EmitVectorExtractZx(context, op.Rn, 1, op.Size);

            context.Emit(OpCodes.Add);

            EmitScalarSet(context, op.Rd, op.Size);
        }

        public static void Addp_V(AILEmitterCtx context)
        {
            EmitVectorPairwiseOpZx(context, () => context.Emit(OpCodes.Add));
        }

        public static void Addv_V(AILEmitterCtx context)
        {
            AOpCodeSimd op = (AOpCodeSimd)context.CurrOp;

            int bytes = op.GetBitsCount() >> 3;
            int elems = bytes >> op.Size;

            EmitVectorExtractZx(context, op.Rn, 0, op.Size);

            for (int index = 1; index < elems; index++)
            {
                EmitVectorExtractZx(context, op.Rn, index, op.Size);

                context.Emit(OpCodes.Add);
            }

            EmitScalarSet(context, op.Rd, op.Size);
        }

        public static void Cls_V(AILEmitterCtx context)
        {
            AOpCodeSimd op = (AOpCodeSimd)context.CurrOp;

            int bytes = op.GetBitsCount() >> 3;
            int elems = bytes >> op.Size;

            int eSize = 8 << op.Size;

            for (int index = 0; index < elems; index++)
            {
                EmitVectorExtractZx(context, op.Rn, index, op.Size);

                context.EmitLdc_I4(eSize);

                ASoftFallback.EmitCall(context, nameof(ASoftFallback.CountLeadingSigns));

                EmitVectorInsert(context, op.Rd, index, op.Size);
            }

            if (op.RegisterSize == ARegisterSize.Simd64) EmitVectorZeroUpper(context, op.Rd);
        }

        public static void Clz_V(AILEmitterCtx context)
        {
            AOpCodeSimd op = (AOpCodeSimd)context.CurrOp;

            int bytes = op.GetBitsCount() >> 3;
            int elems = bytes >> op.Size;

            int eSize = 8 << op.Size;

            for (int index = 0; index < elems; index++)
            {
                EmitVectorExtractZx(context, op.Rn, index, op.Size);

                if (Lzcnt.IsSupported && eSize == 32)
                {
                    context.Emit(OpCodes.Conv_U4);

                    context.EmitCall(typeof(Lzcnt).GetMethod(nameof(Lzcnt.LeadingZeroCount), new Type[] { typeof(uint) }));

                    context.Emit(OpCodes.Conv_U8);
                }
                else
                {
                    context.EmitLdc_I4(eSize);

                    ASoftFallback.EmitCall(context, nameof(ASoftFallback.CountLeadingZeros));
                }

                EmitVectorInsert(context, op.Rd, index, op.Size);
            }

            if (op.RegisterSize == ARegisterSize.Simd64) EmitVectorZeroUpper(context, op.Rd);
        }

        public static void Cnt_V(AILEmitterCtx context)
        {
            AOpCodeSimd op = (AOpCodeSimd)context.CurrOp;

            int elems = op.RegisterSize == ARegisterSize.Simd128 ? 16 : 8;

            for (int index = 0; index < elems; index++)
            {
                EmitVectorExtractZx(context, op.Rn, index, 0);

                if (Popcnt.IsSupported)
                    context.EmitCall(typeof(Popcnt).GetMethod(nameof(Popcnt.PopCount), new Type[] { typeof(ulong) }));
                else
                    ASoftFallback.EmitCall(context, nameof(ASoftFallback.CountSetBits8));

                EmitVectorInsert(context, op.Rd, index, 0);
            }

            if (op.RegisterSize == ARegisterSize.Simd64) EmitVectorZeroUpper(context, op.Rd);
        }

        public static void Fabd_S(AILEmitterCtx context)
        {
            EmitScalarBinaryOpF(context, () =>
            {
                context.Emit(OpCodes.Sub);

                EmitUnaryMathCall(context, nameof(Math.Abs));
            });
        }

        public static void Fabs_S(AILEmitterCtx context)
        {
            EmitScalarUnaryOpF(context, () =>
            {
                EmitUnaryMathCall(context, nameof(Math.Abs));
            });
        }

        public static void Fabs_V(AILEmitterCtx context)
        {
            EmitVectorUnaryOpF(context, () =>
            {
                EmitUnaryMathCall(context, nameof(Math.Abs));
            });
        }

        public static void Fadd_S(AILEmitterCtx context)
        {
            if (AOptimizations.FastFp && AOptimizations.UseSse
                                      && AOptimizations.UseSse2)
                EmitScalarSseOrSse2OpF(context, nameof(Sse.AddScalar));
            else
                EmitScalarBinaryOpF(context, () =>
                {
                    EmitSoftFloatCall(context, nameof(ASoftFloat_32.FpAdd));
                });
        }

        public static void Fadd_V(AILEmitterCtx context)
        {
            if (AOptimizations.FastFp && AOptimizations.UseSse
                                      && AOptimizations.UseSse2)
                EmitVectorSseOrSse2OpF(context, nameof(Sse.Add));
            else
                EmitVectorBinaryOpF(context, () =>
                {
                    EmitSoftFloatCall(context, nameof(ASoftFloat_32.FpAdd));
                });
        }

        public static void Faddp_S(AILEmitterCtx context)
        {
            AOpCodeSimd op = (AOpCodeSimd)context.CurrOp;

            int sizeF = op.Size & 1;

            EmitVectorExtractF(context, op.Rn, 0, sizeF);
            EmitVectorExtractF(context, op.Rn, 1, sizeF);

            context.Emit(OpCodes.Add);

            EmitScalarSetF(context, op.Rd, sizeF);
        }

        public static void Faddp_V(AILEmitterCtx context)
        {
            EmitVectorPairwiseOpF(context, () => context.Emit(OpCodes.Add));
        }

        public static void Fdiv_S(AILEmitterCtx context)
        {
            if (AOptimizations.FastFp && AOptimizations.UseSse
                                      && AOptimizations.UseSse2)
                EmitScalarSseOrSse2OpF(context, nameof(Sse.DivideScalar));
            else
                EmitScalarBinaryOpF(context, () =>
                {
                    EmitSoftFloatCall(context, nameof(ASoftFloat_32.FpDiv));
                });
        }

        public static void Fdiv_V(AILEmitterCtx context)
        {
            if (AOptimizations.FastFp && AOptimizations.UseSse
                                      && AOptimizations.UseSse2)
                EmitVectorSseOrSse2OpF(context, nameof(Sse.Divide));
            else
                EmitVectorBinaryOpF(context, () =>
                {
                    EmitSoftFloatCall(context, nameof(ASoftFloat_32.FpDiv));
                });
        }

        public static void Fmadd_S(AILEmitterCtx context)
        {
            if (AOptimizations.FastFp && AOptimizations.UseSse2)
            {
                AOpCodeSimdReg op = (AOpCodeSimdReg)context.CurrOp;

                if (op.Size == 0)
                {
                    Type[] typesMulAdd = new Type[] { typeof(Vector128<float>), typeof(Vector128<float>) };

                    context.EmitLdvec(op.Ra);
                    context.EmitLdvec(op.Rn);
                    context.EmitLdvec(op.Rm);

                    context.EmitCall(typeof(Sse).GetMethod(nameof(Sse.MultiplyScalar), typesMulAdd));
                    context.EmitCall(typeof(Sse).GetMethod(nameof(Sse.AddScalar),      typesMulAdd));

                    context.EmitStvec(op.Rd);

                    EmitVectorZero32_128(context, op.Rd);
                }
                else /* if (Op.Size == 1) */
                {
                    Type[] typesMulAdd = new Type[] { typeof(Vector128<double>), typeof(Vector128<double>) };

                    EmitLdvecWithCastToDouble(context, op.Ra);
                    EmitLdvecWithCastToDouble(context, op.Rn);
                    EmitLdvecWithCastToDouble(context, op.Rm);

                    context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.MultiplyScalar), typesMulAdd));
                    context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.AddScalar),      typesMulAdd));

                    EmitStvecWithCastFromDouble(context, op.Rd);

                    EmitVectorZeroUpper(context, op.Rd);
                }
            }
            else
            {
                EmitScalarTernaryRaOpF(context, () =>
                {
                    EmitSoftFloatCall(context, nameof(ASoftFloat_32.FpMulAdd));
                });
            }
        }

        public static void Fmax_S(AILEmitterCtx context)
        {
            if (AOptimizations.FastFp && AOptimizations.UseSse
                                      && AOptimizations.UseSse2)
                EmitScalarSseOrSse2OpF(context, nameof(Sse.MaxScalar));
            else
                EmitScalarBinaryOpF(context, () =>
                {
                    EmitSoftFloatCall(context, nameof(ASoftFloat_32.FpMax));
                });
        }

        public static void Fmax_V(AILEmitterCtx context)
        {
            if (AOptimizations.FastFp && AOptimizations.UseSse
                                      && AOptimizations.UseSse2)
                EmitVectorSseOrSse2OpF(context, nameof(Sse.Max));
            else
                EmitVectorBinaryOpF(context, () =>
                {
                    EmitSoftFloatCall(context, nameof(ASoftFloat_32.FpMax));
                });
        }

        public static void Fmaxnm_S(AILEmitterCtx context)
        {
            EmitScalarBinaryOpF(context, () =>
            {
                EmitSoftFloatCall(context, nameof(ASoftFloat_32.FpMaxNum));
            });
        }

        public static void Fmaxnm_V(AILEmitterCtx context)
        {
            EmitVectorBinaryOpF(context, () =>
            {
                EmitSoftFloatCall(context, nameof(ASoftFloat_32.FpMaxNum));
            });
        }

        public static void Fmaxp_V(AILEmitterCtx context)
        {
            EmitVectorPairwiseOpF(context, () =>
            {
                EmitSoftFloatCall(context, nameof(ASoftFloat_32.FpMax));
            });
        }

        public static void Fmin_S(AILEmitterCtx context)
        {
            if (AOptimizations.FastFp && AOptimizations.UseSse
                                      && AOptimizations.UseSse2)
                EmitScalarSseOrSse2OpF(context, nameof(Sse.MinScalar));
            else
                EmitScalarBinaryOpF(context, () =>
                {
                    EmitSoftFloatCall(context, nameof(ASoftFloat_32.FpMin));
                });
        }

        public static void Fmin_V(AILEmitterCtx context)
        {
            if (AOptimizations.FastFp && AOptimizations.UseSse
                                      && AOptimizations.UseSse2)
                EmitVectorSseOrSse2OpF(context, nameof(Sse.Min));
            else
                EmitVectorBinaryOpF(context, () =>
                {
                    EmitSoftFloatCall(context, nameof(ASoftFloat_32.FpMin));
                });
        }

        public static void Fminnm_S(AILEmitterCtx context)
        {
            EmitScalarBinaryOpF(context, () =>
            {
                EmitSoftFloatCall(context, nameof(ASoftFloat_32.FpMinNum));
            });
        }

        public static void Fminnm_V(AILEmitterCtx context)
        {
            EmitVectorBinaryOpF(context, () =>
            {
                EmitSoftFloatCall(context, nameof(ASoftFloat_32.FpMinNum));
            });
        }

        public static void Fminp_V(AILEmitterCtx context)
        {
            EmitVectorPairwiseOpF(context, () =>
            {
                EmitSoftFloatCall(context, nameof(ASoftFloat_32.FpMin));
            });
        }

        public static void Fmla_Se(AILEmitterCtx context)
        {
            EmitScalarTernaryOpByElemF(context, () =>
            {
                context.Emit(OpCodes.Mul);
                context.Emit(OpCodes.Add);
            });
        }

        public static void Fmla_V(AILEmitterCtx context)
        {
            EmitVectorTernaryOpF(context, () =>
            {
                context.Emit(OpCodes.Mul);
                context.Emit(OpCodes.Add);
            });
        }

        public static void Fmla_Ve(AILEmitterCtx context)
        {
            EmitVectorTernaryOpByElemF(context, () =>
            {
                context.Emit(OpCodes.Mul);
                context.Emit(OpCodes.Add);
            });
        }

        public static void Fmls_Se(AILEmitterCtx context)
        {
            EmitScalarTernaryOpByElemF(context, () =>
            {
                context.Emit(OpCodes.Mul);
                context.Emit(OpCodes.Sub);
            });
        }

        public static void Fmls_V(AILEmitterCtx context)
        {
            EmitVectorTernaryOpF(context, () =>
            {
                context.Emit(OpCodes.Mul);
                context.Emit(OpCodes.Sub);
            });
        }

        public static void Fmls_Ve(AILEmitterCtx context)
        {
            EmitVectorTernaryOpByElemF(context, () =>
            {
                context.Emit(OpCodes.Mul);
                context.Emit(OpCodes.Sub);
            });
        }

        public static void Fmsub_S(AILEmitterCtx context)
        {
            if (AOptimizations.FastFp && AOptimizations.UseSse2)
            {
                AOpCodeSimdReg op = (AOpCodeSimdReg)context.CurrOp;

                if (op.Size == 0)
                {
                    Type[] typesMulSub = new Type[] { typeof(Vector128<float>), typeof(Vector128<float>) };

                    context.EmitLdvec(op.Ra);
                    context.EmitLdvec(op.Rn);
                    context.EmitLdvec(op.Rm);

                    context.EmitCall(typeof(Sse).GetMethod(nameof(Sse.MultiplyScalar), typesMulSub));
                    context.EmitCall(typeof(Sse).GetMethod(nameof(Sse.SubtractScalar), typesMulSub));

                    context.EmitStvec(op.Rd);

                    EmitVectorZero32_128(context, op.Rd);
                }
                else /* if (Op.Size == 1) */
                {
                    Type[] typesMulSub = new Type[] { typeof(Vector128<double>), typeof(Vector128<double>) };

                    EmitLdvecWithCastToDouble(context, op.Ra);
                    EmitLdvecWithCastToDouble(context, op.Rn);
                    EmitLdvecWithCastToDouble(context, op.Rm);

                    context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.MultiplyScalar), typesMulSub));
                    context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.SubtractScalar), typesMulSub));

                    EmitStvecWithCastFromDouble(context, op.Rd);

                    EmitVectorZeroUpper(context, op.Rd);
                }
            }
            else
            {
                EmitScalarTernaryRaOpF(context, () =>
                {
                    EmitSoftFloatCall(context, nameof(ASoftFloat_32.FpMulSub));
                });
            }
        }

        public static void Fmul_S(AILEmitterCtx context)
        {
            if (AOptimizations.FastFp && AOptimizations.UseSse
                                      && AOptimizations.UseSse2)
                EmitScalarSseOrSse2OpF(context, nameof(Sse.MultiplyScalar));
            else
                EmitScalarBinaryOpF(context, () =>
                {
                    EmitSoftFloatCall(context, nameof(ASoftFloat_32.FpMul));
                });
        }

        public static void Fmul_Se(AILEmitterCtx context)
        {
            EmitScalarBinaryOpByElemF(context, () => context.Emit(OpCodes.Mul));
        }

        public static void Fmul_V(AILEmitterCtx context)
        {
            if (AOptimizations.FastFp && AOptimizations.UseSse
                                      && AOptimizations.UseSse2)
                EmitVectorSseOrSse2OpF(context, nameof(Sse.Multiply));
            else
                EmitVectorBinaryOpF(context, () =>
                {
                    EmitSoftFloatCall(context, nameof(ASoftFloat_32.FpMul));
                });
        }

        public static void Fmul_Ve(AILEmitterCtx context)
        {
            EmitVectorBinaryOpByElemF(context, () => context.Emit(OpCodes.Mul));
        }

        public static void Fmulx_S(AILEmitterCtx context)
        {
            EmitScalarBinaryOpF(context, () =>
            {
                EmitSoftFloatCall(context, nameof(ASoftFloat_32.FpMulX));
            });
        }

        public static void Fmulx_Se(AILEmitterCtx context)
        {
            EmitScalarBinaryOpByElemF(context, () =>
            {
                EmitSoftFloatCall(context, nameof(ASoftFloat_32.FpMulX));
            });
        }

        public static void Fmulx_V(AILEmitterCtx context)
        {
            EmitVectorBinaryOpF(context, () =>
            {
                EmitSoftFloatCall(context, nameof(ASoftFloat_32.FpMulX));
            });
        }

        public static void Fmulx_Ve(AILEmitterCtx context)
        {
            EmitVectorBinaryOpByElemF(context, () =>
            {
                EmitSoftFloatCall(context, nameof(ASoftFloat_32.FpMulX));
            });
        }

        public static void Fneg_S(AILEmitterCtx context)
        {
            EmitScalarUnaryOpF(context, () => context.Emit(OpCodes.Neg));
        }

        public static void Fneg_V(AILEmitterCtx context)
        {
            EmitVectorUnaryOpF(context, () => context.Emit(OpCodes.Neg));
        }

        public static void Fnmadd_S(AILEmitterCtx context)
        {
            AOpCodeSimdReg op = (AOpCodeSimdReg)context.CurrOp;

            int sizeF = op.Size & 1;

            EmitVectorExtractF(context, op.Rn, 0, sizeF);

            context.Emit(OpCodes.Neg);

            EmitVectorExtractF(context, op.Rm, 0, sizeF);

            context.Emit(OpCodes.Mul);

            EmitVectorExtractF(context, op.Ra, 0, sizeF);

            context.Emit(OpCodes.Sub);

            EmitScalarSetF(context, op.Rd, sizeF);
        }

        public static void Fnmsub_S(AILEmitterCtx context)
        {
            AOpCodeSimdReg op = (AOpCodeSimdReg)context.CurrOp;

            int sizeF = op.Size & 1;

            EmitVectorExtractF(context, op.Rn, 0, sizeF);
            EmitVectorExtractF(context, op.Rm, 0, sizeF);

            context.Emit(OpCodes.Mul);

            EmitVectorExtractF(context, op.Ra, 0, sizeF);

            context.Emit(OpCodes.Sub);

            EmitScalarSetF(context, op.Rd, sizeF);
        }

        public static void Fnmul_S(AILEmitterCtx context)
        {
            EmitScalarBinaryOpF(context, () =>
            {
                context.Emit(OpCodes.Mul);
                context.Emit(OpCodes.Neg);
            });
        }

        public static void Frecpe_S(AILEmitterCtx context)
        {
            EmitScalarUnaryOpF(context, () =>
            {
                EmitUnarySoftFloatCall(context, nameof(ASoftFloat.RecipEstimate));
            });
        }

        public static void Frecpe_V(AILEmitterCtx context)
        {
            EmitVectorUnaryOpF(context, () =>
            {
                EmitUnarySoftFloatCall(context, nameof(ASoftFloat.RecipEstimate));
            });
        }

        public static void Frecps_S(AILEmitterCtx context)
        {
            if (AOptimizations.FastFp && AOptimizations.UseSse2)
            {
                AOpCodeSimdReg op = (AOpCodeSimdReg)context.CurrOp;

                int sizeF = op.Size & 1;

                if (sizeF == 0)
                {
                    Type[] typesSsv    = new Type[] { typeof(float) };
                    Type[] typesMulSub = new Type[] { typeof(Vector128<float>), typeof(Vector128<float>) };

                    context.EmitLdc_R4(2f);
                    context.EmitCall(typeof(Sse).GetMethod(nameof(Sse.SetScalarVector128), typesSsv));

                    context.EmitLdvec(op.Rn);
                    context.EmitLdvec(op.Rm);

                    context.EmitCall(typeof(Sse).GetMethod(nameof(Sse.MultiplyScalar), typesMulSub));
                    context.EmitCall(typeof(Sse).GetMethod(nameof(Sse.SubtractScalar), typesMulSub));

                    context.EmitStvec(op.Rd);

                    EmitVectorZero32_128(context, op.Rd);
                }
                else /* if (SizeF == 1) */
                {
                    Type[] typesSsv    = new Type[] { typeof(double) };
                    Type[] typesMulSub = new Type[] { typeof(Vector128<double>), typeof(Vector128<double>) };

                    context.EmitLdc_R8(2d);
                    context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.SetScalarVector128), typesSsv));

                    EmitLdvecWithCastToDouble(context, op.Rn);
                    EmitLdvecWithCastToDouble(context, op.Rm);

                    context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.MultiplyScalar), typesMulSub));
                    context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.SubtractScalar), typesMulSub));

                    EmitStvecWithCastFromDouble(context, op.Rd);

                    EmitVectorZeroUpper(context, op.Rd);
                }
            }
            else
            {
                EmitScalarBinaryOpF(context, () =>
                {
                    EmitSoftFloatCall(context, nameof(ASoftFloat_32.FpRecipStepFused));
                });
            }
        }

        public static void Frecps_V(AILEmitterCtx context)
        {
            if (AOptimizations.FastFp && AOptimizations.UseSse2)
            {
                AOpCodeSimdReg op = (AOpCodeSimdReg)context.CurrOp;

                int sizeF = op.Size & 1;

                if (sizeF == 0)
                {
                    Type[] typesSav    = new Type[] { typeof(float) };
                    Type[] typesMulSub = new Type[] { typeof(Vector128<float>), typeof(Vector128<float>) };

                    context.EmitLdc_R4(2f);
                    context.EmitCall(typeof(Sse).GetMethod(nameof(Sse.SetAllVector128), typesSav));

                    context.EmitLdvec(op.Rn);
                    context.EmitLdvec(op.Rm);

                    context.EmitCall(typeof(Sse).GetMethod(nameof(Sse.Multiply), typesMulSub));
                    context.EmitCall(typeof(Sse).GetMethod(nameof(Sse.Subtract), typesMulSub));

                    context.EmitStvec(op.Rd);

                    if (op.RegisterSize == ARegisterSize.Simd64) EmitVectorZeroUpper(context, op.Rd);
                }
                else /* if (SizeF == 1) */
                {
                    Type[] typesSav    = new Type[] { typeof(double) };
                    Type[] typesMulSub = new Type[] { typeof(Vector128<double>), typeof(Vector128<double>) };

                    context.EmitLdc_R8(2d);
                    context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.SetAllVector128), typesSav));

                    EmitLdvecWithCastToDouble(context, op.Rn);
                    EmitLdvecWithCastToDouble(context, op.Rm);

                    context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.Multiply), typesMulSub));
                    context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.Subtract), typesMulSub));

                    EmitStvecWithCastFromDouble(context, op.Rd);
                }
            }
            else
            {
                EmitVectorBinaryOpF(context, () =>
                {
                    EmitSoftFloatCall(context, nameof(ASoftFloat_32.FpRecipStepFused));
                });
            }
        }

        public static void Frecpx_S(AILEmitterCtx context)
        {
            EmitScalarUnaryOpF(context, () =>
            {
                EmitSoftFloatCall(context, nameof(ASoftFloat_32.FpRecpX));
            });
        }

        public static void Frinta_S(AILEmitterCtx context)
        {
            AOpCodeSimd op = (AOpCodeSimd)context.CurrOp;

            EmitVectorExtractF(context, op.Rn, 0, op.Size);

            EmitRoundMathCall(context, MidpointRounding.AwayFromZero);

            EmitScalarSetF(context, op.Rd, op.Size);
        }

        public static void Frinta_V(AILEmitterCtx context)
        {
            EmitVectorUnaryOpF(context, () =>
            {
                EmitRoundMathCall(context, MidpointRounding.AwayFromZero);
            });
        }

        public static void Frinti_S(AILEmitterCtx context)
        {
            AOpCodeSimd op = (AOpCodeSimd)context.CurrOp;

            EmitScalarUnaryOpF(context, () =>
            {
                context.EmitLdarg(ATranslatedSub.StateArgIdx);

                if (op.Size == 0)
                    AVectorHelper.EmitCall(context, nameof(AVectorHelper.RoundF));
                else if (op.Size == 1)
                    AVectorHelper.EmitCall(context, nameof(AVectorHelper.Round));
                else
                    throw new InvalidOperationException();
            });
        }

        public static void Frinti_V(AILEmitterCtx context)
        {
            AOpCodeSimd op = (AOpCodeSimd)context.CurrOp;

            int sizeF = op.Size & 1;

            EmitVectorUnaryOpF(context, () =>
            {
                context.EmitLdarg(ATranslatedSub.StateArgIdx);

                if (sizeF == 0)
                    AVectorHelper.EmitCall(context, nameof(AVectorHelper.RoundF));
                else if (sizeF == 1)
                    AVectorHelper.EmitCall(context, nameof(AVectorHelper.Round));
                else
                    throw new InvalidOperationException();
            });
        }

        public static void Frintm_S(AILEmitterCtx context)
        {
            EmitScalarUnaryOpF(context, () =>
            {
                EmitUnaryMathCall(context, nameof(Math.Floor));
            });
        }

        public static void Frintm_V(AILEmitterCtx context)
        {
            EmitVectorUnaryOpF(context, () =>
            {
                EmitUnaryMathCall(context, nameof(Math.Floor));
            });
        }

        public static void Frintn_S(AILEmitterCtx context)
        {
            AOpCodeSimd op = (AOpCodeSimd)context.CurrOp;

            EmitVectorExtractF(context, op.Rn, 0, op.Size);

            EmitRoundMathCall(context, MidpointRounding.ToEven);

            EmitScalarSetF(context, op.Rd, op.Size);
        }

        public static void Frintn_V(AILEmitterCtx context)
        {
            EmitVectorUnaryOpF(context, () =>
            {
                EmitRoundMathCall(context, MidpointRounding.ToEven);
            });
        }

        public static void Frintp_S(AILEmitterCtx context)
        {
            EmitScalarUnaryOpF(context, () =>
            {
                EmitUnaryMathCall(context, nameof(Math.Ceiling));
            });
        }

        public static void Frintp_V(AILEmitterCtx context)
        {
            EmitVectorUnaryOpF(context, () =>
            {
                EmitUnaryMathCall(context, nameof(Math.Ceiling));
            });
        }

        public static void Frintx_S(AILEmitterCtx context)
        {
            AOpCodeSimd op = (AOpCodeSimd)context.CurrOp;

            EmitScalarUnaryOpF(context, () =>
            {
                context.EmitLdarg(ATranslatedSub.StateArgIdx);

                if (op.Size == 0)
                    AVectorHelper.EmitCall(context, nameof(AVectorHelper.RoundF));
                else if (op.Size == 1)
                    AVectorHelper.EmitCall(context, nameof(AVectorHelper.Round));
                else
                    throw new InvalidOperationException();
            });
        }

        public static void Frintx_V(AILEmitterCtx context)
        {
            AOpCodeSimd op = (AOpCodeSimd)context.CurrOp;

            EmitVectorUnaryOpF(context, () =>
            {
                context.EmitLdarg(ATranslatedSub.StateArgIdx);

                if (op.Size == 0)
                    AVectorHelper.EmitCall(context, nameof(AVectorHelper.RoundF));
                else if (op.Size == 1)
                    AVectorHelper.EmitCall(context, nameof(AVectorHelper.Round));
                else
                    throw new InvalidOperationException();
            });
        }

        public static void Frsqrte_S(AILEmitterCtx context)
        {
            EmitScalarUnaryOpF(context, () =>
            {
                EmitUnarySoftFloatCall(context, nameof(ASoftFloat.InvSqrtEstimate));
            });
        }

        public static void Frsqrte_V(AILEmitterCtx context)
        {
            EmitVectorUnaryOpF(context, () =>
            {
                EmitUnarySoftFloatCall(context, nameof(ASoftFloat.InvSqrtEstimate));
            });
        }

        public static void Frsqrts_S(AILEmitterCtx context)
        {
            if (AOptimizations.FastFp && AOptimizations.UseSse2)
            {
                AOpCodeSimdReg op = (AOpCodeSimdReg)context.CurrOp;

                int sizeF = op.Size & 1;

                if (sizeF == 0)
                {
                    Type[] typesSsv    = new Type[] { typeof(float) };
                    Type[] typesMulSub = new Type[] { typeof(Vector128<float>), typeof(Vector128<float>) };

                    context.EmitLdc_R4(0.5f);
                    context.EmitCall(typeof(Sse).GetMethod(nameof(Sse.SetScalarVector128), typesSsv));

                    context.EmitLdc_R4(3f);
                    context.EmitCall(typeof(Sse).GetMethod(nameof(Sse.SetScalarVector128), typesSsv));

                    context.EmitLdvec(op.Rn);
                    context.EmitLdvec(op.Rm);

                    context.EmitCall(typeof(Sse).GetMethod(nameof(Sse.MultiplyScalar), typesMulSub));
                    context.EmitCall(typeof(Sse).GetMethod(nameof(Sse.SubtractScalar), typesMulSub));
                    context.EmitCall(typeof(Sse).GetMethod(nameof(Sse.MultiplyScalar), typesMulSub));

                    context.EmitStvec(op.Rd);

                    EmitVectorZero32_128(context, op.Rd);
                }
                else /* if (SizeF == 1) */
                {
                    Type[] typesSsv    = new Type[] { typeof(double) };
                    Type[] typesMulSub = new Type[] { typeof(Vector128<double>), typeof(Vector128<double>) };

                    context.EmitLdc_R8(0.5d);
                    context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.SetScalarVector128), typesSsv));

                    context.EmitLdc_R8(3d);
                    context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.SetScalarVector128), typesSsv));

                    EmitLdvecWithCastToDouble(context, op.Rn);
                    EmitLdvecWithCastToDouble(context, op.Rm);

                    context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.MultiplyScalar), typesMulSub));
                    context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.SubtractScalar), typesMulSub));
                    context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.MultiplyScalar), typesMulSub));

                    EmitStvecWithCastFromDouble(context, op.Rd);

                    EmitVectorZeroUpper(context, op.Rd);
                }
            }
            else
            {
                EmitScalarBinaryOpF(context, () =>
                {
                    EmitSoftFloatCall(context, nameof(ASoftFloat_32.FprSqrtStepFused));
                });
            }
        }

        public static void Frsqrts_V(AILEmitterCtx context)
        {
            if (AOptimizations.FastFp && AOptimizations.UseSse2)
            {
                AOpCodeSimdReg op = (AOpCodeSimdReg)context.CurrOp;

                int sizeF = op.Size & 1;

                if (sizeF == 0)
                {
                    Type[] typesSav    = new Type[] { typeof(float) };
                    Type[] typesMulSub = new Type[] { typeof(Vector128<float>), typeof(Vector128<float>) };

                    context.EmitLdc_R4(0.5f);
                    context.EmitCall(typeof(Sse).GetMethod(nameof(Sse.SetAllVector128), typesSav));

                    context.EmitLdc_R4(3f);
                    context.EmitCall(typeof(Sse).GetMethod(nameof(Sse.SetAllVector128), typesSav));

                    context.EmitLdvec(op.Rn);
                    context.EmitLdvec(op.Rm);

                    context.EmitCall(typeof(Sse).GetMethod(nameof(Sse.Multiply), typesMulSub));
                    context.EmitCall(typeof(Sse).GetMethod(nameof(Sse.Subtract), typesMulSub));
                    context.EmitCall(typeof(Sse).GetMethod(nameof(Sse.Multiply), typesMulSub));

                    context.EmitStvec(op.Rd);

                    if (op.RegisterSize == ARegisterSize.Simd64) EmitVectorZeroUpper(context, op.Rd);
                }
                else /* if (SizeF == 1) */
                {
                    Type[] typesSav    = new Type[] { typeof(double) };
                    Type[] typesMulSub = new Type[] { typeof(Vector128<double>), typeof(Vector128<double>) };

                    context.EmitLdc_R8(0.5d);
                    context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.SetAllVector128), typesSav));

                    context.EmitLdc_R8(3d);
                    context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.SetAllVector128), typesSav));

                    EmitLdvecWithCastToDouble(context, op.Rn);
                    EmitLdvecWithCastToDouble(context, op.Rm);

                    context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.Multiply), typesMulSub));
                    context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.Subtract), typesMulSub));
                    context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.Multiply), typesMulSub));

                    EmitStvecWithCastFromDouble(context, op.Rd);
                }
            }
            else
            {
                EmitVectorBinaryOpF(context, () =>
                {
                    EmitSoftFloatCall(context, nameof(ASoftFloat_32.FprSqrtStepFused));
                });
            }
        }

        public static void Fsqrt_S(AILEmitterCtx context)
        {
            if (AOptimizations.FastFp && AOptimizations.UseSse
                                      && AOptimizations.UseSse2)
                EmitScalarSseOrSse2OpF(context, nameof(Sse.SqrtScalar));
            else
                EmitScalarUnaryOpF(context, () =>
                {
                    EmitSoftFloatCall(context, nameof(ASoftFloat_32.FpSqrt));
                });
        }

        public static void Fsqrt_V(AILEmitterCtx context)
        {
            if (AOptimizations.FastFp && AOptimizations.UseSse
                                      && AOptimizations.UseSse2)
                EmitVectorSseOrSse2OpF(context, nameof(Sse.Sqrt));
            else
                EmitVectorUnaryOpF(context, () =>
                {
                    EmitSoftFloatCall(context, nameof(ASoftFloat_32.FpSqrt));
                });
        }

        public static void Fsub_S(AILEmitterCtx context)
        {
            if (AOptimizations.FastFp && AOptimizations.UseSse
                                      && AOptimizations.UseSse2)
                EmitScalarSseOrSse2OpF(context, nameof(Sse.SubtractScalar));
            else
                EmitScalarBinaryOpF(context, () =>
                {
                    EmitSoftFloatCall(context, nameof(ASoftFloat_32.FpSub));
                });
        }

        public static void Fsub_V(AILEmitterCtx context)
        {
            if (AOptimizations.FastFp && AOptimizations.UseSse
                                      && AOptimizations.UseSse2)
                EmitVectorSseOrSse2OpF(context, nameof(Sse.Subtract));
            else
                EmitVectorBinaryOpF(context, () =>
                {
                    EmitSoftFloatCall(context, nameof(ASoftFloat_32.FpSub));
                });
        }

        public static void Mla_V(AILEmitterCtx context)
        {
            EmitVectorTernaryOpZx(context, () =>
            {
                context.Emit(OpCodes.Mul);
                context.Emit(OpCodes.Add);
            });
        }

        public static void Mla_Ve(AILEmitterCtx context)
        {
            EmitVectorTernaryOpByElemZx(context, () =>
            {
                context.Emit(OpCodes.Mul);
                context.Emit(OpCodes.Add);
            });
        }

        public static void Mls_V(AILEmitterCtx context)
        {
            EmitVectorTernaryOpZx(context, () =>
            {
                context.Emit(OpCodes.Mul);
                context.Emit(OpCodes.Sub);
            });
        }

        public static void Mls_Ve(AILEmitterCtx context)
        {
            EmitVectorTernaryOpByElemZx(context, () =>
            {
                context.Emit(OpCodes.Mul);
                context.Emit(OpCodes.Sub);
            });
        }

        public static void Mul_V(AILEmitterCtx context)
        {
            EmitVectorBinaryOpZx(context, () => context.Emit(OpCodes.Mul));
        }

        public static void Mul_Ve(AILEmitterCtx context)
        {
            EmitVectorBinaryOpByElemZx(context, () => context.Emit(OpCodes.Mul));
        }

        public static void Neg_S(AILEmitterCtx context)
        {
            EmitScalarUnaryOpSx(context, () => context.Emit(OpCodes.Neg));
        }

        public static void Neg_V(AILEmitterCtx context)
        {
            EmitVectorUnaryOpSx(context, () => context.Emit(OpCodes.Neg));
        }

        public static void Raddhn_V(AILEmitterCtx context)
        {
            EmitHighNarrow(context, () => context.Emit(OpCodes.Add), true);
        }

        public static void Rsubhn_V(AILEmitterCtx context)
        {
            EmitHighNarrow(context, () => context.Emit(OpCodes.Sub), true);
        }

        public static void Saba_V(AILEmitterCtx context)
        {
            EmitVectorTernaryOpSx(context, () =>
            {
                context.Emit(OpCodes.Sub);
                EmitAbs(context);

                context.Emit(OpCodes.Add);
            });
        }

        public static void Sabal_V(AILEmitterCtx context)
        {
            EmitVectorWidenRnRmTernaryOpSx(context, () =>
            {
                context.Emit(OpCodes.Sub);
                EmitAbs(context);

                context.Emit(OpCodes.Add);
            });
        }

        public static void Sabd_V(AILEmitterCtx context)
        {
            EmitVectorBinaryOpSx(context, () =>
            {
                context.Emit(OpCodes.Sub);
                EmitAbs(context);
            });
        }

        public static void Sabdl_V(AILEmitterCtx context)
        {
            EmitVectorWidenRnRmBinaryOpSx(context, () =>
            {
                context.Emit(OpCodes.Sub);
                EmitAbs(context);
            });
        }

        public static void Sadalp_V(AILEmitterCtx context)
        {
            EmitAddLongPairwise(context, true, true);
        }

        public static void Saddl_V(AILEmitterCtx context)
        {
            if (AOptimizations.UseSse41)
            {
                AOpCodeSimdReg op = (AOpCodeSimdReg)context.CurrOp;

                Type[] typesSrl = new Type[] { VectorIntTypesPerSizeLog2[op.Size], typeof(byte) };
                Type[] typesCvt = new Type[] { VectorIntTypesPerSizeLog2[op.Size] };
                Type[] typesAdd = new Type[] { VectorIntTypesPerSizeLog2[op.Size + 1],
                                               VectorIntTypesPerSizeLog2[op.Size + 1] };

                string[] namesCvt = new string[] { nameof(Sse41.ConvertToVector128Int16),
                                                   nameof(Sse41.ConvertToVector128Int32),
                                                   nameof(Sse41.ConvertToVector128Int64) };

                int numBytes = op.RegisterSize == ARegisterSize.Simd128 ? 8 : 0;

                EmitLdvecWithSignedCast(context, op.Rn, op.Size);

                context.EmitLdc_I4(numBytes);
                context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.ShiftRightLogical128BitLane), typesSrl));

                context.EmitCall(typeof(Sse41).GetMethod(namesCvt[op.Size], typesCvt));

                EmitLdvecWithUnsignedCast(context, op.Rm, op.Size);

                context.EmitLdc_I4(numBytes);
                context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.ShiftRightLogical128BitLane), typesSrl));

                context.EmitCall(typeof(Sse41).GetMethod(namesCvt[op.Size], typesCvt));

                context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.Add), typesAdd));

                EmitStvecWithSignedCast(context, op.Rd, op.Size + 1);
            }
            else
            {
                EmitVectorWidenRnRmBinaryOpSx(context, () => context.Emit(OpCodes.Add));
            }
        }

        public static void Saddlp_V(AILEmitterCtx context)
        {
            EmitAddLongPairwise(context, true, false);
        }

        public static void Saddw_V(AILEmitterCtx context)
        {
            EmitVectorWidenRmBinaryOpSx(context, () => context.Emit(OpCodes.Add));
        }

        public static void Shadd_V(AILEmitterCtx context)
        {
            AOpCodeSimdReg op = (AOpCodeSimdReg)context.CurrOp;

            if (AOptimizations.UseSse2 && op.Size > 0)
            {
                Type[] typesSra       = new Type[] { VectorIntTypesPerSizeLog2[op.Size], typeof(byte) };
                Type[] typesAndXorAdd = new Type[] { VectorIntTypesPerSizeLog2[op.Size], VectorIntTypesPerSizeLog2[op.Size] };

                EmitLdvecWithSignedCast(context, op.Rn, op.Size);

                context.Emit(OpCodes.Dup);
                context.EmitStvectmp();

                EmitLdvecWithSignedCast(context, op.Rm, op.Size);

                context.Emit(OpCodes.Dup);
                context.EmitStvectmp2();

                context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.And), typesAndXorAdd));

                context.EmitLdvectmp();
                context.EmitLdvectmp2();

                context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.Xor), typesAndXorAdd));

                context.EmitLdc_I4(1);
                context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.ShiftRightArithmetic), typesSra));

                context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.Add), typesAndXorAdd));

                EmitStvecWithSignedCast(context, op.Rd, op.Size);

                if (op.RegisterSize == ARegisterSize.Simd64) EmitVectorZeroUpper(context, op.Rd);
            }
            else
            {
                EmitVectorBinaryOpSx(context, () =>
                {
                    context.Emit(OpCodes.Add);

                    context.Emit(OpCodes.Ldc_I4_1);
                    context.Emit(OpCodes.Shr);
                });
            }
        }

        public static void Shsub_V(AILEmitterCtx context)
        {
            AOpCodeSimdReg op = (AOpCodeSimdReg)context.CurrOp;

            if (AOptimizations.UseSse2 && op.Size < 2)
            {
                Type[] typesSav    = new Type[] { IntTypesPerSizeLog2[op.Size] };
                Type[] typesAddSub = new Type[] { VectorIntTypesPerSizeLog2 [op.Size], VectorIntTypesPerSizeLog2 [op.Size] };
                Type[] typesAvg    = new Type[] { VectorUIntTypesPerSizeLog2[op.Size], VectorUIntTypesPerSizeLog2[op.Size] };

                context.EmitLdc_I4(op.Size == 0 ? sbyte.MinValue : short.MinValue);
                context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.SetAllVector128), typesSav));

                context.EmitStvectmp();

                EmitLdvecWithSignedCast(context, op.Rn, op.Size);
                context.EmitLdvectmp();

                context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.Add), typesAddSub));

                context.Emit(OpCodes.Dup);

                EmitLdvecWithSignedCast(context, op.Rm, op.Size);
                context.EmitLdvectmp();

                context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.Add), typesAddSub));

                context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.Average), typesAvg));

                context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.Subtract), typesAddSub));

                EmitStvecWithSignedCast(context, op.Rd, op.Size);

                if (op.RegisterSize == ARegisterSize.Simd64) EmitVectorZeroUpper(context, op.Rd);
            }
            else
            {
                EmitVectorBinaryOpSx(context, () =>
                {
                    context.Emit(OpCodes.Sub);

                    context.Emit(OpCodes.Ldc_I4_1);
                    context.Emit(OpCodes.Shr);
                });
            }
        }

        public static void Smax_V(AILEmitterCtx context)
        {
            Type[] types = new Type[] { typeof(long), typeof(long) };

            MethodInfo mthdInfo = typeof(Math).GetMethod(nameof(Math.Max), types);

            EmitVectorBinaryOpSx(context, () => context.EmitCall(mthdInfo));
        }

        public static void Smaxp_V(AILEmitterCtx context)
        {
            Type[] types = new Type[] { typeof(long), typeof(long) };

            MethodInfo mthdInfo = typeof(Math).GetMethod(nameof(Math.Max), types);

            EmitVectorPairwiseOpSx(context, () => context.EmitCall(mthdInfo));
        }

        public static void Smin_V(AILEmitterCtx context)
        {
            Type[] types = new Type[] { typeof(long), typeof(long) };

            MethodInfo mthdInfo = typeof(Math).GetMethod(nameof(Math.Min), types);

            EmitVectorBinaryOpSx(context, () => context.EmitCall(mthdInfo));
        }

        public static void Sminp_V(AILEmitterCtx context)
        {
            Type[] types = new Type[] { typeof(long), typeof(long) };

            MethodInfo mthdInfo = typeof(Math).GetMethod(nameof(Math.Min), types);

            EmitVectorPairwiseOpSx(context, () => context.EmitCall(mthdInfo));
        }

        public static void Smlal_V(AILEmitterCtx context)
        {
            AOpCodeSimdReg op = (AOpCodeSimdReg)context.CurrOp;

            if (AOptimizations.UseSse41 && op.Size < 2)
            {
                Type[] typesSrl    = new Type[] { VectorIntTypesPerSizeLog2[op.Size], typeof(byte) };
                Type[] typesCvt    = new Type[] { VectorIntTypesPerSizeLog2[op.Size] };
                Type[] typesMulAdd = new Type[] { VectorIntTypesPerSizeLog2[op.Size + 1],
                                                  VectorIntTypesPerSizeLog2[op.Size + 1] };

                Type typeMul = op.Size == 0 ? typeof(Sse2) : typeof(Sse41);

                string nameCvt = op.Size == 0
                    ? nameof(Sse41.ConvertToVector128Int16)
                    : nameof(Sse41.ConvertToVector128Int32);

                int numBytes = op.RegisterSize == ARegisterSize.Simd128 ? 8 : 0;

                EmitLdvecWithSignedCast(context, op.Rd, op.Size + 1);

                EmitLdvecWithSignedCast(context, op.Rn, op.Size);

                context.EmitLdc_I4(numBytes);
                context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.ShiftRightLogical128BitLane), typesSrl));

                context.EmitCall(typeof(Sse41).GetMethod(nameCvt, typesCvt));

                EmitLdvecWithSignedCast(context, op.Rm, op.Size);

                context.EmitLdc_I4(numBytes);
                context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.ShiftRightLogical128BitLane), typesSrl));

                context.EmitCall(typeof(Sse41).GetMethod(nameCvt, typesCvt));

                context.EmitCall(typeMul.GetMethod(nameof(Sse2.MultiplyLow), typesMulAdd));

                context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.Add), typesMulAdd));

                EmitStvecWithSignedCast(context, op.Rd, op.Size + 1);
            }
            else
            {
                EmitVectorWidenRnRmTernaryOpSx(context, () =>
                {
                    context.Emit(OpCodes.Mul);
                    context.Emit(OpCodes.Add);
                });
            }
        }

        public static void Smlsl_V(AILEmitterCtx context)
        {
            AOpCodeSimdReg op = (AOpCodeSimdReg)context.CurrOp;

            if (AOptimizations.UseSse41 && op.Size < 2)
            {
                Type[] typesSrl    = new Type[] { VectorIntTypesPerSizeLog2[op.Size], typeof(byte) };
                Type[] typesCvt    = new Type[] { VectorIntTypesPerSizeLog2[op.Size] };
                Type[] typesMulSub = new Type[] { VectorIntTypesPerSizeLog2[op.Size + 1],
                                                  VectorIntTypesPerSizeLog2[op.Size + 1] };

                Type typeMul = op.Size == 0 ? typeof(Sse2) : typeof(Sse41);

                string nameCvt = op.Size == 0
                    ? nameof(Sse41.ConvertToVector128Int16)
                    : nameof(Sse41.ConvertToVector128Int32);

                int numBytes = op.RegisterSize == ARegisterSize.Simd128 ? 8 : 0;

                EmitLdvecWithSignedCast(context, op.Rd, op.Size + 1);

                EmitLdvecWithSignedCast(context, op.Rn, op.Size);

                context.EmitLdc_I4(numBytes);
                context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.ShiftRightLogical128BitLane), typesSrl));

                context.EmitCall(typeof(Sse41).GetMethod(nameCvt, typesCvt));

                EmitLdvecWithSignedCast(context, op.Rm, op.Size);

                context.EmitLdc_I4(numBytes);
                context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.ShiftRightLogical128BitLane), typesSrl));

                context.EmitCall(typeof(Sse41).GetMethod(nameCvt, typesCvt));

                context.EmitCall(typeMul.GetMethod(nameof(Sse2.MultiplyLow), typesMulSub));

                context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.Subtract), typesMulSub));

                EmitStvecWithSignedCast(context, op.Rd, op.Size + 1);
            }
            else
            {
                EmitVectorWidenRnRmTernaryOpSx(context, () =>
                {
                    context.Emit(OpCodes.Mul);
                    context.Emit(OpCodes.Sub);
                });
            }
        }

        public static void Smull_V(AILEmitterCtx context)
        {
            EmitVectorWidenRnRmBinaryOpSx(context, () => context.Emit(OpCodes.Mul));
        }

        public static void Sqabs_S(AILEmitterCtx context)
        {
            EmitScalarSaturatingUnaryOpSx(context, () => EmitAbs(context));
        }

        public static void Sqabs_V(AILEmitterCtx context)
        {
            EmitVectorSaturatingUnaryOpSx(context, () => EmitAbs(context));
        }

        public static void Sqadd_S(AILEmitterCtx context)
        {
            EmitScalarSaturatingBinaryOpSx(context, SaturatingFlags.Add);
        }

        public static void Sqadd_V(AILEmitterCtx context)
        {
            EmitVectorSaturatingBinaryOpSx(context, SaturatingFlags.Add);
        }

        public static void Sqdmulh_S(AILEmitterCtx context)
        {
            EmitSaturatingBinaryOp(context, () => EmitDoublingMultiplyHighHalf(context, false), SaturatingFlags.ScalarSx);
        }

        public static void Sqdmulh_V(AILEmitterCtx context)
        {
            EmitSaturatingBinaryOp(context, () => EmitDoublingMultiplyHighHalf(context, false), SaturatingFlags.VectorSx);
        }

        public static void Sqneg_S(AILEmitterCtx context)
        {
            EmitScalarSaturatingUnaryOpSx(context, () => context.Emit(OpCodes.Neg));
        }

        public static void Sqneg_V(AILEmitterCtx context)
        {
            EmitVectorSaturatingUnaryOpSx(context, () => context.Emit(OpCodes.Neg));
        }

        public static void Sqrdmulh_S(AILEmitterCtx context)
        {
            EmitSaturatingBinaryOp(context, () => EmitDoublingMultiplyHighHalf(context, true), SaturatingFlags.ScalarSx);
        }

        public static void Sqrdmulh_V(AILEmitterCtx context)
        {
            EmitSaturatingBinaryOp(context, () => EmitDoublingMultiplyHighHalf(context, true), SaturatingFlags.VectorSx);
        }

        public static void Sqsub_S(AILEmitterCtx context)
        {
            EmitScalarSaturatingBinaryOpSx(context, SaturatingFlags.Sub);
        }

        public static void Sqsub_V(AILEmitterCtx context)
        {
            EmitVectorSaturatingBinaryOpSx(context, SaturatingFlags.Sub);
        }

        public static void Sqxtn_S(AILEmitterCtx context)
        {
            EmitSaturatingNarrowOp(context, SaturatingNarrowFlags.ScalarSxSx);
        }

        public static void Sqxtn_V(AILEmitterCtx context)
        {
            EmitSaturatingNarrowOp(context, SaturatingNarrowFlags.VectorSxSx);
        }

        public static void Sqxtun_S(AILEmitterCtx context)
        {
            EmitSaturatingNarrowOp(context, SaturatingNarrowFlags.ScalarSxZx);
        }

        public static void Sqxtun_V(AILEmitterCtx context)
        {
            EmitSaturatingNarrowOp(context, SaturatingNarrowFlags.VectorSxZx);
        }

        public static void Srhadd_V(AILEmitterCtx context)
        {
            AOpCodeSimdReg op = (AOpCodeSimdReg)context.CurrOp;

            if (AOptimizations.UseSse2 && op.Size < 2)
            {
                Type[] typesSav    = new Type[] { IntTypesPerSizeLog2[op.Size] };
                Type[] typesSubAdd = new Type[] { VectorIntTypesPerSizeLog2 [op.Size], VectorIntTypesPerSizeLog2 [op.Size] };
                Type[] typesAvg    = new Type[] { VectorUIntTypesPerSizeLog2[op.Size], VectorUIntTypesPerSizeLog2[op.Size] };

                context.EmitLdc_I4(op.Size == 0 ? sbyte.MinValue : short.MinValue);
                context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.SetAllVector128), typesSav));

                context.Emit(OpCodes.Dup);
                context.EmitStvectmp();

                EmitLdvecWithSignedCast(context, op.Rn, op.Size);
                context.EmitLdvectmp();

                context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.Subtract), typesSubAdd));

                EmitLdvecWithSignedCast(context, op.Rm, op.Size);
                context.EmitLdvectmp();

                context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.Subtract), typesSubAdd));

                context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.Average), typesAvg));
                context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.Add),     typesSubAdd));

                EmitStvecWithSignedCast(context, op.Rd, op.Size);

                if (op.RegisterSize == ARegisterSize.Simd64) EmitVectorZeroUpper(context, op.Rd);
            }
            else
            {
                EmitVectorBinaryOpSx(context, () =>
                {
                    context.Emit(OpCodes.Add);

                    context.Emit(OpCodes.Ldc_I4_1);
                    context.Emit(OpCodes.Add);

                    context.Emit(OpCodes.Ldc_I4_1);
                    context.Emit(OpCodes.Shr);
                });
            }
        }

        public static void Ssubl_V(AILEmitterCtx context)
        {
            if (AOptimizations.UseSse41)
            {
                AOpCodeSimdReg op = (AOpCodeSimdReg)context.CurrOp;

                Type[] typesSrl = new Type[] { VectorIntTypesPerSizeLog2[op.Size], typeof(byte) };
                Type[] typesCvt = new Type[] { VectorIntTypesPerSizeLog2[op.Size] };
                Type[] typesSub = new Type[] { VectorIntTypesPerSizeLog2[op.Size + 1],
                                               VectorIntTypesPerSizeLog2[op.Size + 1] };

                string[] namesCvt = new string[] { nameof(Sse41.ConvertToVector128Int16),
                                                   nameof(Sse41.ConvertToVector128Int32),
                                                   nameof(Sse41.ConvertToVector128Int64) };

                int numBytes = op.RegisterSize == ARegisterSize.Simd128 ? 8 : 0;

                EmitLdvecWithSignedCast(context, op.Rn, op.Size);

                context.EmitLdc_I4(numBytes);
                context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.ShiftRightLogical128BitLane), typesSrl));

                context.EmitCall(typeof(Sse41).GetMethod(namesCvt[op.Size], typesCvt));

                EmitLdvecWithUnsignedCast(context, op.Rm, op.Size);

                context.EmitLdc_I4(numBytes);
                context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.ShiftRightLogical128BitLane), typesSrl));

                context.EmitCall(typeof(Sse41).GetMethod(namesCvt[op.Size], typesCvt));

                context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.Subtract), typesSub));

                EmitStvecWithSignedCast(context, op.Rd, op.Size + 1);
            }
            else
            {
                EmitVectorWidenRnRmBinaryOpSx(context, () => context.Emit(OpCodes.Sub));
            }
        }

        public static void Ssubw_V(AILEmitterCtx context)
        {
            EmitVectorWidenRmBinaryOpSx(context, () => context.Emit(OpCodes.Sub));
        }

        public static void Sub_S(AILEmitterCtx context)
        {
            EmitScalarBinaryOpZx(context, () => context.Emit(OpCodes.Sub));
        }

        public static void Sub_V(AILEmitterCtx context)
        {
            if (AOptimizations.UseSse2)
                EmitSse2Op(context, nameof(Sse2.Subtract));
            else
                EmitVectorBinaryOpZx(context, () => context.Emit(OpCodes.Sub));
        }

        public static void Subhn_V(AILEmitterCtx context)
        {
            EmitHighNarrow(context, () => context.Emit(OpCodes.Sub), false);
        }

        public static void Suqadd_S(AILEmitterCtx context)
        {
            EmitScalarSaturatingBinaryOpSx(context, SaturatingFlags.Accumulate);
        }

        public static void Suqadd_V(AILEmitterCtx context)
        {
            EmitVectorSaturatingBinaryOpSx(context, SaturatingFlags.Accumulate);
        }

        public static void Uaba_V(AILEmitterCtx context)
        {
            EmitVectorTernaryOpZx(context, () =>
            {
                context.Emit(OpCodes.Sub);
                EmitAbs(context);

                context.Emit(OpCodes.Add);
            });
        }

        public static void Uabal_V(AILEmitterCtx context)
        {
            EmitVectorWidenRnRmTernaryOpZx(context, () =>
            {
                context.Emit(OpCodes.Sub);
                EmitAbs(context);

                context.Emit(OpCodes.Add);
            });
        }

        public static void Uabd_V(AILEmitterCtx context)
        {
            EmitVectorBinaryOpZx(context, () =>
            {
                context.Emit(OpCodes.Sub);
                EmitAbs(context);
            });
        }

        public static void Uabdl_V(AILEmitterCtx context)
        {
            EmitVectorWidenRnRmBinaryOpZx(context, () =>
            {
                context.Emit(OpCodes.Sub);
                EmitAbs(context);
            });
        }

        public static void Uadalp_V(AILEmitterCtx context)
        {
            EmitAddLongPairwise(context, false, true);
        }

        public static void Uaddl_V(AILEmitterCtx context)
        {
            if (AOptimizations.UseSse41)
            {
                AOpCodeSimdReg op = (AOpCodeSimdReg)context.CurrOp;

                Type[] typesSrl = new Type[] { VectorUIntTypesPerSizeLog2[op.Size], typeof(byte) };
                Type[] typesCvt = new Type[] { VectorUIntTypesPerSizeLog2[op.Size] };
                Type[] typesAdd = new Type[] { VectorUIntTypesPerSizeLog2[op.Size + 1],
                                               VectorUIntTypesPerSizeLog2[op.Size + 1] };

                string[] namesCvt = new string[] { nameof(Sse41.ConvertToVector128Int16),
                                                   nameof(Sse41.ConvertToVector128Int32),
                                                   nameof(Sse41.ConvertToVector128Int64) };

                int numBytes = op.RegisterSize == ARegisterSize.Simd128 ? 8 : 0;

                EmitLdvecWithUnsignedCast(context, op.Rn, op.Size);

                context.EmitLdc_I4(numBytes);
                context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.ShiftRightLogical128BitLane), typesSrl));

                context.EmitCall(typeof(Sse41).GetMethod(namesCvt[op.Size], typesCvt));

                EmitLdvecWithUnsignedCast(context, op.Rm, op.Size);

                context.EmitLdc_I4(numBytes);
                context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.ShiftRightLogical128BitLane), typesSrl));

                context.EmitCall(typeof(Sse41).GetMethod(namesCvt[op.Size], typesCvt));

                context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.Add), typesAdd));

                EmitStvecWithUnsignedCast(context, op.Rd, op.Size + 1);
            }
            else
            {
                EmitVectorWidenRnRmBinaryOpZx(context, () => context.Emit(OpCodes.Add));
            }
        }

        public static void Uaddlp_V(AILEmitterCtx context)
        {
            EmitAddLongPairwise(context, false, false);
        }

        public static void Uaddlv_V(AILEmitterCtx context)
        {
            AOpCodeSimd op = (AOpCodeSimd)context.CurrOp;

            int bytes = op.GetBitsCount() >> 3;
            int elems = bytes >> op.Size;

            EmitVectorExtractZx(context, op.Rn, 0, op.Size);

            for (int index = 1; index < elems; index++)
            {
                EmitVectorExtractZx(context, op.Rn, index, op.Size);

                context.Emit(OpCodes.Add);
            }

            EmitScalarSet(context, op.Rd, op.Size + 1);
        }

        public static void Uaddw_V(AILEmitterCtx context)
        {
            EmitVectorWidenRmBinaryOpZx(context, () => context.Emit(OpCodes.Add));
        }

        public static void Uhadd_V(AILEmitterCtx context)
        {
            AOpCodeSimdReg op = (AOpCodeSimdReg)context.CurrOp;

            if (AOptimizations.UseSse2 && op.Size > 0)
            {
                Type[] typesSrl       = new Type[] { VectorUIntTypesPerSizeLog2[op.Size], typeof(byte) };
                Type[] typesAndXorAdd = new Type[] { VectorUIntTypesPerSizeLog2[op.Size], VectorUIntTypesPerSizeLog2[op.Size] };

                EmitLdvecWithUnsignedCast(context, op.Rn, op.Size);

                context.Emit(OpCodes.Dup);
                context.EmitStvectmp();

                EmitLdvecWithUnsignedCast(context, op.Rm, op.Size);

                context.Emit(OpCodes.Dup);
                context.EmitStvectmp2();

                context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.And), typesAndXorAdd));

                context.EmitLdvectmp();
                context.EmitLdvectmp2();

                context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.Xor), typesAndXorAdd));

                context.EmitLdc_I4(1);
                context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.ShiftRightLogical), typesSrl));

                context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.Add), typesAndXorAdd));

                EmitStvecWithUnsignedCast(context, op.Rd, op.Size);

                if (op.RegisterSize == ARegisterSize.Simd64) EmitVectorZeroUpper(context, op.Rd);
            }
            else
            {
                EmitVectorBinaryOpZx(context, () =>
                {
                    context.Emit(OpCodes.Add);

                    context.Emit(OpCodes.Ldc_I4_1);
                    context.Emit(OpCodes.Shr_Un);
                });
            }
        }

        public static void Uhsub_V(AILEmitterCtx context)
        {
            AOpCodeSimdReg op = (AOpCodeSimdReg)context.CurrOp;

            if (AOptimizations.UseSse2 && op.Size < 2)
            {
                Type[] typesAvgSub = new Type[] { VectorUIntTypesPerSizeLog2[op.Size], VectorUIntTypesPerSizeLog2[op.Size] };

                EmitLdvecWithUnsignedCast(context, op.Rn, op.Size);
                context.Emit(OpCodes.Dup);

                EmitLdvecWithUnsignedCast(context, op.Rm, op.Size);

                context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.Average), typesAvgSub));

                context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.Subtract), typesAvgSub));

                EmitStvecWithUnsignedCast(context, op.Rd, op.Size);

                if (op.RegisterSize == ARegisterSize.Simd64) EmitVectorZeroUpper(context, op.Rd);
            }
            else
            {
                EmitVectorBinaryOpZx(context, () =>
                {
                    context.Emit(OpCodes.Sub);

                    context.Emit(OpCodes.Ldc_I4_1);
                    context.Emit(OpCodes.Shr_Un);
                });
            }
        }

        public static void Umax_V(AILEmitterCtx context)
        {
            Type[] types = new Type[] { typeof(ulong), typeof(ulong) };

            MethodInfo mthdInfo = typeof(Math).GetMethod(nameof(Math.Max), types);

            EmitVectorBinaryOpZx(context, () => context.EmitCall(mthdInfo));
        }

        public static void Umaxp_V(AILEmitterCtx context)
        {
            Type[] types = new Type[] { typeof(ulong), typeof(ulong) };

            MethodInfo mthdInfo = typeof(Math).GetMethod(nameof(Math.Max), types);

            EmitVectorPairwiseOpZx(context, () => context.EmitCall(mthdInfo));
        }

        public static void Umin_V(AILEmitterCtx context)
        {
            Type[] types = new Type[] { typeof(ulong), typeof(ulong) };

            MethodInfo mthdInfo = typeof(Math).GetMethod(nameof(Math.Min), types);

            EmitVectorBinaryOpZx(context, () => context.EmitCall(mthdInfo));
        }

        public static void Uminp_V(AILEmitterCtx context)
        {
            Type[] types = new Type[] { typeof(ulong), typeof(ulong) };

            MethodInfo mthdInfo = typeof(Math).GetMethod(nameof(Math.Min), types);

            EmitVectorPairwiseOpZx(context, () => context.EmitCall(mthdInfo));
        }

        public static void Umlal_V(AILEmitterCtx context)
        {
            AOpCodeSimdReg op = (AOpCodeSimdReg)context.CurrOp;

            if (AOptimizations.UseSse41 && op.Size < 2)
            {
                Type[] typesSrl    = new Type[] { VectorUIntTypesPerSizeLog2[op.Size], typeof(byte) };
                Type[] typesCvt    = new Type[] { VectorUIntTypesPerSizeLog2[op.Size] };
                Type[] typesMulAdd = new Type[] { VectorIntTypesPerSizeLog2 [op.Size + 1],
                                                  VectorIntTypesPerSizeLog2 [op.Size + 1] };

                Type typeMul = op.Size == 0 ? typeof(Sse2) : typeof(Sse41);

                string nameCvt = op.Size == 0
                    ? nameof(Sse41.ConvertToVector128Int16)
                    : nameof(Sse41.ConvertToVector128Int32);

                int numBytes = op.RegisterSize == ARegisterSize.Simd128 ? 8 : 0;

                EmitLdvecWithUnsignedCast(context, op.Rd, op.Size + 1);

                EmitLdvecWithUnsignedCast(context, op.Rn, op.Size);

                context.EmitLdc_I4(numBytes);
                context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.ShiftRightLogical128BitLane), typesSrl));

                context.EmitCall(typeof(Sse41).GetMethod(nameCvt, typesCvt));

                EmitLdvecWithUnsignedCast(context, op.Rm, op.Size);

                context.EmitLdc_I4(numBytes);
                context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.ShiftRightLogical128BitLane), typesSrl));

                context.EmitCall(typeof(Sse41).GetMethod(nameCvt, typesCvt));

                context.EmitCall(typeMul.GetMethod(nameof(Sse2.MultiplyLow), typesMulAdd));

                context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.Add), typesMulAdd));

                EmitStvecWithUnsignedCast(context, op.Rd, op.Size + 1);
            }
            else
            {
                EmitVectorWidenRnRmTernaryOpZx(context, () =>
                {
                    context.Emit(OpCodes.Mul);
                    context.Emit(OpCodes.Add);
                });
            }
        }

        public static void Umlsl_V(AILEmitterCtx context)
        {
            AOpCodeSimdReg op = (AOpCodeSimdReg)context.CurrOp;

            if (AOptimizations.UseSse41 && op.Size < 2)
            {
                Type[] typesSrl    = new Type[] { VectorUIntTypesPerSizeLog2[op.Size], typeof(byte) };
                Type[] typesCvt    = new Type[] { VectorUIntTypesPerSizeLog2[op.Size] };
                Type[] typesMulSub = new Type[] { VectorIntTypesPerSizeLog2 [op.Size + 1],
                                                  VectorIntTypesPerSizeLog2 [op.Size + 1] };

                Type typeMul = op.Size == 0 ? typeof(Sse2) : typeof(Sse41);

                string nameCvt = op.Size == 0
                    ? nameof(Sse41.ConvertToVector128Int16)
                    : nameof(Sse41.ConvertToVector128Int32);

                int numBytes = op.RegisterSize == ARegisterSize.Simd128 ? 8 : 0;

                EmitLdvecWithUnsignedCast(context, op.Rd, op.Size + 1);

                EmitLdvecWithUnsignedCast(context, op.Rn, op.Size);

                context.EmitLdc_I4(numBytes);
                context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.ShiftRightLogical128BitLane), typesSrl));

                context.EmitCall(typeof(Sse41).GetMethod(nameCvt, typesCvt));

                EmitLdvecWithUnsignedCast(context, op.Rm, op.Size);

                context.EmitLdc_I4(numBytes);
                context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.ShiftRightLogical128BitLane), typesSrl));

                context.EmitCall(typeof(Sse41).GetMethod(nameCvt, typesCvt));

                context.EmitCall(typeMul.GetMethod(nameof(Sse2.MultiplyLow), typesMulSub));

                context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.Subtract), typesMulSub));

                EmitStvecWithUnsignedCast(context, op.Rd, op.Size + 1);
            }
            else
            {
                EmitVectorWidenRnRmTernaryOpZx(context, () =>
                {
                    context.Emit(OpCodes.Mul);
                    context.Emit(OpCodes.Sub);
                });
            }
        }

        public static void Umull_V(AILEmitterCtx context)
        {
            EmitVectorWidenRnRmBinaryOpZx(context, () => context.Emit(OpCodes.Mul));
        }

        public static void Uqadd_S(AILEmitterCtx context)
        {
            EmitScalarSaturatingBinaryOpZx(context, SaturatingFlags.Add);
        }

        public static void Uqadd_V(AILEmitterCtx context)
        {
            EmitVectorSaturatingBinaryOpZx(context, SaturatingFlags.Add);
        }

        public static void Uqsub_S(AILEmitterCtx context)
        {
            EmitScalarSaturatingBinaryOpZx(context, SaturatingFlags.Sub);
        }

        public static void Uqsub_V(AILEmitterCtx context)
        {
            EmitVectorSaturatingBinaryOpZx(context, SaturatingFlags.Sub);
        }

        public static void Uqxtn_S(AILEmitterCtx context)
        {
            EmitSaturatingNarrowOp(context, SaturatingNarrowFlags.ScalarZxZx);
        }

        public static void Uqxtn_V(AILEmitterCtx context)
        {
            EmitSaturatingNarrowOp(context, SaturatingNarrowFlags.VectorZxZx);
        }

        public static void Urhadd_V(AILEmitterCtx context)
        {
            AOpCodeSimdReg op = (AOpCodeSimdReg)context.CurrOp;

            if (AOptimizations.UseSse2 && op.Size < 2)
            {
                Type[] typesAvg = new Type[] { VectorUIntTypesPerSizeLog2[op.Size], VectorUIntTypesPerSizeLog2[op.Size] };

                EmitLdvecWithUnsignedCast(context, op.Rn, op.Size);
                EmitLdvecWithUnsignedCast(context, op.Rm, op.Size);

                context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.Average), typesAvg));

                EmitStvecWithUnsignedCast(context, op.Rd, op.Size);

                if (op.RegisterSize == ARegisterSize.Simd64) EmitVectorZeroUpper(context, op.Rd);
            }
            else
            {
                EmitVectorBinaryOpZx(context, () =>
                {
                    context.Emit(OpCodes.Add);

                    context.Emit(OpCodes.Ldc_I4_1);
                    context.Emit(OpCodes.Add);

                    context.Emit(OpCodes.Ldc_I4_1);
                    context.Emit(OpCodes.Shr_Un);
                });
            }
        }

        public static void Usqadd_S(AILEmitterCtx context)
        {
            EmitScalarSaturatingBinaryOpZx(context, SaturatingFlags.Accumulate);
        }

        public static void Usqadd_V(AILEmitterCtx context)
        {
            EmitVectorSaturatingBinaryOpZx(context, SaturatingFlags.Accumulate);
        }

        public static void Usubl_V(AILEmitterCtx context)
        {
            if (AOptimizations.UseSse41)
            {
                AOpCodeSimdReg op = (AOpCodeSimdReg)context.CurrOp;

                Type[] typesSrl = new Type[] { VectorUIntTypesPerSizeLog2[op.Size], typeof(byte) };
                Type[] typesCvt = new Type[] { VectorUIntTypesPerSizeLog2[op.Size] };
                Type[] typesSub = new Type[] { VectorUIntTypesPerSizeLog2[op.Size + 1],
                                               VectorUIntTypesPerSizeLog2[op.Size + 1] };

                string[] namesCvt = new string[] { nameof(Sse41.ConvertToVector128Int16),
                                                   nameof(Sse41.ConvertToVector128Int32),
                                                   nameof(Sse41.ConvertToVector128Int64) };

                int numBytes = op.RegisterSize == ARegisterSize.Simd128 ? 8 : 0;

                EmitLdvecWithUnsignedCast(context, op.Rn, op.Size);

                context.EmitLdc_I4(numBytes);
                context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.ShiftRightLogical128BitLane), typesSrl));

                context.EmitCall(typeof(Sse41).GetMethod(namesCvt[op.Size], typesCvt));

                EmitLdvecWithUnsignedCast(context, op.Rm, op.Size);

                context.EmitLdc_I4(numBytes);
                context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.ShiftRightLogical128BitLane), typesSrl));

                context.EmitCall(typeof(Sse41).GetMethod(namesCvt[op.Size], typesCvt));

                context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.Subtract), typesSub));

                EmitStvecWithUnsignedCast(context, op.Rd, op.Size + 1);
            }
            else
            {
                EmitVectorWidenRnRmBinaryOpZx(context, () => context.Emit(OpCodes.Sub));
            }
        }

        public static void Usubw_V(AILEmitterCtx context)
        {
            EmitVectorWidenRmBinaryOpZx(context, () => context.Emit(OpCodes.Sub));
        }

        private static void EmitAbs(AILEmitterCtx context)
        {
            AILLabel lblTrue = new AILLabel();

            context.Emit(OpCodes.Dup);
            context.Emit(OpCodes.Ldc_I4_0);
            context.Emit(OpCodes.Bge_S, lblTrue);

            context.Emit(OpCodes.Neg);

            context.MarkLabel(lblTrue);
        }

        private static void EmitAddLongPairwise(AILEmitterCtx context, bool signed, bool accumulate)
        {
            AOpCodeSimd op = (AOpCodeSimd)context.CurrOp;

            int words = op.GetBitsCount() >> 4;
            int pairs = words >> op.Size;

            for (int index = 0; index < pairs; index++)
            {
                int idx = index << 1;

                EmitVectorExtract(context, op.Rn, idx,     op.Size, signed);
                EmitVectorExtract(context, op.Rn, idx + 1, op.Size, signed);

                context.Emit(OpCodes.Add);

                if (accumulate)
                {
                    EmitVectorExtract(context, op.Rd, index, op.Size + 1, signed);

                    context.Emit(OpCodes.Add);
                }

                EmitVectorInsertTmp(context, index, op.Size + 1);
            }

            context.EmitLdvectmp();
            context.EmitStvec(op.Rd);

            if (op.RegisterSize == ARegisterSize.Simd64) EmitVectorZeroUpper(context, op.Rd);
        }

        private static void EmitDoublingMultiplyHighHalf(AILEmitterCtx context, bool round)
        {
            AOpCodeSimdReg op = (AOpCodeSimdReg)context.CurrOp;

            int eSize = 8 << op.Size;

            context.Emit(OpCodes.Mul);

            if (!round)
            {
                context.EmitAsr(eSize - 1);
            }
            else
            {
                long roundConst = 1L << (eSize - 1);

                AILLabel lblTrue = new AILLabel();

                context.EmitLsl(1);

                context.EmitLdc_I8(roundConst);

                context.Emit(OpCodes.Add);

                context.EmitAsr(eSize);

                context.Emit(OpCodes.Dup);
                context.EmitLdc_I8((long)int.MinValue);
                context.Emit(OpCodes.Bne_Un_S, lblTrue);

                context.Emit(OpCodes.Neg);

                context.MarkLabel(lblTrue);
            }
        }

        private static void EmitHighNarrow(AILEmitterCtx context, Action emit, bool round)
        {
            AOpCodeSimdReg op = (AOpCodeSimdReg)context.CurrOp;

            int elems = 8 >> op.Size;

            int eSize = 8 << op.Size;

            int part = op.RegisterSize == ARegisterSize.Simd128 ? elems : 0;

            long roundConst = 1L << (eSize - 1);

            if (part != 0)
            {
                context.EmitLdvec(op.Rd);
                context.EmitStvectmp();
            }

            for (int index = 0; index < elems; index++)
            {
                EmitVectorExtractZx(context, op.Rn, index, op.Size + 1);
                EmitVectorExtractZx(context, op.Rm, index, op.Size + 1);

                emit();

                if (round)
                {
                    context.EmitLdc_I8(roundConst);

                    context.Emit(OpCodes.Add);
                }

                context.EmitLsr(eSize);

                EmitVectorInsertTmp(context, part + index, op.Size);
            }

            context.EmitLdvectmp();
            context.EmitStvec(op.Rd);

            if (part == 0) EmitVectorZeroUpper(context, op.Rd);
        }
    }
}
