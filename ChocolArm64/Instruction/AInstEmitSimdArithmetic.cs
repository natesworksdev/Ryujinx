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
    static partial class AInstEmit
    {
        public static void Abs_S(AilEmitterCtx context)
        {
            EmitScalarUnaryOpSx(context, () => EmitAbs(context));
        }

        public static void Abs_V(AilEmitterCtx context)
        {
            EmitVectorUnaryOpSx(context, () => EmitAbs(context));
        }

        public static void Add_S(AilEmitterCtx context)
        {
            EmitScalarBinaryOpZx(context, () => context.Emit(OpCodes.Add));
        }

        public static void Add_V(AilEmitterCtx context)
        {
            if (AOptimizations.UseSse2)
            {
                EmitSse2Op(context, nameof(Sse2.Add));
            }
            else
            {
                EmitVectorBinaryOpZx(context, () => context.Emit(OpCodes.Add));
            }
        }

        public static void Addhn_V(AilEmitterCtx context)
        {
            EmitHighNarrow(context, () => context.Emit(OpCodes.Add), round: false);
        }

        public static void Addp_S(AilEmitterCtx context)
        {
            AOpCodeSimd op = (AOpCodeSimd)context.CurrOp;

            EmitVectorExtractZx(context, op.Rn, 0, op.Size);
            EmitVectorExtractZx(context, op.Rn, 1, op.Size);

            context.Emit(OpCodes.Add);

            EmitScalarSet(context, op.Rd, op.Size);
        }

        public static void Addp_V(AilEmitterCtx context)
        {
            EmitVectorPairwiseOpZx(context, () => context.Emit(OpCodes.Add));
        }

        public static void Addv_V(AilEmitterCtx context)
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

        public static void Cls_V(AilEmitterCtx context)
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

            if (op.RegisterSize == ARegisterSize.Simd64)
            {
                EmitVectorZeroUpper(context, op.Rd);
            }
        }

        public static void Clz_V(AilEmitterCtx context)
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

            if (op.RegisterSize == ARegisterSize.Simd64)
            {
                EmitVectorZeroUpper(context, op.Rd);
            }
        }

        public static void Cnt_V(AilEmitterCtx context)
        {
            AOpCodeSimd op = (AOpCodeSimd)context.CurrOp;

            int elems = op.RegisterSize == ARegisterSize.Simd128 ? 16 : 8;

            for (int index = 0; index < elems; index++)
            {
                EmitVectorExtractZx(context, op.Rn, index, 0);

                if (Popcnt.IsSupported)
                {
                    context.EmitCall(typeof(Popcnt).GetMethod(nameof(Popcnt.PopCount), new Type[] { typeof(ulong) }));
                }
                else
                {
                    ASoftFallback.EmitCall(context, nameof(ASoftFallback.CountSetBits8));
                }

                EmitVectorInsert(context, op.Rd, index, 0);
            }

            if (op.RegisterSize == ARegisterSize.Simd64)
            {
                EmitVectorZeroUpper(context, op.Rd);
            }
        }

        public static void Fabd_S(AilEmitterCtx context)
        {
            EmitScalarBinaryOpF(context, () =>
            {
                context.Emit(OpCodes.Sub);

                EmitUnaryMathCall(context, nameof(Math.Abs));
            });
        }

        public static void Fabs_S(AilEmitterCtx context)
        {
            EmitScalarUnaryOpF(context, () =>
            {
                EmitUnaryMathCall(context, nameof(Math.Abs));
            });
        }

        public static void Fabs_V(AilEmitterCtx context)
        {
            EmitVectorUnaryOpF(context, () =>
            {
                EmitUnaryMathCall(context, nameof(Math.Abs));
            });
        }

        public static void Fadd_S(AilEmitterCtx context)
        {
            if (AOptimizations.FastFp && AOptimizations.UseSse
                                      && AOptimizations.UseSse2)
            {
                EmitScalarSseOrSse2OpF(context, nameof(Sse.AddScalar));
            }
            else
            {
                EmitScalarBinaryOpF(context, () =>
                {
                    EmitSoftFloatCall(context, nameof(ASoftFloat32.FpAdd));
                });
            }
        }

        public static void Fadd_V(AilEmitterCtx context)
        {
            if (AOptimizations.FastFp && AOptimizations.UseSse
                                      && AOptimizations.UseSse2)
            {
                EmitVectorSseOrSse2OpF(context, nameof(Sse.Add));
            }
            else
            {
                EmitVectorBinaryOpF(context, () =>
                {
                    EmitSoftFloatCall(context, nameof(ASoftFloat32.FpAdd));
                });
            }
        }

        public static void Faddp_S(AilEmitterCtx context)
        {
            AOpCodeSimd op = (AOpCodeSimd)context.CurrOp;

            int sizeF = op.Size & 1;

            EmitVectorExtractF(context, op.Rn, 0, sizeF);
            EmitVectorExtractF(context, op.Rn, 1, sizeF);

            context.Emit(OpCodes.Add);

            EmitScalarSetF(context, op.Rd, sizeF);
        }

        public static void Faddp_V(AilEmitterCtx context)
        {
            EmitVectorPairwiseOpF(context, () => context.Emit(OpCodes.Add));
        }

        public static void Fdiv_S(AilEmitterCtx context)
        {
            if (AOptimizations.FastFp && AOptimizations.UseSse
                                      && AOptimizations.UseSse2)
            {
                EmitScalarSseOrSse2OpF(context, nameof(Sse.DivideScalar));
            }
            else
            {
                EmitScalarBinaryOpF(context, () =>
                {
                    EmitSoftFloatCall(context, nameof(ASoftFloat32.FpDiv));
                });
            }
        }

        public static void Fdiv_V(AilEmitterCtx context)
        {
            if (AOptimizations.FastFp && AOptimizations.UseSse
                                      && AOptimizations.UseSse2)
            {
                EmitVectorSseOrSse2OpF(context, nameof(Sse.Divide));
            }
            else
            {
                EmitVectorBinaryOpF(context, () =>
                {
                    EmitSoftFloatCall(context, nameof(ASoftFloat32.FpDiv));
                });
            }
        }

        public static void Fmadd_S(AilEmitterCtx context)
        {
            if (AOptimizations.FastFp && AOptimizations.UseSse2)
            {
                AOpCodeSimdReg op = (AOpCodeSimdReg)context.CurrOp;

                if (op.Size == 0)
                {
                    Type[] types = new Type[] { typeof(Vector128<float>), typeof(Vector128<float>) };

                    context.EmitLdvec(op.Ra);
                    context.EmitLdvec(op.Rn);
                    context.EmitLdvec(op.Rm);

                    context.EmitCall(typeof(Sse).GetMethod(nameof(Sse.MultiplyScalar), types));
                    context.EmitCall(typeof(Sse).GetMethod(nameof(Sse.AddScalar),      types));

                    context.EmitStvec(op.Rd);

                    EmitVectorZero32_128(context, op.Rd);
                }
                else /* if (Op.Size == 1) */
                {
                    Type[] types = new Type[] { typeof(Vector128<double>), typeof(Vector128<double>) };

                    EmitLdvecWithCastToDouble(context, op.Ra);
                    EmitLdvecWithCastToDouble(context, op.Rn);
                    EmitLdvecWithCastToDouble(context, op.Rm);

                    context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.MultiplyScalar), types));
                    context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.AddScalar),      types));

                    EmitStvecWithCastFromDouble(context, op.Rd);

                    EmitVectorZeroUpper(context, op.Rd);
                }
            }
            else
            {
                EmitScalarTernaryRaOpF(context, () =>
                {
                    EmitSoftFloatCall(context, nameof(ASoftFloat32.FpMulAdd));
                });
            }
        }

        public static void Fmax_S(AilEmitterCtx context)
        {
            if (AOptimizations.FastFp && AOptimizations.UseSse
                                      && AOptimizations.UseSse2)
            {
                EmitScalarSseOrSse2OpF(context, nameof(Sse.MaxScalar));
            }
            else
            {
                EmitScalarBinaryOpF(context, () =>
                {
                    EmitSoftFloatCall(context, nameof(ASoftFloat32.FpMax));
                });
            }
        }

        public static void Fmax_V(AilEmitterCtx context)
        {
            if (AOptimizations.FastFp && AOptimizations.UseSse
                                      && AOptimizations.UseSse2)
            {
                EmitVectorSseOrSse2OpF(context, nameof(Sse.Max));
            }
            else
            {
                EmitVectorBinaryOpF(context, () =>
                {
                    EmitSoftFloatCall(context, nameof(ASoftFloat32.FpMax));
                });
            }
        }

        public static void Fmaxnm_S(AilEmitterCtx context)
        {
            EmitScalarBinaryOpF(context, () =>
            {
                EmitSoftFloatCall(context, nameof(ASoftFloat32.FpMaxNum));
            });
        }

        public static void Fmaxnm_V(AilEmitterCtx context)
        {
            EmitVectorBinaryOpF(context, () =>
            {
                EmitSoftFloatCall(context, nameof(ASoftFloat32.FpMaxNum));
            });
        }

        public static void Fmaxp_V(AilEmitterCtx context)
        {
            EmitVectorPairwiseOpF(context, () =>
            {
                EmitSoftFloatCall(context, nameof(ASoftFloat32.FpMax));
            });
        }

        public static void Fmin_S(AilEmitterCtx context)
        {
            if (AOptimizations.FastFp && AOptimizations.UseSse
                                      && AOptimizations.UseSse2)
            {
                EmitScalarSseOrSse2OpF(context, nameof(Sse.MinScalar));
            }
            else
            {
                EmitScalarBinaryOpF(context, () =>
                {
                    EmitSoftFloatCall(context, nameof(ASoftFloat32.FpMin));
                });
            }
        }

        public static void Fmin_V(AilEmitterCtx context)
        {
            if (AOptimizations.FastFp && AOptimizations.UseSse
                                      && AOptimizations.UseSse2)
            {
                EmitVectorSseOrSse2OpF(context, nameof(Sse.Min));
            }
            else
            {
                EmitVectorBinaryOpF(context, () =>
                {
                    EmitSoftFloatCall(context, nameof(ASoftFloat32.FpMin));
                });
            }
        }

        public static void Fminnm_S(AilEmitterCtx context)
        {
            EmitScalarBinaryOpF(context, () =>
            {
                EmitSoftFloatCall(context, nameof(ASoftFloat32.FpMinNum));
            });
        }

        public static void Fminnm_V(AilEmitterCtx context)
        {
            EmitVectorBinaryOpF(context, () =>
            {
                EmitSoftFloatCall(context, nameof(ASoftFloat32.FpMinNum));
            });
        }

        public static void Fminp_V(AilEmitterCtx context)
        {
            EmitVectorPairwiseOpF(context, () =>
            {
                EmitSoftFloatCall(context, nameof(ASoftFloat32.FpMin));
            });
        }

        public static void Fmla_Se(AilEmitterCtx context)
        {
            EmitScalarTernaryOpByElemF(context, () =>
            {
                context.Emit(OpCodes.Mul);
                context.Emit(OpCodes.Add);
            });
        }

        public static void Fmla_V(AilEmitterCtx context)
        {
            EmitVectorTernaryOpF(context, () =>
            {
                context.Emit(OpCodes.Mul);
                context.Emit(OpCodes.Add);
            });
        }

        public static void Fmla_Ve(AilEmitterCtx context)
        {
            EmitVectorTernaryOpByElemF(context, () =>
            {
                context.Emit(OpCodes.Mul);
                context.Emit(OpCodes.Add);
            });
        }

        public static void Fmls_Se(AilEmitterCtx context)
        {
            EmitScalarTernaryOpByElemF(context, () =>
            {
                context.Emit(OpCodes.Mul);
                context.Emit(OpCodes.Sub);
            });
        }

        public static void Fmls_V(AilEmitterCtx context)
        {
            EmitVectorTernaryOpF(context, () =>
            {
                context.Emit(OpCodes.Mul);
                context.Emit(OpCodes.Sub);
            });
        }

        public static void Fmls_Ve(AilEmitterCtx context)
        {
            EmitVectorTernaryOpByElemF(context, () =>
            {
                context.Emit(OpCodes.Mul);
                context.Emit(OpCodes.Sub);
            });
        }

        public static void Fmsub_S(AilEmitterCtx context)
        {
            if (AOptimizations.FastFp && AOptimizations.UseSse2)
            {
                AOpCodeSimdReg op = (AOpCodeSimdReg)context.CurrOp;

                if (op.Size == 0)
                {
                    Type[] types = new Type[] { typeof(Vector128<float>), typeof(Vector128<float>) };

                    context.EmitLdvec(op.Ra);
                    context.EmitLdvec(op.Rn);
                    context.EmitLdvec(op.Rm);

                    context.EmitCall(typeof(Sse).GetMethod(nameof(Sse.MultiplyScalar), types));
                    context.EmitCall(typeof(Sse).GetMethod(nameof(Sse.SubtractScalar), types));

                    context.EmitStvec(op.Rd);

                    EmitVectorZero32_128(context, op.Rd);
                }
                else /* if (Op.Size == 1) */
                {
                    Type[] types = new Type[] { typeof(Vector128<double>), typeof(Vector128<double>) };

                    EmitLdvecWithCastToDouble(context, op.Ra);
                    EmitLdvecWithCastToDouble(context, op.Rn);
                    EmitLdvecWithCastToDouble(context, op.Rm);

                    context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.MultiplyScalar), types));
                    context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.SubtractScalar), types));

                    EmitStvecWithCastFromDouble(context, op.Rd);

                    EmitVectorZeroUpper(context, op.Rd);
                }
            }
            else
            {
                EmitScalarTernaryRaOpF(context, () =>
                {
                    EmitSoftFloatCall(context, nameof(ASoftFloat32.FpMulSub));
                });
            }
        }

        public static void Fmul_S(AilEmitterCtx context)
        {
            if (AOptimizations.FastFp && AOptimizations.UseSse
                                      && AOptimizations.UseSse2)
            {
                EmitScalarSseOrSse2OpF(context, nameof(Sse.MultiplyScalar));
            }
            else
            {
                EmitScalarBinaryOpF(context, () =>
                {
                    EmitSoftFloatCall(context, nameof(ASoftFloat32.FpMul));
                });
            }
        }

        public static void Fmul_Se(AilEmitterCtx context)
        {
            EmitScalarBinaryOpByElemF(context, () => context.Emit(OpCodes.Mul));
        }

        public static void Fmul_V(AilEmitterCtx context)
        {
            if (AOptimizations.FastFp && AOptimizations.UseSse
                                      && AOptimizations.UseSse2)
            {
                EmitVectorSseOrSse2OpF(context, nameof(Sse.Multiply));
            }
            else
            {
                EmitVectorBinaryOpF(context, () =>
                {
                    EmitSoftFloatCall(context, nameof(ASoftFloat32.FpMul));
                });
            }
        }

        public static void Fmul_Ve(AilEmitterCtx context)
        {
            EmitVectorBinaryOpByElemF(context, () => context.Emit(OpCodes.Mul));
        }

        public static void Fmulx_S(AilEmitterCtx context)
        {
            EmitScalarBinaryOpF(context, () =>
            {
                EmitSoftFloatCall(context, nameof(ASoftFloat32.FpMulX));
            });
        }

        public static void Fmulx_Se(AilEmitterCtx context)
        {
            EmitScalarBinaryOpByElemF(context, () =>
            {
                EmitSoftFloatCall(context, nameof(ASoftFloat32.FpMulX));
            });
        }

        public static void Fmulx_V(AilEmitterCtx context)
        {
            EmitVectorBinaryOpF(context, () =>
            {
                EmitSoftFloatCall(context, nameof(ASoftFloat32.FpMulX));
            });
        }

        public static void Fmulx_Ve(AilEmitterCtx context)
        {
            EmitVectorBinaryOpByElemF(context, () =>
            {
                EmitSoftFloatCall(context, nameof(ASoftFloat32.FpMulX));
            });
        }

        public static void Fneg_S(AilEmitterCtx context)
        {
            EmitScalarUnaryOpF(context, () => context.Emit(OpCodes.Neg));
        }

        public static void Fneg_V(AilEmitterCtx context)
        {
            EmitVectorUnaryOpF(context, () => context.Emit(OpCodes.Neg));
        }

        public static void Fnmadd_S(AilEmitterCtx context)
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

        public static void Fnmsub_S(AilEmitterCtx context)
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

        public static void Fnmul_S(AilEmitterCtx context)
        {
            EmitScalarBinaryOpF(context, () =>
            {
                context.Emit(OpCodes.Mul);
                context.Emit(OpCodes.Neg);
            });
        }

        public static void Frecpe_S(AilEmitterCtx context)
        {
            EmitScalarUnaryOpF(context, () =>
            {
                EmitUnarySoftFloatCall(context, nameof(ASoftFloat.RecipEstimate));
            });
        }

        public static void Frecpe_V(AilEmitterCtx context)
        {
            EmitVectorUnaryOpF(context, () =>
            {
                EmitUnarySoftFloatCall(context, nameof(ASoftFloat.RecipEstimate));
            });
        }

        public static void Frecps_S(AilEmitterCtx context)
        {
            if (AOptimizations.FastFp && AOptimizations.UseSse2)
            {
                AOpCodeSimdReg op = (AOpCodeSimdReg)context.CurrOp;

                int sizeF = op.Size & 1;

                if (sizeF == 0)
                {
                    Type[] types = new Type[] { typeof(float) };

                    context.EmitLdc_R4(2f);
                    context.EmitCall(typeof(Sse).GetMethod(nameof(Sse.SetScalarVector128), types));

                    types = new Type[] { typeof(Vector128<float>), typeof(Vector128<float>) };

                    context.EmitLdvec(op.Rn);
                    context.EmitLdvec(op.Rm);

                    context.EmitCall(typeof(Sse).GetMethod(nameof(Sse.MultiplyScalar), types));
                    context.EmitCall(typeof(Sse).GetMethod(nameof(Sse.SubtractScalar), types));

                    context.EmitStvec(op.Rd);

                    EmitVectorZero32_128(context, op.Rd);
                }
                else /* if (SizeF == 1) */
                {
                    Type[] types = new Type[] { typeof(double) };

                    context.EmitLdc_R8(2d);
                    context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.SetScalarVector128), types));

                    types = new Type[] { typeof(Vector128<double>), typeof(Vector128<double>) };

                    EmitLdvecWithCastToDouble(context, op.Rn);
                    EmitLdvecWithCastToDouble(context, op.Rm);

                    context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.MultiplyScalar), types));
                    context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.SubtractScalar), types));

                    EmitStvecWithCastFromDouble(context, op.Rd);

                    EmitVectorZeroUpper(context, op.Rd);
                }
            }
            else
            {
                EmitScalarBinaryOpF(context, () =>
                {
                    EmitSoftFloatCall(context, nameof(ASoftFloat32.FpRecipStepFused));
                });
            }
        }

        public static void Frecps_V(AilEmitterCtx context)
        {
            if (AOptimizations.FastFp && AOptimizations.UseSse2)
            {
                AOpCodeSimdReg op = (AOpCodeSimdReg)context.CurrOp;

                int sizeF = op.Size & 1;

                if (sizeF == 0)
                {
                    Type[] types = new Type[] { typeof(float) };

                    context.EmitLdc_R4(2f);
                    context.EmitCall(typeof(Sse).GetMethod(nameof(Sse.SetAllVector128), types));

                    types = new Type[] { typeof(Vector128<float>), typeof(Vector128<float>) };

                    context.EmitLdvec(op.Rn);
                    context.EmitLdvec(op.Rm);

                    context.EmitCall(typeof(Sse).GetMethod(nameof(Sse.Multiply), types));
                    context.EmitCall(typeof(Sse).GetMethod(nameof(Sse.Subtract), types));

                    context.EmitStvec(op.Rd);

                    if (op.RegisterSize == ARegisterSize.Simd64)
                    {
                        EmitVectorZeroUpper(context, op.Rd);
                    }
                }
                else /* if (SizeF == 1) */
                {
                    Type[] types = new Type[] { typeof(double) };

                    context.EmitLdc_R8(2d);
                    context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.SetAllVector128), types));

                    types = new Type[] { typeof(Vector128<double>), typeof(Vector128<double>) };

                    EmitLdvecWithCastToDouble(context, op.Rn);
                    EmitLdvecWithCastToDouble(context, op.Rm);

                    context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.Multiply), types));
                    context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.Subtract), types));

                    EmitStvecWithCastFromDouble(context, op.Rd);
                }
            }
            else
            {
                EmitVectorBinaryOpF(context, () =>
                {
                    EmitSoftFloatCall(context, nameof(ASoftFloat32.FpRecipStepFused));
                });
            }
        }

        public static void Frecpx_S(AilEmitterCtx context)
        {
            EmitScalarUnaryOpF(context, () =>
            {
                EmitSoftFloatCall(context, nameof(ASoftFloat32.FpRecpX));
            });
        }

        public static void Frinta_S(AilEmitterCtx context)
        {
            AOpCodeSimd op = (AOpCodeSimd)context.CurrOp;

            EmitVectorExtractF(context, op.Rn, 0, op.Size);

            EmitRoundMathCall(context, MidpointRounding.AwayFromZero);

            EmitScalarSetF(context, op.Rd, op.Size);
        }

        public static void Frinta_V(AilEmitterCtx context)
        {
            EmitVectorUnaryOpF(context, () =>
            {
                EmitRoundMathCall(context, MidpointRounding.AwayFromZero);
            });
        }

        public static void Frinti_S(AilEmitterCtx context)
        {
            AOpCodeSimd op = (AOpCodeSimd)context.CurrOp;

            EmitScalarUnaryOpF(context, () =>
            {
                context.EmitLdarg(ATranslatedSub.StateArgIdx);

                if (op.Size == 0)
                {
                    AVectorHelper.EmitCall(context, nameof(AVectorHelper.RoundF));
                }
                else if (op.Size == 1)
                {
                    AVectorHelper.EmitCall(context, nameof(AVectorHelper.Round));
                }
                else
                {
                    throw new InvalidOperationException();
                }
            });
        }

        public static void Frinti_V(AilEmitterCtx context)
        {
            AOpCodeSimd op = (AOpCodeSimd)context.CurrOp;

            int sizeF = op.Size & 1;

            EmitVectorUnaryOpF(context, () =>
            {
                context.EmitLdarg(ATranslatedSub.StateArgIdx);

                if (sizeF == 0)
                {
                    AVectorHelper.EmitCall(context, nameof(AVectorHelper.RoundF));
                }
                else if (sizeF == 1)
                {
                    AVectorHelper.EmitCall(context, nameof(AVectorHelper.Round));
                }
                else
                {
                    throw new InvalidOperationException();
                }
            });
        }

        public static void Frintm_S(AilEmitterCtx context)
        {
            EmitScalarUnaryOpF(context, () =>
            {
                EmitUnaryMathCall(context, nameof(Math.Floor));
            });
        }

        public static void Frintm_V(AilEmitterCtx context)
        {
            EmitVectorUnaryOpF(context, () =>
            {
                EmitUnaryMathCall(context, nameof(Math.Floor));
            });
        }

        public static void Frintn_S(AilEmitterCtx context)
        {
            AOpCodeSimd op = (AOpCodeSimd)context.CurrOp;

            EmitVectorExtractF(context, op.Rn, 0, op.Size);

            EmitRoundMathCall(context, MidpointRounding.ToEven);

            EmitScalarSetF(context, op.Rd, op.Size);
        }

        public static void Frintn_V(AilEmitterCtx context)
        {
            EmitVectorUnaryOpF(context, () =>
            {
                EmitRoundMathCall(context, MidpointRounding.ToEven);
            });
        }

        public static void Frintp_S(AilEmitterCtx context)
        {
            EmitScalarUnaryOpF(context, () =>
            {
                EmitUnaryMathCall(context, nameof(Math.Ceiling));
            });
        }

        public static void Frintp_V(AilEmitterCtx context)
        {
            EmitVectorUnaryOpF(context, () =>
            {
                EmitUnaryMathCall(context, nameof(Math.Ceiling));
            });
        }

        public static void Frintx_S(AilEmitterCtx context)
        {
            AOpCodeSimd op = (AOpCodeSimd)context.CurrOp;

            EmitScalarUnaryOpF(context, () =>
            {
                context.EmitLdarg(ATranslatedSub.StateArgIdx);

                if (op.Size == 0)
                {
                    AVectorHelper.EmitCall(context, nameof(AVectorHelper.RoundF));
                }
                else if (op.Size == 1)
                {
                    AVectorHelper.EmitCall(context, nameof(AVectorHelper.Round));
                }
                else
                {
                    throw new InvalidOperationException();
                }
            });
        }

        public static void Frintx_V(AilEmitterCtx context)
        {
            AOpCodeSimd op = (AOpCodeSimd)context.CurrOp;

            EmitVectorUnaryOpF(context, () =>
            {
                context.EmitLdarg(ATranslatedSub.StateArgIdx);

                if (op.Size == 0)
                {
                    AVectorHelper.EmitCall(context, nameof(AVectorHelper.RoundF));
                }
                else if (op.Size == 1)
                {
                    AVectorHelper.EmitCall(context, nameof(AVectorHelper.Round));
                }
                else
                {
                    throw new InvalidOperationException();
                }
            });
        }

        public static void Frsqrte_S(AilEmitterCtx context)
        {
            EmitScalarUnaryOpF(context, () =>
            {
                EmitUnarySoftFloatCall(context, nameof(ASoftFloat.InvSqrtEstimate));
            });
        }

        public static void Frsqrte_V(AilEmitterCtx context)
        {
            EmitVectorUnaryOpF(context, () =>
            {
                EmitUnarySoftFloatCall(context, nameof(ASoftFloat.InvSqrtEstimate));
            });
        }

        public static void Frsqrts_S(AilEmitterCtx context)
        {
            if (AOptimizations.FastFp && AOptimizations.UseSse2)
            {
                AOpCodeSimdReg op = (AOpCodeSimdReg)context.CurrOp;

                int sizeF = op.Size & 1;

                if (sizeF == 0)
                {
                    Type[] types = new Type[] { typeof(float) };

                    context.EmitLdc_R4(0.5f);
                    context.EmitCall(typeof(Sse).GetMethod(nameof(Sse.SetScalarVector128), types));

                    context.EmitLdc_R4(3f);
                    context.EmitCall(typeof(Sse).GetMethod(nameof(Sse.SetScalarVector128), types));

                    types = new Type[] { typeof(Vector128<float>), typeof(Vector128<float>) };

                    context.EmitLdvec(op.Rn);
                    context.EmitLdvec(op.Rm);

                    context.EmitCall(typeof(Sse).GetMethod(nameof(Sse.MultiplyScalar), types));
                    context.EmitCall(typeof(Sse).GetMethod(nameof(Sse.SubtractScalar), types));
                    context.EmitCall(typeof(Sse).GetMethod(nameof(Sse.MultiplyScalar), types));

                    context.EmitStvec(op.Rd);

                    EmitVectorZero32_128(context, op.Rd);
                }
                else /* if (SizeF == 1) */
                {
                    Type[] types = new Type[] { typeof(double) };

                    context.EmitLdc_R8(0.5d);
                    context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.SetScalarVector128), types));

                    context.EmitLdc_R8(3d);
                    context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.SetScalarVector128), types));

                    types = new Type[] { typeof(Vector128<double>), typeof(Vector128<double>) };

                    EmitLdvecWithCastToDouble(context, op.Rn);
                    EmitLdvecWithCastToDouble(context, op.Rm);

                    context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.MultiplyScalar), types));
                    context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.SubtractScalar), types));
                    context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.MultiplyScalar), types));

                    EmitStvecWithCastFromDouble(context, op.Rd);

                    EmitVectorZeroUpper(context, op.Rd);
                }
            }
            else
            {
                EmitScalarBinaryOpF(context, () =>
                {
                    EmitSoftFloatCall(context, nameof(ASoftFloat32.FprSqrtStepFused));
                });
            }
        }

        public static void Frsqrts_V(AilEmitterCtx context)
        {
            if (AOptimizations.FastFp && AOptimizations.UseSse2)
            {
                AOpCodeSimdReg op = (AOpCodeSimdReg)context.CurrOp;

                int sizeF = op.Size & 1;

                if (sizeF == 0)
                {
                    Type[] types = new Type[] { typeof(float) };

                    context.EmitLdc_R4(0.5f);
                    context.EmitCall(typeof(Sse).GetMethod(nameof(Sse.SetAllVector128), types));

                    context.EmitLdc_R4(3f);
                    context.EmitCall(typeof(Sse).GetMethod(nameof(Sse.SetAllVector128), types));

                    types = new Type[] { typeof(Vector128<float>), typeof(Vector128<float>) };

                    context.EmitLdvec(op.Rn);
                    context.EmitLdvec(op.Rm);

                    context.EmitCall(typeof(Sse).GetMethod(nameof(Sse.Multiply), types));
                    context.EmitCall(typeof(Sse).GetMethod(nameof(Sse.Subtract), types));
                    context.EmitCall(typeof(Sse).GetMethod(nameof(Sse.Multiply), types));

                    context.EmitStvec(op.Rd);

                    if (op.RegisterSize == ARegisterSize.Simd64)
                    {
                        EmitVectorZeroUpper(context, op.Rd);
                    }
                }
                else /* if (SizeF == 1) */
                {
                    Type[] types = new Type[] { typeof(double) };

                    context.EmitLdc_R8(0.5d);
                    context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.SetAllVector128), types));

                    context.EmitLdc_R8(3d);
                    context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.SetAllVector128), types));

                    types = new Type[] { typeof(Vector128<double>), typeof(Vector128<double>) };

                    EmitLdvecWithCastToDouble(context, op.Rn);
                    EmitLdvecWithCastToDouble(context, op.Rm);

                    context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.Multiply), types));
                    context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.Subtract), types));
                    context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.Multiply), types));

                    EmitStvecWithCastFromDouble(context, op.Rd);
                }
            }
            else
            {
                EmitVectorBinaryOpF(context, () =>
                {
                    EmitSoftFloatCall(context, nameof(ASoftFloat32.FprSqrtStepFused));
                });
            }
        }

        public static void Fsqrt_S(AilEmitterCtx context)
        {
            if (AOptimizations.FastFp && AOptimizations.UseSse
                                      && AOptimizations.UseSse2)
            {
                EmitScalarSseOrSse2OpF(context, nameof(Sse.SqrtScalar));
            }
            else
            {
                EmitScalarUnaryOpF(context, () =>
                {
                    EmitSoftFloatCall(context, nameof(ASoftFloat32.FpSqrt));
                });
            }
        }

        public static void Fsqrt_V(AilEmitterCtx context)
        {
            if (AOptimizations.FastFp && AOptimizations.UseSse
                                      && AOptimizations.UseSse2)
            {
                EmitVectorSseOrSse2OpF(context, nameof(Sse.Sqrt));
            }
            else
            {
                EmitVectorUnaryOpF(context, () =>
                {
                    EmitSoftFloatCall(context, nameof(ASoftFloat32.FpSqrt));
                });
            }
        }

        public static void Fsub_S(AilEmitterCtx context)
        {
            if (AOptimizations.FastFp && AOptimizations.UseSse
                                      && AOptimizations.UseSse2)
            {
                EmitScalarSseOrSse2OpF(context, nameof(Sse.SubtractScalar));
            }
            else
            {
                EmitScalarBinaryOpF(context, () =>
                {
                    EmitSoftFloatCall(context, nameof(ASoftFloat32.FpSub));
                });
            }
        }

        public static void Fsub_V(AilEmitterCtx context)
        {
            if (AOptimizations.FastFp && AOptimizations.UseSse
                                      && AOptimizations.UseSse2)
            {
                EmitVectorSseOrSse2OpF(context, nameof(Sse.Subtract));
            }
            else
            {
                EmitVectorBinaryOpF(context, () =>
                {
                    EmitSoftFloatCall(context, nameof(ASoftFloat32.FpSub));
                });
            }
        }

        public static void Mla_V(AilEmitterCtx context)
        {
            EmitVectorTernaryOpZx(context, () =>
            {
                context.Emit(OpCodes.Mul);
                context.Emit(OpCodes.Add);
            });
        }

        public static void Mla_Ve(AilEmitterCtx context)
        {
            EmitVectorTernaryOpByElemZx(context, () =>
            {
                context.Emit(OpCodes.Mul);
                context.Emit(OpCodes.Add);
            });
        }

        public static void Mls_V(AilEmitterCtx context)
        {
            EmitVectorTernaryOpZx(context, () =>
            {
                context.Emit(OpCodes.Mul);
                context.Emit(OpCodes.Sub);
            });
        }

        public static void Mls_Ve(AilEmitterCtx context)
        {
            EmitVectorTernaryOpByElemZx(context, () =>
            {
                context.Emit(OpCodes.Mul);
                context.Emit(OpCodes.Sub);
            });
        }

        public static void Mul_V(AilEmitterCtx context)
        {
            EmitVectorBinaryOpZx(context, () => context.Emit(OpCodes.Mul));
        }

        public static void Mul_Ve(AilEmitterCtx context)
        {
            EmitVectorBinaryOpByElemZx(context, () => context.Emit(OpCodes.Mul));
        }

        public static void Neg_S(AilEmitterCtx context)
        {
            EmitScalarUnaryOpSx(context, () => context.Emit(OpCodes.Neg));
        }

        public static void Neg_V(AilEmitterCtx context)
        {
            EmitVectorUnaryOpSx(context, () => context.Emit(OpCodes.Neg));
        }

        public static void Raddhn_V(AilEmitterCtx context)
        {
            EmitHighNarrow(context, () => context.Emit(OpCodes.Add), round: true);
        }

        public static void Rsubhn_V(AilEmitterCtx context)
        {
            EmitHighNarrow(context, () => context.Emit(OpCodes.Sub), round: true);
        }

        public static void Saba_V(AilEmitterCtx context)
        {
            EmitVectorTernaryOpSx(context, () =>
            {
                context.Emit(OpCodes.Sub);
                EmitAbs(context);

                context.Emit(OpCodes.Add);
            });
        }

        public static void Sabal_V(AilEmitterCtx context)
        {
            EmitVectorWidenRnRmTernaryOpSx(context, () =>
            {
                context.Emit(OpCodes.Sub);
                EmitAbs(context);

                context.Emit(OpCodes.Add);
            });
        }

        public static void Sabd_V(AilEmitterCtx context)
        {
            EmitVectorBinaryOpSx(context, () =>
            {
                context.Emit(OpCodes.Sub);
                EmitAbs(context);
            });
        }

        public static void Sabdl_V(AilEmitterCtx context)
        {
            EmitVectorWidenRnRmBinaryOpSx(context, () =>
            {
                context.Emit(OpCodes.Sub);
                EmitAbs(context);
            });
        }

        public static void Sadalp_V(AilEmitterCtx context)
        {
            EmitAddLongPairwise(context, signed: true, accumulate: true);
        }

        public static void Saddl_V(AilEmitterCtx context)
        {
            EmitVectorWidenRnRmBinaryOpSx(context, () => context.Emit(OpCodes.Add));
        }

        public static void Saddlp_V(AilEmitterCtx context)
        {
            EmitAddLongPairwise(context, signed: true, accumulate: false);
        }

        public static void Saddw_V(AilEmitterCtx context)
        {
            EmitVectorWidenRmBinaryOpSx(context, () => context.Emit(OpCodes.Add));
        }

        public static void Shadd_V(AilEmitterCtx context)
        {
            EmitVectorBinaryOpSx(context, () =>
            {
                context.Emit(OpCodes.Add);

                context.Emit(OpCodes.Ldc_I4_1);
                context.Emit(OpCodes.Shr);
            });
        }

        public static void Shsub_V(AilEmitterCtx context)
        {
            EmitVectorBinaryOpSx(context, () =>
            {
                context.Emit(OpCodes.Sub);

                context.Emit(OpCodes.Ldc_I4_1);
                context.Emit(OpCodes.Shr);
            });
        }

        public static void Smax_V(AilEmitterCtx context)
        {
            Type[] types = new Type[] { typeof(long), typeof(long) };

            MethodInfo mthdInfo = typeof(Math).GetMethod(nameof(Math.Max), types);

            EmitVectorBinaryOpSx(context, () => context.EmitCall(mthdInfo));
        }

        public static void Smaxp_V(AilEmitterCtx context)
        {
            Type[] types = new Type[] { typeof(long), typeof(long) };

            MethodInfo mthdInfo = typeof(Math).GetMethod(nameof(Math.Max), types);

            EmitVectorPairwiseOpSx(context, () => context.EmitCall(mthdInfo));
        }

        public static void Smin_V(AilEmitterCtx context)
        {
            Type[] types = new Type[] { typeof(long), typeof(long) };

            MethodInfo mthdInfo = typeof(Math).GetMethod(nameof(Math.Min), types);

            EmitVectorBinaryOpSx(context, () => context.EmitCall(mthdInfo));
        }

        public static void Sminp_V(AilEmitterCtx context)
        {
            Type[] types = new Type[] { typeof(long), typeof(long) };

            MethodInfo mthdInfo = typeof(Math).GetMethod(nameof(Math.Min), types);

            EmitVectorPairwiseOpSx(context, () => context.EmitCall(mthdInfo));
        }

        public static void Smlal_V(AilEmitterCtx context)
        {
            EmitVectorWidenRnRmTernaryOpSx(context, () =>
            {
                context.Emit(OpCodes.Mul);
                context.Emit(OpCodes.Add);
            });
        }

        public static void Smlsl_V(AilEmitterCtx context)
        {
            EmitVectorWidenRnRmTernaryOpSx(context, () =>
            {
                context.Emit(OpCodes.Mul);
                context.Emit(OpCodes.Sub);
            });
        }

        public static void Smull_V(AilEmitterCtx context)
        {
            EmitVectorWidenRnRmBinaryOpSx(context, () => context.Emit(OpCodes.Mul));
        }

        public static void Sqabs_S(AilEmitterCtx context)
        {
            EmitScalarSaturatingUnaryOpSx(context, () => EmitAbs(context));
        }

        public static void Sqabs_V(AilEmitterCtx context)
        {
            EmitVectorSaturatingUnaryOpSx(context, () => EmitAbs(context));
        }

        public static void Sqadd_S(AilEmitterCtx context)
        {
            EmitScalarSaturatingBinaryOpSx(context, SaturatingFlags.Add);
        }

        public static void Sqadd_V(AilEmitterCtx context)
        {
            EmitVectorSaturatingBinaryOpSx(context, SaturatingFlags.Add);
        }

        public static void Sqdmulh_S(AilEmitterCtx context)
        {
            EmitSaturatingBinaryOp(context, () => EmitDoublingMultiplyHighHalf(context, round: false), SaturatingFlags.ScalarSx);
        }

        public static void Sqdmulh_V(AilEmitterCtx context)
        {
            EmitSaturatingBinaryOp(context, () => EmitDoublingMultiplyHighHalf(context, round: false), SaturatingFlags.VectorSx);
        }

        public static void Sqneg_S(AilEmitterCtx context)
        {
            EmitScalarSaturatingUnaryOpSx(context, () => context.Emit(OpCodes.Neg));
        }

        public static void Sqneg_V(AilEmitterCtx context)
        {
            EmitVectorSaturatingUnaryOpSx(context, () => context.Emit(OpCodes.Neg));
        }

        public static void Sqrdmulh_S(AilEmitterCtx context)
        {
            EmitSaturatingBinaryOp(context, () => EmitDoublingMultiplyHighHalf(context, round: true), SaturatingFlags.ScalarSx);
        }

        public static void Sqrdmulh_V(AilEmitterCtx context)
        {
            EmitSaturatingBinaryOp(context, () => EmitDoublingMultiplyHighHalf(context, round: true), SaturatingFlags.VectorSx);
        }

        public static void Sqsub_S(AilEmitterCtx context)
        {
            EmitScalarSaturatingBinaryOpSx(context, SaturatingFlags.Sub);
        }

        public static void Sqsub_V(AilEmitterCtx context)
        {
            EmitVectorSaturatingBinaryOpSx(context, SaturatingFlags.Sub);
        }

        public static void Sqxtn_S(AilEmitterCtx context)
        {
            EmitSaturatingNarrowOp(context, SaturatingNarrowFlags.ScalarSxSx);
        }

        public static void Sqxtn_V(AilEmitterCtx context)
        {
            EmitSaturatingNarrowOp(context, SaturatingNarrowFlags.VectorSxSx);
        }

        public static void Sqxtun_S(AilEmitterCtx context)
        {
            EmitSaturatingNarrowOp(context, SaturatingNarrowFlags.ScalarSxZx);
        }

        public static void Sqxtun_V(AilEmitterCtx context)
        {
            EmitSaturatingNarrowOp(context, SaturatingNarrowFlags.VectorSxZx);
        }

        public static void Srhadd_V(AilEmitterCtx context)
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

        public static void Ssubl_V(AilEmitterCtx context)
        {
            EmitVectorWidenRnRmBinaryOpSx(context, () => context.Emit(OpCodes.Sub));
        }

        public static void Ssubw_V(AilEmitterCtx context)
        {
            EmitVectorWidenRmBinaryOpSx(context, () => context.Emit(OpCodes.Sub));
        }

        public static void Sub_S(AilEmitterCtx context)
        {
            EmitScalarBinaryOpZx(context, () => context.Emit(OpCodes.Sub));
        }

        public static void Sub_V(AilEmitterCtx context)
        {
            if (AOptimizations.UseSse2)
            {
                EmitSse2Op(context, nameof(Sse2.Subtract));
            }
            else
            {
                EmitVectorBinaryOpZx(context, () => context.Emit(OpCodes.Sub));
            }
        }

        public static void Subhn_V(AilEmitterCtx context)
        {
            EmitHighNarrow(context, () => context.Emit(OpCodes.Sub), round: false);
        }

        public static void Suqadd_S(AilEmitterCtx context)
        {
            EmitScalarSaturatingBinaryOpSx(context, SaturatingFlags.Accumulate);
        }

        public static void Suqadd_V(AilEmitterCtx context)
        {
            EmitVectorSaturatingBinaryOpSx(context, SaturatingFlags.Accumulate);
        }

        public static void Uaba_V(AilEmitterCtx context)
        {
            EmitVectorTernaryOpZx(context, () =>
            {
                context.Emit(OpCodes.Sub);
                EmitAbs(context);

                context.Emit(OpCodes.Add);
            });
        }

        public static void Uabal_V(AilEmitterCtx context)
        {
            EmitVectorWidenRnRmTernaryOpZx(context, () =>
            {
                context.Emit(OpCodes.Sub);
                EmitAbs(context);

                context.Emit(OpCodes.Add);
            });
        }

        public static void Uabd_V(AilEmitterCtx context)
        {
            EmitVectorBinaryOpZx(context, () =>
            {
                context.Emit(OpCodes.Sub);
                EmitAbs(context);
            });
        }

        public static void Uabdl_V(AilEmitterCtx context)
        {
            EmitVectorWidenRnRmBinaryOpZx(context, () =>
            {
                context.Emit(OpCodes.Sub);
                EmitAbs(context);
            });
        }

        public static void Uadalp_V(AilEmitterCtx context)
        {
            EmitAddLongPairwise(context, signed: false, accumulate: true);
        }

        public static void Uaddl_V(AilEmitterCtx context)
        {
            EmitVectorWidenRnRmBinaryOpZx(context, () => context.Emit(OpCodes.Add));
        }

        public static void Uaddlp_V(AilEmitterCtx context)
        {
            EmitAddLongPairwise(context, signed: false, accumulate: false);
        }

        public static void Uaddlv_V(AilEmitterCtx context)
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

        public static void Uaddw_V(AilEmitterCtx context)
        {
            EmitVectorWidenRmBinaryOpZx(context, () => context.Emit(OpCodes.Add));
        }

        public static void Uhadd_V(AilEmitterCtx context)
        {
            EmitVectorBinaryOpZx(context, () =>
            {
                context.Emit(OpCodes.Add);

                context.Emit(OpCodes.Ldc_I4_1);
                context.Emit(OpCodes.Shr_Un);
            });
        }

        public static void Uhsub_V(AilEmitterCtx context)
        {
            EmitVectorBinaryOpZx(context, () =>
            {
                context.Emit(OpCodes.Sub);

                context.Emit(OpCodes.Ldc_I4_1);
                context.Emit(OpCodes.Shr_Un);
            });
        }

        public static void Umax_V(AilEmitterCtx context)
        {
            Type[] types = new Type[] { typeof(ulong), typeof(ulong) };

            MethodInfo mthdInfo = typeof(Math).GetMethod(nameof(Math.Max), types);

            EmitVectorBinaryOpZx(context, () => context.EmitCall(mthdInfo));
        }

        public static void Umaxp_V(AilEmitterCtx context)
        {
            Type[] types = new Type[] { typeof(ulong), typeof(ulong) };

            MethodInfo mthdInfo = typeof(Math).GetMethod(nameof(Math.Max), types);

            EmitVectorPairwiseOpZx(context, () => context.EmitCall(mthdInfo));
        }

        public static void Umin_V(AilEmitterCtx context)
        {
            Type[] types = new Type[] { typeof(ulong), typeof(ulong) };

            MethodInfo mthdInfo = typeof(Math).GetMethod(nameof(Math.Min), types);

            EmitVectorBinaryOpZx(context, () => context.EmitCall(mthdInfo));
        }

        public static void Uminp_V(AilEmitterCtx context)
        {
            Type[] types = new Type[] { typeof(ulong), typeof(ulong) };

            MethodInfo mthdInfo = typeof(Math).GetMethod(nameof(Math.Min), types);

            EmitVectorPairwiseOpZx(context, () => context.EmitCall(mthdInfo));
        }

        public static void Umlal_V(AilEmitterCtx context)
        {
            EmitVectorWidenRnRmTernaryOpZx(context, () =>
            {
                context.Emit(OpCodes.Mul);
                context.Emit(OpCodes.Add);
            });
        }

        public static void Umlsl_V(AilEmitterCtx context)
        {
            EmitVectorWidenRnRmTernaryOpZx(context, () =>
            {
                context.Emit(OpCodes.Mul);
                context.Emit(OpCodes.Sub);
            });
        }

        public static void Umull_V(AilEmitterCtx context)
        {
            EmitVectorWidenRnRmBinaryOpZx(context, () => context.Emit(OpCodes.Mul));
        }

        public static void Uqadd_S(AilEmitterCtx context)
        {
            EmitScalarSaturatingBinaryOpZx(context, SaturatingFlags.Add);
        }

        public static void Uqadd_V(AilEmitterCtx context)
        {
            EmitVectorSaturatingBinaryOpZx(context, SaturatingFlags.Add);
        }

        public static void Uqsub_S(AilEmitterCtx context)
        {
            EmitScalarSaturatingBinaryOpZx(context, SaturatingFlags.Sub);
        }

        public static void Uqsub_V(AilEmitterCtx context)
        {
            EmitVectorSaturatingBinaryOpZx(context, SaturatingFlags.Sub);
        }

        public static void Uqxtn_S(AilEmitterCtx context)
        {
            EmitSaturatingNarrowOp(context, SaturatingNarrowFlags.ScalarZxZx);
        }

        public static void Uqxtn_V(AilEmitterCtx context)
        {
            EmitSaturatingNarrowOp(context, SaturatingNarrowFlags.VectorZxZx);
        }

        public static void Urhadd_V(AilEmitterCtx context)
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

        public static void Usqadd_S(AilEmitterCtx context)
        {
            EmitScalarSaturatingBinaryOpZx(context, SaturatingFlags.Accumulate);
        }

        public static void Usqadd_V(AilEmitterCtx context)
        {
            EmitVectorSaturatingBinaryOpZx(context, SaturatingFlags.Accumulate);
        }

        public static void Usubl_V(AilEmitterCtx context)
        {
            EmitVectorWidenRnRmBinaryOpZx(context, () => context.Emit(OpCodes.Sub));
        }

        public static void Usubw_V(AilEmitterCtx context)
        {
            EmitVectorWidenRmBinaryOpZx(context, () => context.Emit(OpCodes.Sub));
        }

        private static void EmitAbs(AilEmitterCtx context)
        {
            AilLabel lblTrue = new AilLabel();

            context.Emit(OpCodes.Dup);
            context.Emit(OpCodes.Ldc_I4_0);
            context.Emit(OpCodes.Bge_S, lblTrue);

            context.Emit(OpCodes.Neg);

            context.MarkLabel(lblTrue);
        }

        private static void EmitAddLongPairwise(AilEmitterCtx context, bool signed, bool accumulate)
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

            if (op.RegisterSize == ARegisterSize.Simd64)
            {
                EmitVectorZeroUpper(context, op.Rd);
            }
        }

        private static void EmitDoublingMultiplyHighHalf(AilEmitterCtx context, bool round)
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

                AilLabel lblTrue = new AilLabel();

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

        private static void EmitHighNarrow(AilEmitterCtx context, Action emit, bool round)
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

            if (part == 0)
            {
                EmitVectorZeroUpper(context, op.Rd);
            }
        }
    }
}
