// https://github.com/intel/ARM_NEON_2_x86_SSE/blob/master/NEON_2_SSE.h
// https://www.agner.org/optimize/#vectorclass @ vectori128.h

using ARMeilleure.Decoders;
using ARMeilleure.IntermediateRepresentation;
using ARMeilleure.State;
using ARMeilleure.Translation;
using System;
using System.Reflection;

using static ARMeilleure.Instructions.InstEmitHelper;
using static ARMeilleure.Instructions.InstEmitSimdHelper;
using static ARMeilleure.IntermediateRepresentation.OperandHelper;

namespace ARMeilleure.Instructions
{
    using Func2I = Func<Operand, Operand, Operand>;


    static partial class InstEmit
    {
        public static void Abs_S(EmitterContext context)
        {
            EmitScalarUnaryOpSx(context, (op1) => EmitAbs(context, op1));
        }

        public static void Abs_V(EmitterContext context)
        {
            EmitVectorUnaryOpSx(context, (op1) => EmitAbs(context, op1));
        }

        public static void Add_S(EmitterContext context)
        {
            EmitScalarBinaryOpZx(context, (op1, op2) => context.Add(op1, op2));
        }

        public static void Add_V(EmitterContext context)
        {
            if (Optimizations.UseSse2)
            {
                OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

                Operand n = GetVec(op.Rn);
                Operand m = GetVec(op.Rm);

                Instruction addInst = X86PaddInstruction[op.Size];

                Operand res = context.AddIntrinsic(addInst, n, m);

                if (op.RegisterSize == RegisterSize.Simd64)
                {
                    res = context.VectorZeroUpper64(res);
                }

                context.Copy(GetVec(op.Rd), res);
            }
            else
            {
                EmitVectorBinaryOpZx(context, (op1, op2) => context.Add(op1, op2));
            }
        }

        public static void Addhn_V(EmitterContext context)
        {
            EmitHighNarrow(context, (op1, op2) => context.Add(op1, op2), round: false);
        }

        public static void Addp_S(EmitterContext context)
        {
            OpCodeSimd op = (OpCodeSimd)context.CurrOp;

            Operand ne0 = EmitVectorExtractZx(context, op.Rn, 0, op.Size);
            Operand ne1 = EmitVectorExtractZx(context, op.Rn, 1, op.Size);

            Operand res = context.Add(ne0, ne1);

            context.Copy(GetVec(op.Rd), EmitVectorInsert(context, context.VectorZero(), res, 0, op.Size));
        }

        public static void Addp_V(EmitterContext context)
        {
            EmitVectorPairwiseOpZx(context, (op1, op2) => context.Add(op1, op2));
        }

        public static void Addv_V(EmitterContext context)
        {
            EmitVectorAcrossVectorOpZx(context, (op1, op2) => context.Add(op1, op2));
        }

        public static void Cls_V(EmitterContext context)
        {
            OpCodeSimd op = (OpCodeSimd)context.CurrOp;

            Operand res = context.VectorZero();

            int elems = op.GetBytesCount() >> op.Size;

            int eSize = 8 << op.Size;

            for (int index = 0; index < elems; index++)
            {
                Operand ne = EmitVectorExtractZx(context, op.Rn, index, op.Size);

                MethodInfo info = typeof(SoftFallback).GetMethod(nameof(SoftFallback.CountLeadingSigns));

                Operand de = context.Call(info, ne, Const(eSize));

                res = EmitVectorInsert(context, res, de, index, op.Size);
            }

            context.Copy(GetVec(op.Rd), res);
        }

        public static void Clz_V(EmitterContext context)
        {
            OpCodeSimd op = (OpCodeSimd)context.CurrOp;

            Operand res = context.VectorZero();

            int elems = op.GetBytesCount() >> op.Size;

            int eSize = 8 << op.Size;

            for (int index = 0; index < elems; index++)
            {
                Operand ne = EmitVectorExtractZx(context, op.Rn, index, op.Size);

                Operand de;

                if (eSize == 64)
                {
                    de = context.CountLeadingZeros(ne);
                }
                else
                {
                    MethodInfo info = typeof(SoftFallback).GetMethod(nameof(SoftFallback.CountLeadingZeros));

                    de = context.Call(info, ne, Const(eSize));
                }

                res = EmitVectorInsert(context, res, de, index, op.Size);
            }

            context.Copy(GetVec(op.Rd), res);
        }

        public static void Cnt_V(EmitterContext context)
        {
            OpCodeSimd op = (OpCodeSimd)context.CurrOp;

            Operand res = context.VectorZero();

            int elems = op.RegisterSize == RegisterSize.Simd128 ? 16 : 8;

            for (int index = 0; index < elems; index++)
            {
                Operand ne = EmitVectorExtractZx(context, op.Rn, index, 0);

                Operand de;

                if (Optimizations.UsePopCnt)
                {
                    de = context.AddIntrinsic(Instruction.X86Popcnt, ne);
                }
                else
                {
                    MethodInfo info = typeof(SoftFallback).GetMethod(nameof(SoftFallback.CountSetBits8));

                    de = context.Call(info, ne);
                }

                res = EmitVectorInsert(context, res, de, index, 0);
            }

            context.Copy(GetVec(op.Rd), res);
        }

        public static void Fabd_S(EmitterContext context)
        {
            if (Optimizations.FastFP && Optimizations.UseSse2)
            {
                OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

                int sizeF = op.Size & 1;

                if (sizeF == 0)
                {
                    Operand res = context.AddIntrinsic(Instruction.X86Subss, GetVec(op.Rn), GetVec(op.Rm));

                    Operand mask = X86GetScalar(context, -0f);

                    res = context.AddIntrinsic(Instruction.X86Andnps, mask, res);

                    context.Copy(GetVec(op.Rd), context.VectorZeroUpper96(res));
                }
                else /* if (sizeF == 1) */
                {
                    Operand res = context.AddIntrinsic(Instruction.X86Subsd, GetVec(op.Rn), GetVec(op.Rm));

                    Operand mask = X86GetScalar(context, -0d);

                    res = context.AddIntrinsic(Instruction.X86Andnpd, mask, res);

                    context.Copy(GetVec(op.Rd), context.VectorZeroUpper64(res));
                }
            }
            else
            {
                EmitScalarBinaryOpF(context, (op1, op2) =>
                {
                    Operand res = EmitSoftFloatCall(context, nameof(SoftFloat32.FPSub), op1, op2);

                    return EmitUnaryMathCall(context, nameof(Math.Abs), res);
                });
            }
        }

        public static void Fabd_V(EmitterContext context)
        {
            if (Optimizations.FastFP && Optimizations.UseSse2)
            {
                OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

                int sizeF = op.Size & 1;

                if (sizeF == 0)
                {
                    Operand res = context.AddIntrinsic(Instruction.X86Subps, GetVec(op.Rn), GetVec(op.Rm));

                    Operand mask = X86GetAllElements(context, -0f);

                    res = context.AddIntrinsic(Instruction.X86Andnps, mask, res);

                    if (op.RegisterSize == RegisterSize.Simd64)
                    {
                        res = context.VectorZeroUpper64(res);
                    }

                    context.Copy(GetVec(op.Rd), res);
                }
                else /* if (sizeF == 1) */
                {
                    Operand res = context.AddIntrinsic(Instruction.X86Subpd, GetVec(op.Rn), GetVec(op.Rm));

                    Operand mask = X86GetAllElements(context, -0d);

                    res = context.AddIntrinsic(Instruction.X86Andnpd, mask, res);

                    context.Copy(GetVec(op.Rd), res);
                }
            }
            else
            {
                EmitVectorBinaryOpF(context, (op1, op2) =>
                {
                    Operand res = EmitSoftFloatCall(context, nameof(SoftFloat32.FPSub), op1, op2);

                    return EmitUnaryMathCall(context, nameof(Math.Abs), res);
                });
            }
        }

        public static void Fabs_S(EmitterContext context)
        {
            if (Optimizations.UseSse2)
            {
                OpCodeSimd op = (OpCodeSimd)context.CurrOp;

                if (op.Size == 0)
                {
                    Operand mask = X86GetScalar(context, -0f);

                    Operand res = context.AddIntrinsic(Instruction.X86Andnps, mask, GetVec(op.Rn));

                    context.Copy(GetVec(op.Rd), context.VectorZeroUpper96(res));
                }
                else /* if (op.Size == 1) */
                {
                    Operand mask = X86GetScalar(context, -0d);

                    Operand res = context.AddIntrinsic(Instruction.X86Andnpd, mask, GetVec(op.Rn));

                    context.Copy(GetVec(op.Rd), context.VectorZeroUpper64(res));
                }
            }
            else
            {
                EmitScalarUnaryOpF(context, (op1) =>
                {
                    return EmitUnaryMathCall(context, nameof(Math.Abs), op1);
                });
            }
        }

        public static void Fabs_V(EmitterContext context)
        {
            if (Optimizations.UseSse2)
            {
                OpCodeSimd op = (OpCodeSimd)context.CurrOp;

                int sizeF = op.Size & 1;

                 if (sizeF == 0)
                {
                    Operand mask = X86GetAllElements(context, -0f);

                    Operand res = context.AddIntrinsic(Instruction.X86Andnps, mask, GetVec(op.Rn));

                    if (op.RegisterSize == RegisterSize.Simd64)
                    {
                        res = context.VectorZeroUpper64(res);
                    }

                    context.Copy(GetVec(op.Rd), res);
                }
                else /* if (sizeF == 1) */
                {
                    Operand mask = X86GetAllElements(context, -0d);

                    Operand res = context.AddIntrinsic(Instruction.X86Andnpd, mask, GetVec(op.Rn));

                    context.Copy(GetVec(op.Rd), res);
                }
            }
            else
            {
                EmitVectorUnaryOpF(context, (op1) =>
                {
                    return EmitUnaryMathCall(context, nameof(Math.Abs), op1);
                });
            }
        }

        public static void Fadd_S(EmitterContext context)
        {
            if (Optimizations.FastFP && Optimizations.UseSse2)
            {
                EmitScalarBinaryOpF(context, Instruction.X86Addss, Instruction.X86Addsd);
            }
            else
            {
                EmitScalarBinaryOpF(context, (op1, op2) =>
                {
                    return EmitSoftFloatCall(context, nameof(SoftFloat32.FPAdd), op1, op2);
                });
            }
        }

        public static void Fadd_V(EmitterContext context)
        {
            if (Optimizations.FastFP && Optimizations.UseSse2)
            {
                EmitVectorBinaryOpF(context, Instruction.X86Addps, Instruction.X86Addpd);
            }
            else
            {
                EmitVectorBinaryOpF(context, (op1, op2) =>
                {
                    return EmitSoftFloatCall(context, nameof(SoftFloat32.FPAdd), op1, op2);
                });
            }
        }

        public static void Faddp_S(EmitterContext context)
        {
            OpCodeSimd op = (OpCodeSimd)context.CurrOp;

            int sizeF = op.Size & 1;

            if (Optimizations.FastFP && Optimizations.UseSse3)
            {
                EmitScalarBinaryOpF(context, Instruction.X86Haddps, Instruction.X86Haddpd);
            }
            else
            {
                OperandType type = sizeF != 0 ? OperandType.FP64
                                              : OperandType.FP32;

                Operand ne0 = context.VectorExtract(GetVec(op.Rn), Local(type), 0);
                Operand ne1 = context.VectorExtract(GetVec(op.Rn), Local(type), 1);

                Operand res = EmitSoftFloatCall(context, nameof(SoftFloat32.FPAdd), ne0, ne1);

                context.Copy(GetVec(op.Rd), context.VectorInsert(context.VectorZero(), res, 0));
            }
        }

        public static void Faddp_V(EmitterContext context)
        {
            if (Optimizations.FastFP && Optimizations.UseSse2)
            {
                EmitVectorPairwiseOpF(context, Instruction.X86Addps, Instruction.X86Addpd);
            }
            else
            {
                EmitVectorPairwiseOpF(context, (op1, op2) =>
                {
                    return EmitSoftFloatCall(context, nameof(SoftFloat32.FPAdd), op1, op2);
                });
            }
        }

        public static void Fdiv_S(EmitterContext context)
        {
            if (Optimizations.FastFP && Optimizations.UseSse2)
            {
                EmitScalarBinaryOpF(context, Instruction.X86Divss, Instruction.X86Divsd);
            }
            else
            {
                EmitScalarBinaryOpF(context, (op1, op2) =>
                {
                    return EmitSoftFloatCall(context, nameof(SoftFloat32.FPDiv), op1, op2);
                });
            }
        }

        public static void Fdiv_V(EmitterContext context)
        {
            if (Optimizations.FastFP && Optimizations.UseSse2)
            {
                EmitVectorBinaryOpF(context, Instruction.X86Divps, Instruction.X86Divpd);
            }
            else
            {
                EmitVectorBinaryOpF(context, (op1, op2) =>
                {
                    return EmitSoftFloatCall(context, nameof(SoftFloat32.FPDiv), op1, op2);
                });
            }
        }

        public static void Fmadd_S(EmitterContext context) // Fused.
        {
            if (Optimizations.FastFP && Optimizations.UseSse2)
            {
                OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

                Operand a = GetVec(op.Ra);
                Operand n = GetVec(op.Rn);
                Operand m = GetVec(op.Rm);

                if (op.Size == 0)
                {
                    Operand res = context.AddIntrinsic(Instruction.X86Mulss, n, m);

                    res = context.AddIntrinsic(Instruction.X86Addss, a, res);

                    context.Copy(GetVec(op.Rd), context.VectorZeroUpper96(res));
                }
                else /* if (op.Size == 1) */
                {
                    Operand res = context.AddIntrinsic(Instruction.X86Mulsd, n, m);

                    res = context.AddIntrinsic(Instruction.X86Addsd, a, res);

                    context.Copy(GetVec(op.Rd), context.VectorZeroUpper64(res));
                }
            }
            else
            {
                EmitScalarTernaryRaOpF(context, (op1, op2, op3) =>
                {
                    return EmitSoftFloatCall(context, nameof(SoftFloat32.FPMulAdd), op1, op2, op3);
                });
            }
        }

        public static void Fmax_S(EmitterContext context)
        {
            if (Optimizations.FastFP && Optimizations.UseSse2)
            {
                EmitScalarBinaryOpF(context, Instruction.X86Maxss, Instruction.X86Maxsd);
            }
            else
            {
                EmitScalarBinaryOpF(context, (op1, op2) =>
                {
                    return EmitSoftFloatCall(context, nameof(SoftFloat32.FPMax), op1, op2);
                });
            }
        }

        public static void Fmax_V(EmitterContext context)
        {
            if (Optimizations.FastFP && Optimizations.UseSse2)
            {
                EmitVectorBinaryOpF(context, Instruction.X86Maxps, Instruction.X86Maxpd);
            }
            else
            {
                EmitVectorBinaryOpF(context, (op1, op2) =>
                {
                    return EmitSoftFloatCall(context, nameof(SoftFloat32.FPMax), op1, op2);
                });
            }
        }

        public static void Fmaxnm_S(EmitterContext context)
        {
            EmitScalarBinaryOpF(context, (op1, op2) =>
            {
                return EmitSoftFloatCall(context, nameof(SoftFloat32.FPMaxNum), op1, op2);
            });
        }

        public static void Fmaxnm_V(EmitterContext context)
        {
            EmitVectorBinaryOpF(context, (op1, op2) =>
            {
                return EmitSoftFloatCall(context, nameof(SoftFloat32.FPMaxNum), op1, op2);
            });
        }

        public static void Fmaxp_V(EmitterContext context)
        {
            if (Optimizations.FastFP && Optimizations.UseSse2)
            {
                EmitVectorPairwiseOpF(context, Instruction.X86Maxps, Instruction.X86Maxpd);
            }
            else
            {
                EmitVectorPairwiseOpF(context, (op1, op2) =>
                {
                    return EmitSoftFloatCall(context, nameof(SoftFloat32.FPMax), op1, op2);
                });
            }
        }

        public static void Fmin_S(EmitterContext context)
        {
            if (Optimizations.FastFP && Optimizations.UseSse2)
            {
                EmitScalarBinaryOpF(context, Instruction.X86Minss, Instruction.X86Minsd);
            }
            else
            {
                EmitScalarBinaryOpF(context, (op1, op2) =>
                {
                    return EmitSoftFloatCall(context, nameof(SoftFloat32.FPMin), op1, op2);
                });
            }
        }

        public static void Fmin_V(EmitterContext context)
        {
            if (Optimizations.FastFP && Optimizations.UseSse2)
            {
                EmitVectorBinaryOpF(context, Instruction.X86Minps, Instruction.X86Minpd);
            }
            else
            {
                EmitVectorBinaryOpF(context, (op1, op2) =>
                {
                    return EmitSoftFloatCall(context, nameof(SoftFloat32.FPMin), op1, op2);
                });
            }
        }

        public static void Fminnm_S(EmitterContext context)
        {
            EmitScalarBinaryOpF(context, (op1, op2) =>
            {
                return EmitSoftFloatCall(context, nameof(SoftFloat32.FPMinNum), op1, op2);
            });
        }

        public static void Fminnm_V(EmitterContext context)
        {
            EmitVectorBinaryOpF(context, (op1, op2) =>
            {
                return EmitSoftFloatCall(context, nameof(SoftFloat32.FPMinNum), op1, op2);
            });
        }

        public static void Fminp_V(EmitterContext context)
        {
            if (Optimizations.FastFP && Optimizations.UseSse2)
            {
                EmitVectorPairwiseOpF(context, Instruction.X86Minps, Instruction.X86Minpd);
            }
            else
            {
                EmitVectorPairwiseOpF(context, (op1, op2) =>
                {
                    return EmitSoftFloatCall(context, nameof(SoftFloat32.FPMin), op1, op2);
                });
            }
        }

        public static void Fmla_Se(EmitterContext context) // Fused.
        {
            EmitScalarTernaryOpByElemF(context, (op1, op2, op3) =>
            {
                return context.Add(op1, context.Multiply(op2, op3));
            });
        }

        public static void Fmla_V(EmitterContext context) // Fused.
        {
            if (Optimizations.FastFP && Optimizations.UseSse2)
            {
                OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

                Operand d = GetVec(op.Rd);
                Operand n = GetVec(op.Rn);
                Operand m = GetVec(op.Rm);

                int sizeF = op.Size & 1;

                if (sizeF == 0)
                {
                    Operand res = context.AddIntrinsic(Instruction.X86Mulps, n, m);

                    res = context.AddIntrinsic(Instruction.X86Addps, d, res);

                    if (op.RegisterSize == RegisterSize.Simd64)
                    {
                        res = context.VectorZeroUpper64(res);
                    }

                    context.Copy(GetVec(op.Rd), res);
                }
                else /* if (sizeF == 1) */
                {
                    Operand res = context.AddIntrinsic(Instruction.X86Mulpd, n, m);

                    res = context.AddIntrinsic(Instruction.X86Addpd, d, res);

                    context.Copy(GetVec(op.Rd), res);
                }
            }
            else
            {
                EmitVectorTernaryOpF(context, (op1, op2, op3) =>
                {
                    return EmitSoftFloatCall(context, nameof(SoftFloat32.FPMulAdd), op1, op2, op3);
                });
            }
        }

        public static void Fmla_Ve(EmitterContext context) // Fused.
        {
            if (Optimizations.FastFP && Optimizations.UseSse2)
            {
                OpCodeSimdRegElemF op = (OpCodeSimdRegElemF)context.CurrOp;

                Operand d = GetVec(op.Rd);
                Operand n = GetVec(op.Rn);
                Operand m = GetVec(op.Rm);

                int sizeF = op.Size & 1;

                if (sizeF == 0)
                {
                    int shuffleMask = op.Index | op.Index << 2 | op.Index << 4 | op.Index << 6;

                    Operand res = context.AddIntrinsic(Instruction.X86Shufps, m, m, Const(shuffleMask));

                    res = context.AddIntrinsic(Instruction.X86Mulps, n, res);
                    res = context.AddIntrinsic(Instruction.X86Addps, d, res);

                    if (op.RegisterSize == RegisterSize.Simd64)
                    {
                        res = context.VectorZeroUpper64(res);
                    }

                    context.Copy(GetVec(op.Rd), res);
                }
                else /* if (sizeF == 1) */
                {
                    int shuffleMask = op.Index | op.Index << 1;

                    Operand res = context.AddIntrinsic(Instruction.X86Shufpd, m, m, Const(shuffleMask));

                    res = context.AddIntrinsic(Instruction.X86Mulpd, n, res);
                    res = context.AddIntrinsic(Instruction.X86Addpd, d, res);

                    context.Copy(GetVec(op.Rd), res);
                }
            }
            else
            {
                EmitVectorTernaryOpByElemF(context, (op1, op2, op3) =>
                {
                    return EmitSoftFloatCall(context, nameof(SoftFloat32.FPMulAdd), op1, op2, op3);
                });
            }
        }

        public static void Fmls_Se(EmitterContext context) // Fused.
        {
            EmitScalarTernaryOpByElemF(context, (op1, op2, op3) =>
            {
                return context.Subtract(op1, context.Multiply(op2, op3));
            });
        }

        public static void Fmls_V(EmitterContext context) // Fused.
        {
            if (Optimizations.FastFP && Optimizations.UseSse2)
            {
                OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

                Operand d = GetVec(op.Rd);
                Operand n = GetVec(op.Rn);
                Operand m = GetVec(op.Rm);

                int sizeF = op.Size & 1;

                if (sizeF == 0)
                {
                    Operand res = context.AddIntrinsic(Instruction.X86Mulps, n, m);

                    res = context.AddIntrinsic(Instruction.X86Subps, d, res);

                    if (op.RegisterSize == RegisterSize.Simd64)
                    {
                        res = context.VectorZeroUpper64(res);
                    }

                    context.Copy(GetVec(op.Rd), res);
                }
                else /* if (sizeF == 1) */
                {
                    Operand res = context.AddIntrinsic(Instruction.X86Mulpd, n, m);

                    res = context.AddIntrinsic(Instruction.X86Subpd, d, res);

                    context.Copy(GetVec(op.Rd), res);
                }
            }
            else
            {
                EmitVectorTernaryOpF(context, (op1, op2, op3) =>
                {
                    return EmitSoftFloatCall(context, nameof(SoftFloat32.FPMulSub), op1, op2, op3);
                });
            }
        }

        public static void Fmls_Ve(EmitterContext context) // Fused.
        {
            if (Optimizations.FastFP && Optimizations.UseSse2)
            {
                OpCodeSimdRegElemF op = (OpCodeSimdRegElemF)context.CurrOp;

                Operand d = GetVec(op.Rd);
                Operand n = GetVec(op.Rn);
                Operand m = GetVec(op.Rm);

                int sizeF = op.Size & 1;

                if (sizeF == 0)
                {
                    int shuffleMask = op.Index | op.Index << 2 | op.Index << 4 | op.Index << 6;

                    Operand res = context.AddIntrinsic(Instruction.X86Shufps, m, m, Const(shuffleMask));

                    res = context.AddIntrinsic(Instruction.X86Mulps, n, res);
                    res = context.AddIntrinsic(Instruction.X86Subps, d, res);

                    if (op.RegisterSize == RegisterSize.Simd64)
                    {
                        res = context.VectorZeroUpper64(res);
                    }

                    context.Copy(GetVec(op.Rd), res);
                }
                else /* if (sizeF == 1) */
                {
                    int shuffleMask = op.Index | op.Index << 1;

                    Operand res = context.AddIntrinsic(Instruction.X86Shufpd, m, m, Const(shuffleMask));

                    res = context.AddIntrinsic(Instruction.X86Mulpd, n, res);
                    res = context.AddIntrinsic(Instruction.X86Subpd, d, res);

                    context.Copy(GetVec(op.Rd), res);
                }
            }
            else
            {
                EmitVectorTernaryOpByElemF(context, (op1, op2, op3) =>
                {
                    return EmitSoftFloatCall(context, nameof(SoftFloat32.FPMulSub), op1, op2, op3);
                });
            }
        }

        public static void Fmsub_S(EmitterContext context) // Fused.
        {
            if (Optimizations.FastFP && Optimizations.UseSse2)
            {
                OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

                Operand a = GetVec(op.Ra);
                Operand n = GetVec(op.Rn);
                Operand m = GetVec(op.Rm);

                if (op.Size == 0)
                {
                    Operand res = context.AddIntrinsic(Instruction.X86Mulss, n, m);

                    res = context.AddIntrinsic(Instruction.X86Subss, a, res);

                    context.Copy(GetVec(op.Rd), context.VectorZeroUpper96(res));
                }
                else /* if (op.Size == 1) */
                {
                    Operand res = context.AddIntrinsic(Instruction.X86Mulsd, n, m);

                    res = context.AddIntrinsic(Instruction.X86Subsd, a, res);

                    context.Copy(GetVec(op.Rd), context.VectorZeroUpper64(res));
                }
            }
            else
            {
                EmitScalarTernaryRaOpF(context, (op1, op2, op3) =>
                {
                    return EmitSoftFloatCall(context, nameof(SoftFloat32.FPMulSub), op1, op2, op3);
                });
            }
        }

        public static void Fmul_S(EmitterContext context)
        {
            if (Optimizations.FastFP && Optimizations.UseSse2)
            {
                EmitScalarBinaryOpF(context, Instruction.X86Mulss, Instruction.X86Mulsd);
            }
            else
            {
                EmitScalarBinaryOpF(context, (op1, op2) =>
                {
                    return EmitSoftFloatCall(context, nameof(SoftFloat32.FPMul), op1, op2);
                });
            }
        }

        public static void Fmul_Se(EmitterContext context)
        {
            EmitScalarBinaryOpByElemF(context, (op1, op2) => context.Multiply(op1, op2));
        }

        public static void Fmul_V(EmitterContext context)
        {
            if (Optimizations.FastFP && Optimizations.UseSse2)
            {
                EmitVectorBinaryOpF(context, Instruction.X86Mulps, Instruction.X86Mulpd);
            }
            else
            {
                EmitVectorBinaryOpF(context, (op1, op2) =>
                {
                    return EmitSoftFloatCall(context, nameof(SoftFloat32.FPMul), op1, op2);
                });
            }
        }

        public static void Fmul_Ve(EmitterContext context)
        {
            if (Optimizations.FastFP && Optimizations.UseSse2)
            {
                OpCodeSimdRegElemF op = (OpCodeSimdRegElemF)context.CurrOp;

                Operand n = GetVec(op.Rn);
                Operand m = GetVec(op.Rm);

                int sizeF = op.Size & 1;

                if (sizeF == 0)
                {
                    int shuffleMask = op.Index | op.Index << 2 | op.Index << 4 | op.Index << 6;

                    Operand res = context.AddIntrinsic(Instruction.X86Shufps, m, m, Const(shuffleMask));

                    res = context.AddIntrinsic(Instruction.X86Mulps, n, res);

                    if (op.RegisterSize == RegisterSize.Simd64)
                    {
                        res = context.VectorZeroUpper64(res);
                    }

                    context.Copy(GetVec(op.Rd), res);
                }
                else /* if (sizeF == 1) */
                {
                    int shuffleMask = op.Index | op.Index << 1;

                    Operand res = context.AddIntrinsic(Instruction.X86Shufpd, m, m, Const(shuffleMask));

                    res = context.AddIntrinsic(Instruction.X86Mulpd, n, res);

                    context.Copy(GetVec(op.Rd), res);
                }
            }
            else
            {
                EmitVectorBinaryOpByElemF(context, (op1, op2) =>
                {
                    return EmitSoftFloatCall(context, nameof(SoftFloat32.FPMul), op1, op2);
                });
            }
        }

        public static void Fmulx_S(EmitterContext context)
        {
            EmitScalarBinaryOpF(context, (op1, op2) =>
            {
                return EmitSoftFloatCall(context, nameof(SoftFloat32.FPMulX), op1, op2);
            });
        }

        public static void Fmulx_Se(EmitterContext context)
        {
            EmitScalarBinaryOpByElemF(context, (op1, op2) =>
            {
                return EmitSoftFloatCall(context, nameof(SoftFloat32.FPMulX), op1, op2);
            });
        }

        public static void Fmulx_V(EmitterContext context)
        {
            EmitVectorBinaryOpF(context, (op1, op2) =>
            {
                return EmitSoftFloatCall(context, nameof(SoftFloat32.FPMulX), op1, op2);
            });
        }

        public static void Fmulx_Ve(EmitterContext context)
        {
            EmitVectorBinaryOpByElemF(context, (op1, op2) =>
            {
                return EmitSoftFloatCall(context, nameof(SoftFloat32.FPMulX), op1, op2);
            });
        }

        public static void Fneg_S(EmitterContext context)
        {
            if (Optimizations.UseSse2)
            {
                OpCodeSimd op = (OpCodeSimd)context.CurrOp;

                if (op.Size == 0)
                {
                    Operand mask = X86GetScalar(context, -0f);

                    Operand res = context.AddIntrinsic(Instruction.X86Xorps, mask, GetVec(op.Rn));

                    context.Copy(GetVec(op.Rd), context.VectorZeroUpper96(res));
                }
                else /* if (op.Size == 1) */
                {
                    Operand mask = X86GetScalar(context, -0d);

                    Operand res = context.AddIntrinsic(Instruction.X86Xorpd, mask, GetVec(op.Rn));

                    context.Copy(GetVec(op.Rd), context.VectorZeroUpper64(res));
                }
            }
            else
            {
                EmitScalarUnaryOpF(context, (op1) => context.Negate(op1));
            }
        }

        public static void Fneg_V(EmitterContext context)
        {
            if (Optimizations.UseSse2)
            {
                OpCodeSimd op = (OpCodeSimd)context.CurrOp;

                int sizeF = op.Size & 1;

                if (sizeF == 0)
                {
                    Operand mask = X86GetAllElements(context, -0f);

                    Operand res = context.AddIntrinsic(Instruction.X86Xorps, mask, GetVec(op.Rn));

                    if (op.RegisterSize == RegisterSize.Simd64)
                    {
                        res = context.VectorZeroUpper64(res);
                    }

                    context.Copy(GetVec(op.Rd), res);
                }
                else /* if (sizeF == 1) */
                {
                    Operand mask = X86GetAllElements(context, -0d);

                    Operand res = context.AddIntrinsic(Instruction.X86Xorpd, mask, GetVec(op.Rn));

                    context.Copy(GetVec(op.Rd), res);
                }
            }
            else
            {
                EmitVectorUnaryOpF(context, (op1) => context.Negate(op1));
            }
        }

        public static void Fnmadd_S(EmitterContext context)
        {
            OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

            int sizeF = op.Size & 1;

            OperandType type = sizeF != 0 ? OperandType.FP64
                                          : OperandType.FP32;

            Operand ne = context.VectorExtract(GetVec(op.Rn), Local(type), 0);
            Operand me = context.VectorExtract(GetVec(op.Rm), Local(type), 0);
            Operand ae = context.VectorExtract(GetVec(op.Ra), Local(type), 0);

            Operand res = context.Subtract(context.Multiply(context.Negate(ne), me), ae);

            context.Copy(GetVec(op.Rd), context.VectorInsert(context.VectorZero(), res, 0));
        }

        public static void Fnmsub_S(EmitterContext context)
        {
            OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

            int sizeF = op.Size & 1;

            OperandType type = sizeF != 0 ? OperandType.FP64
                                          : OperandType.FP32;

            Operand ne = context.VectorExtract(GetVec(op.Rn), Local(type), 0);
            Operand me = context.VectorExtract(GetVec(op.Rm), Local(type), 0);
            Operand ae = context.VectorExtract(GetVec(op.Ra), Local(type), 0);

            Operand res = context.Subtract(context.Multiply(ne, me), ae);

            context.Copy(GetVec(op.Rd), context.VectorInsert(context.VectorZero(), res, 0));
        }

        public static void Fnmul_S(EmitterContext context)
        {
            EmitScalarBinaryOpF(context, (op1, op2) => context.Negate(context.Multiply(op1, op2)));
        }

        public static void Frecpe_S(EmitterContext context)
        {
            OpCodeSimd op = (OpCodeSimd)context.CurrOp;

            int sizeF = op.Size & 1;

            if (Optimizations.FastFP && Optimizations.UseSse && sizeF == 0)
            {
                Operand n = GetVec(op.Rn);

                context.Copy(GetVec(op.Rd), context.AddIntrinsic(Instruction.X86Rcpss, n));
            }
            else
            {
                EmitScalarUnaryOpF(context, (op1) =>
                {
                    return EmitSoftFloatCall(context, nameof(SoftFloat32.FPRecipEstimate), op1);
                });
            }
        }

        public static void Frecpe_V(EmitterContext context)
        {
            OpCodeSimd op = (OpCodeSimd)context.CurrOp;

            int sizeF = op.Size & 1;

            if (Optimizations.FastFP && Optimizations.UseSse && sizeF == 0)
            {
                Operand n = GetVec(op.Rn);

                context.Copy(GetVec(op.Rd), context.AddIntrinsic(Instruction.X86Rcpps, n));
            }
            else
            {
                EmitVectorUnaryOpF(context, (op1) =>
                {
                    return EmitSoftFloatCall(context, nameof(SoftFloat32.FPRecipEstimate), op1);
                });
            }
        }

        public static void Frecps_S(EmitterContext context) // Fused.
        {
            if (Optimizations.FastFP && Optimizations.UseSse2)
            {
                OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

                int sizeF = op.Size & 1;

                if (sizeF == 0)
                {
                    Operand mask = X86GetScalar(context, 2f);

                    Operand res = context.AddIntrinsic(Instruction.X86Mulss, GetVec(op.Rn), GetVec(op.Rm));

                    res = context.AddIntrinsic(Instruction.X86Subss, mask, res);

                    context.Copy(GetVec(op.Rd), context.VectorZeroUpper96(res));
                }
                else /* if (sizeF == 1) */
                {
                    Operand mask = X86GetScalar(context, 2d);

                    Operand res = context.AddIntrinsic(Instruction.X86Mulsd, GetVec(op.Rn), GetVec(op.Rm));

                    res = context.AddIntrinsic(Instruction.X86Subsd, mask, res);

                    context.Copy(GetVec(op.Rd), context.VectorZeroUpper64(res));
                }
            }
            else
            {
                EmitScalarBinaryOpF(context, (op1, op2) =>
                {
                    return EmitSoftFloatCall(context, nameof(SoftFloat32.FPRecipStepFused), op1, op2);
                });
            }
        }

        public static void Frecps_V(EmitterContext context) // Fused.
        {
            if (Optimizations.FastFP && Optimizations.UseSse2)
            {
                OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

                int sizeF = op.Size & 1;

                if (sizeF == 0)
                {
                    Operand mask = X86GetAllElements(context, 2f);

                    Operand res = context.AddIntrinsic(Instruction.X86Mulps, GetVec(op.Rn), GetVec(op.Rm));

                    res = context.AddIntrinsic(Instruction.X86Subps, mask, res);

                    if (op.RegisterSize == RegisterSize.Simd64)
                    {
                        res = context.VectorZeroUpper64(res);
                    }

                    context.Copy(GetVec(op.Rd), res);
                }
                else /* if (sizeF == 1) */
                {
                    Operand mask = X86GetAllElements(context, 2d);

                    Operand res = context.AddIntrinsic(Instruction.X86Mulpd, GetVec(op.Rn), GetVec(op.Rm));

                    res = context.AddIntrinsic(Instruction.X86Subpd, mask, res);

                    context.Copy(GetVec(op.Rd), res);
                }
            }
            else
            {
                EmitVectorBinaryOpF(context, (op1, op2) =>
                {
                    return EmitSoftFloatCall(context, nameof(SoftFloat32.FPRecipStepFused), op1, op2);
                });
            }
        }

        public static void Frecpx_S(EmitterContext context)
        {
            EmitScalarUnaryOpF(context, (op1) =>
            {
                return EmitSoftFloatCall(context, nameof(SoftFloat32.FPRecpX), op1);
            });
        }

        public static void Frinta_S(EmitterContext context)
        {
            EmitScalarUnaryOpF(context, (op1) =>
            {
                return EmitRoundMathCall(context, MidpointRounding.AwayFromZero, op1);
            });
        }

        public static void Frinta_V(EmitterContext context)
        {
            EmitVectorUnaryOpF(context, (op1) =>
            {
                return EmitRoundMathCall(context, MidpointRounding.AwayFromZero, op1);
            });
        }

        public static void Frinti_S(EmitterContext context)
        {
            OpCodeSimd op = (OpCodeSimd)context.CurrOp;

            EmitScalarUnaryOpF(context, (op1) =>
            {
                if (op.Size == 0)
                {
                    MethodInfo info = typeof(SoftFallback).GetMethod(nameof(SoftFallback.RoundF));

                    return context.Call(info, op1);
                }
                else /* if (op.Size == 1) */
                {
                    MethodInfo info = typeof(SoftFallback).GetMethod(nameof(SoftFallback.Round));

                    return context.Call(info, op1);
                }
            });
        }

        public static void Frinti_V(EmitterContext context)
        {
            OpCodeSimd op = (OpCodeSimd)context.CurrOp;

            int sizeF = op.Size & 1;

            EmitVectorUnaryOpF(context, (op1) =>
            {
                if (sizeF == 0)
                {
                    MethodInfo info = typeof(SoftFallback).GetMethod(nameof(SoftFallback.RoundF));

                    return context.Call(info, op1);
                }
                else /* if (sizeF == 1) */
                {
                    MethodInfo info = typeof(SoftFallback).GetMethod(nameof(SoftFallback.Round));

                    return context.Call(info, op1);
                }
            });
        }

        public static void Frintm_S(EmitterContext context)
        {
            if (Optimizations.UseSse41)
            {
                EmitScalarRoundOpF(context, FPRoundingMode.TowardsMinusInfinity);
            }
            else
            {
                EmitScalarUnaryOpF(context, (op1) =>
                {
                    return EmitUnaryMathCall(context, nameof(Math.Floor), op1);
                });
            }
        }

        public static void Frintm_V(EmitterContext context)
        {
            if (Optimizations.UseSse41)
            {
                EmitVectorRoundOpF(context, FPRoundingMode.TowardsMinusInfinity);
            }
            else
            {
                EmitVectorUnaryOpF(context, (op1) =>
                {
                    return EmitUnaryMathCall(context, nameof(Math.Floor), op1);
                });
            }
        }

        public static void Frintn_S(EmitterContext context)
        {
            if (Optimizations.UseSse41)
            {
                EmitScalarRoundOpF(context, FPRoundingMode.ToNearest);
            }
            else
            {
                EmitScalarUnaryOpF(context, (op1) =>
                {
                    return EmitRoundMathCall(context, MidpointRounding.ToEven, op1);
                });
            }
        }

        public static void Frintn_V(EmitterContext context)
        {
            if (Optimizations.UseSse41)
            {
                EmitVectorRoundOpF(context, FPRoundingMode.ToNearest);
            }
            else
            {
                EmitVectorUnaryOpF(context, (op1) =>
                {
                    return EmitRoundMathCall(context, MidpointRounding.ToEven, op1);
                });
            }
        }

        public static void Frintp_S(EmitterContext context)
        {
            if (Optimizations.UseSse41)
            {
                EmitScalarRoundOpF(context, FPRoundingMode.TowardsPlusInfinity);
            }
            else
            {
                EmitScalarUnaryOpF(context, (op1) =>
                {
                    return EmitUnaryMathCall(context, nameof(Math.Ceiling), op1);
                });
            }
        }

        public static void Frintp_V(EmitterContext context)
        {
            if (Optimizations.UseSse41)
            {
                EmitVectorRoundOpF(context, FPRoundingMode.TowardsPlusInfinity);
            }
            else
            {
                EmitVectorUnaryOpF(context, (op1) =>
                {
                    return EmitUnaryMathCall(context, nameof(Math.Ceiling), op1);
                });
            }
        }

        public static void Frintx_S(EmitterContext context)
        {
            OpCodeSimd op = (OpCodeSimd)context.CurrOp;

            EmitScalarUnaryOpF(context, (op1) =>
            {
                if (op.Size == 0)
                {
                    MethodInfo info = typeof(SoftFallback).GetMethod(nameof(SoftFallback.RoundF));

                    return context.Call(info, op1);
                }
                else /* if (op.Size == 1) */
                {
                    MethodInfo info = typeof(SoftFallback).GetMethod(nameof(SoftFallback.Round));

                    return context.Call(info, op1);
                }
            });
        }

        public static void Frintx_V(EmitterContext context)
        {
            OpCodeSimd op = (OpCodeSimd)context.CurrOp;

            int sizeF = op.Size & 1;

            EmitVectorUnaryOpF(context, (op1) =>
            {
                if (sizeF == 0)
                {
                    MethodInfo info = typeof(SoftFallback).GetMethod(nameof(SoftFallback.RoundF));

                    return context.Call(info, op1);
                }
                else /* if (sizeF == 1) */
                {
                    MethodInfo info = typeof(SoftFallback).GetMethod(nameof(SoftFallback.Round));

                    return context.Call(info, op1);
                }
            });
        }

        public static void Frintz_S(EmitterContext context)
        {
            if (Optimizations.UseSse41)
            {
                EmitScalarRoundOpF(context, FPRoundingMode.TowardsZero);
            }
            else
            {
                EmitScalarUnaryOpF(context, (op1) =>
                {
                    return EmitUnaryMathCall(context, nameof(Math.Truncate), op1);
                });
            }
        }

        public static void Frintz_V(EmitterContext context)
        {
            if (Optimizations.UseSse41)
            {
                EmitVectorRoundOpF(context, FPRoundingMode.TowardsZero);
            }
            else
            {
                EmitVectorUnaryOpF(context, (op1) =>
                {
                    return EmitUnaryMathCall(context, nameof(Math.Truncate), op1);
                });
            }
        }

        public static void Frsqrte_S(EmitterContext context)
        {
            OpCodeSimd op = (OpCodeSimd)context.CurrOp;

            int sizeF = op.Size & 1;

            if (Optimizations.FastFP && Optimizations.UseSse && sizeF == 0)
            {
                EmitScalarUnaryOpF(context, Instruction.X86Rsqrtss, 0);
            }
            else
            {
                EmitScalarUnaryOpF(context, (op1) =>
                {
                    return EmitSoftFloatCall(context, nameof(SoftFloat32.FPRSqrtEstimate), op1);
                });
            }
        }

        public static void Frsqrte_V(EmitterContext context)
        {
            OpCodeSimd op = (OpCodeSimd)context.CurrOp;

            int sizeF = op.Size & 1;

            if (Optimizations.FastFP && Optimizations.UseSse && sizeF == 0)
            {
                EmitVectorUnaryOpF(context, Instruction.X86Rsqrtps, 0);
            }
            else
            {
                EmitVectorUnaryOpF(context, (op1) =>
                {
                    return EmitSoftFloatCall(context, nameof(SoftFloat32.FPRSqrtEstimate), op1);
                });
            }
        }

        public static void Frsqrts_S(EmitterContext context) // Fused.
        {
            if (Optimizations.FastFP && Optimizations.UseSse2)
            {
                OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

                int sizeF = op.Size & 1;

                if (sizeF == 0)
                {
                    Operand maskHalf  = X86GetScalar(context, 0.5f);
                    Operand maskThree = X86GetScalar(context, 3f);

                    Operand res = context.AddIntrinsic(Instruction.X86Mulss, GetVec(op.Rn), GetVec(op.Rm));

                    res = context.AddIntrinsic(Instruction.X86Subss, maskThree, res);
                    res = context.AddIntrinsic(Instruction.X86Mulss, maskHalf,  res);

                    context.Copy(GetVec(op.Rd), context.VectorZeroUpper96(res));
                }
                else /* if (sizeF == 1) */
                {
                    Operand maskHalf  = X86GetScalar(context, 0.5d);
                    Operand maskThree = X86GetScalar(context, 3d);

                    Operand res = context.AddIntrinsic(Instruction.X86Mulsd, GetVec(op.Rn), GetVec(op.Rm));

                    res = context.AddIntrinsic(Instruction.X86Subsd, maskThree, res);
                    res = context.AddIntrinsic(Instruction.X86Mulsd, maskHalf,  res);

                    context.Copy(GetVec(op.Rd), context.VectorZeroUpper64(res));
                }
            }
            else
            {
                EmitScalarBinaryOpF(context, (op1, op2) =>
                {
                    return EmitSoftFloatCall(context, nameof(SoftFloat32.FPRSqrtStepFused), op1, op2);
                });
            }
        }

        public static void Frsqrts_V(EmitterContext context) // Fused.
        {
            if (Optimizations.FastFP && Optimizations.UseSse2)
            {
                OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

                int sizeF = op.Size & 1;

                if (sizeF == 0)
                {
                    Operand maskHalf  = X86GetAllElements(context, 0.5f);
                    Operand maskThree = X86GetAllElements(context, 3f);

                    Operand res = context.AddIntrinsic(Instruction.X86Mulps, GetVec(op.Rn), GetVec(op.Rm));

                    res = context.AddIntrinsic(Instruction.X86Subps, maskThree, res);
                    res = context.AddIntrinsic(Instruction.X86Mulps, maskHalf,  res);

                    if (op.RegisterSize == RegisterSize.Simd64)
                    {
                        res = context.VectorZeroUpper64(res);
                    }

                    context.Copy(GetVec(op.Rd), res);
                }
                else /* if (sizeF == 1) */
                {
                    Operand maskHalf  = X86GetAllElements(context, 0.5d);
                    Operand maskThree = X86GetAllElements(context, 3d);

                    Operand res = context.AddIntrinsic(Instruction.X86Mulpd, GetVec(op.Rn), GetVec(op.Rm));

                    res = context.AddIntrinsic(Instruction.X86Subpd, maskThree, res);
                    res = context.AddIntrinsic(Instruction.X86Mulpd, maskHalf,  res);

                    context.Copy(GetVec(op.Rd), res);
                }
            }
            else
            {
                EmitVectorBinaryOpF(context, (op1, op2) =>
                {
                    return EmitSoftFloatCall(context, nameof(SoftFloat32.FPRSqrtStepFused), op1, op2);
                });
            }
        }

        public static void Fsqrt_S(EmitterContext context)
        {
            if (Optimizations.FastFP && Optimizations.UseSse2)
            {
                EmitScalarUnaryOpF(context, Instruction.X86Sqrtss, Instruction.X86Sqrtsd);
            }
            else
            {
                EmitScalarUnaryOpF(context, (op1) =>
                {
                    return EmitSoftFloatCall(context, nameof(SoftFloat32.FPSqrt), op1);
                });
            }
        }

        public static void Fsqrt_V(EmitterContext context)
        {
            if (Optimizations.FastFP && Optimizations.UseSse2)
            {
                EmitVectorUnaryOpF(context, Instruction.X86Sqrtps, Instruction.X86Sqrtpd);
            }
            else
            {
                EmitVectorUnaryOpF(context, (op1) =>
                {
                    return EmitSoftFloatCall(context, nameof(SoftFloat32.FPSqrt), op1);
                });
            }
        }

        public static void Fsub_S(EmitterContext context)
        {
            if (Optimizations.FastFP && Optimizations.UseSse2)
            {
                EmitScalarBinaryOpF(context, Instruction.X86Subss, Instruction.X86Subsd);
            }
            else
            {
                EmitScalarBinaryOpF(context, (op1, op2) =>
                {
                    return EmitSoftFloatCall(context, nameof(SoftFloat32.FPSub), op1, op2);
                });
            }
        }

        public static void Fsub_V(EmitterContext context)
        {
            if (Optimizations.FastFP && Optimizations.UseSse2)
            {
                EmitVectorBinaryOpF(context, Instruction.X86Subps, Instruction.X86Subpd);
            }
            else
            {
                EmitVectorBinaryOpF(context, (op1, op2) =>
                {
                    return EmitSoftFloatCall(context, nameof(SoftFloat32.FPSub), op1, op2);
                });
            }
        }

        public static void Mla_V(EmitterContext context)
        {
            if (Optimizations.UseSse41)
            {
                EmitSse41Mul_AddSub(context, AddSub.Add);
            }
            else
            {
                EmitVectorTernaryOpZx(context, (op1, op2, op3) =>
                {
                    return context.Add(op1, context.Multiply(op2, op3));
                });
            }
        }

        public static void Mla_Ve(EmitterContext context)
        {
            EmitVectorTernaryOpByElemZx(context, (op1, op2, op3) =>
            {
                return context.Add(op1, context.Multiply(op2, op3));
            });
        }

        public static void Mls_V(EmitterContext context)
        {
            if (Optimizations.UseSse41)
            {
                EmitSse41Mul_AddSub(context, AddSub.Subtract);
            }
            else
            {
                EmitVectorTernaryOpZx(context, (op1, op2, op3) =>
                {
                    return context.Subtract(op1, context.Multiply(op2, op3));
                });
            }
        }

        public static void Mls_Ve(EmitterContext context)
        {
            EmitVectorTernaryOpByElemZx(context, (op1, op2, op3) =>
            {
                return context.Subtract(op1, context.Multiply(op2, op3));
            });
        }

        public static void Mul_V(EmitterContext context)
        {
            if (Optimizations.UseSse41)
            {
                EmitSse41Mul_AddSub(context, AddSub.None);
            }
            else
            {
                EmitVectorBinaryOpZx(context, (op1, op2) => context.Multiply(op1, op2));
            }
        }

        public static void Mul_Ve(EmitterContext context)
        {
            EmitVectorBinaryOpByElemZx(context, (op1, op2) => context.Multiply(op1, op2));
        }

        public static void Neg_S(EmitterContext context)
        {
            EmitScalarUnaryOpSx(context, (op1) => context.Negate(op1));
        }

        public static void Neg_V(EmitterContext context)
        {
            if (Optimizations.UseSse2)
            {
                OpCodeSimd op = (OpCodeSimd)context.CurrOp;

                Instruction subInst = X86PsubInstruction[op.Size];

                Operand res = context.AddIntrinsic(subInst, context.VectorZero(), GetVec(op.Rn));

                if (op.RegisterSize == RegisterSize.Simd64)
                {
                    res = context.VectorZeroUpper64(res);
                }

                context.Copy(GetVec(op.Rd), res);
            }
            else
            {
                EmitVectorUnaryOpSx(context, (op1) => context.Negate(op1));
            }
        }

        public static void Raddhn_V(EmitterContext context)
        {
            EmitHighNarrow(context, (op1, op2) => context.Add(op1, op2), round: true);
        }

        public static void Rsubhn_V(EmitterContext context)
        {
            EmitHighNarrow(context, (op1, op2) => context.Subtract(op1, op2), round: true);
        }

        public static void Saba_V(EmitterContext context)
        {
            EmitVectorTernaryOpSx(context, (op1, op2, op3) =>
            {
                return context.Add(op1, EmitAbs(context, context.Subtract(op2, op3)));
            });
        }

        public static void Sabal_V(EmitterContext context)
        {
            EmitVectorWidenRnRmTernaryOpSx(context, (op1, op2, op3) =>
            {
                return context.Add(op1, EmitAbs(context, context.Subtract(op2, op3)));
            });
        }

        public static void Sabd_V(EmitterContext context)
        {
            if (Optimizations.UseSse2)
            {
                OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

                Operand n = GetVec(op.Rn);
                Operand m = GetVec(op.Rm);

                EmitSse41Sabd(context, op, n, m, op.Size);
            }
            else
            {
                EmitVectorBinaryOpSx(context, (op1, op2) =>
                {
                    return EmitAbs(context, context.Subtract(op1, op2));
                });
            }
        }

        public static void Sabdl_V(EmitterContext context)
        {
            OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

            if (Optimizations.UseSse41 && op.Size < 2)
            {
                Operand n = GetVec(op.Rn);
                Operand m = GetVec(op.Rm);

                if (op.RegisterSize == RegisterSize.Simd128)
                {
                    n = context.AddIntrinsic(Instruction.X86Psrldq, n, Const(8));
                    m = context.AddIntrinsic(Instruction.X86Psrldq, m, Const(8));
                }

                Instruction movInst = op.Size == 0
                    ? Instruction.X86Pmovsxbw
                    : Instruction.X86Pmovsxwd;

                n = context.AddIntrinsic(movInst, n);
                m = context.AddIntrinsic(movInst, m);

                EmitSse41Sabd(context, op, n, m, op.Size + 1);
            }
            else
            {
                EmitVectorWidenRnRmBinaryOpSx(context, (op1, op2) =>
                {
                    return EmitAbs(context, context.Subtract(op1, op2));
                });
            }
        }

        public static void Sadalp_V(EmitterContext context)
        {
            EmitAddLongPairwise(context, signed: true, accumulate: true);
        }

        public static void Saddl_V(EmitterContext context)
        {
            if (Optimizations.UseSse41)
            {
                OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

                Operand n = GetVec(op.Rn);
                Operand m = GetVec(op.Rm);

                if (op.RegisterSize == RegisterSize.Simd128)
                {
                    n = context.AddIntrinsic(Instruction.X86Psrldq, n, Const(8));
                    m = context.AddIntrinsic(Instruction.X86Psrldq, m, Const(8));
                }

                Instruction movInst = X86PmovsxInstruction[op.Size];

                n = context.AddIntrinsic(movInst, n);
                m = context.AddIntrinsic(movInst, m);

                Instruction addInst = X86PaddInstruction[op.Size + 1];

                context.Copy(GetVec(op.Rd), context.AddIntrinsic(addInst, n, m));
            }
            else
            {
                EmitVectorWidenRnRmBinaryOpSx(context, (op1, op2) => context.Add(op1, op2));
            }
        }

        public static void Saddlp_V(EmitterContext context)
        {
            EmitAddLongPairwise(context, signed: true, accumulate: false);
        }

        public static void Saddlv_V(EmitterContext context)
        {
            EmitVectorLongAcrossVectorOpSx(context, (op1, op2) => context.Add(op1, op2));
        }

        public static void Saddw_V(EmitterContext context)
        {
            if (Optimizations.UseSse41)
            {
                OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

                Operand n = GetVec(op.Rn);
                Operand m = GetVec(op.Rm);

                if (op.RegisterSize == RegisterSize.Simd128)
                {
                    m = context.AddIntrinsic(Instruction.X86Psrldq, m, Const(8));
                }

                Instruction movInst = X86PmovsxInstruction[op.Size];

                m = context.AddIntrinsic(movInst, m);

                Instruction addInst = X86PaddInstruction[op.Size + 1];

                context.Copy(GetVec(op.Rd), context.AddIntrinsic(addInst, n, m));
            }
            else
            {
                EmitVectorWidenRmBinaryOpSx(context, (op1, op2) => context.Add(op1, op2));
            }
        }

        public static void Shadd_V(EmitterContext context)
        {
            OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

            if (Optimizations.UseSse2 && op.Size > 0)
            {
                Operand n = GetVec(op.Rn);
                Operand m = GetVec(op.Rm);

                Operand res  = context.AddIntrinsic(Instruction.X86Pand, n, m);
                Operand res2 = context.AddIntrinsic(Instruction.X86Pxor, n, m);

                Instruction shiftInst = op.Size == 1 ? Instruction.X86Psraw
                                                     : Instruction.X86Psrad;

                res2 = context.AddIntrinsic(shiftInst, res2, Const(1));

                Instruction addInst = X86PaddInstruction[op.Size];

                res = context.AddIntrinsic(addInst, res, res2);

                if (op.RegisterSize == RegisterSize.Simd64)
                {
                    res = context.VectorZeroUpper64(res);
                }

                context.Copy(GetVec(op.Rd), res);
            }
            else
            {
                EmitVectorBinaryOpSx(context, (op1, op2) =>
                {
                    return context.ShiftRightSI(context.Add(op1, op2), Const(1));
                });
            }
        }

        public static void Shsub_V(EmitterContext context)
        {
            OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

            if (Optimizations.UseSse2 && op.Size < 2)
            {
                Operand n = GetVec(op.Rn);
                Operand m = GetVec(op.Rm);

                Operand mask = X86GetAllElements(context, (int)(op.Size == 0 ? 0x80808080u : 0x80008000u));

                Instruction addInst = X86PaddInstruction[op.Size];

                Operand nPlusMask = context.AddIntrinsic(addInst, n, mask);
                Operand mPlusMask = context.AddIntrinsic(addInst, m, mask);

                Instruction avgInst = op.Size == 0 ? Instruction.X86Pavgb
                                                   : Instruction.X86Pavgw;

                Operand res = context.AddIntrinsic(avgInst, nPlusMask, mPlusMask);

                Instruction subInst = X86PsubInstruction[op.Size];

                res = context.AddIntrinsic(subInst, nPlusMask, res);

                if (op.RegisterSize == RegisterSize.Simd64)
                {
                    res = context.VectorZeroUpper64(res);
                }

                context.Copy(GetVec(op.Rd), res);
            }
            else
            {
                EmitVectorBinaryOpSx(context, (op1, op2) =>
                {
                    return context.ShiftRightSI(context.Subtract(op1, op2), Const(1));
                });
            }
        }

        public static void Smax_V(EmitterContext context)
        {
            if (Optimizations.UseSse41)
            {
                OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

                Operand n = GetVec(op.Rn);
                Operand m = GetVec(op.Rm);

                Instruction maxInst = X86PmaxsInstruction[op.Size];

                Operand res = context.AddIntrinsic(maxInst, n, m);

                if (op.RegisterSize == RegisterSize.Simd64)
                {
                    res = context.VectorZeroUpper64(res);
                }

                context.Copy(GetVec(op.Rd), res);
            }
            else
            {
                Type[] types = new Type[] { typeof(long), typeof(long) };

                MethodInfo info = typeof(Math).GetMethod(nameof(Math.Max), types);

                EmitVectorBinaryOpSx(context, (op1, op2) => context.Call(info, op1, op2));
            }
        }

        public static void Smaxp_V(EmitterContext context)
        {
            Type[] types = new Type[] { typeof(long), typeof(long) };

            MethodInfo info = typeof(Math).GetMethod(nameof(Math.Max), types);

            EmitVectorPairwiseOpSx(context, (op1, op2) => context.Call(info, op1, op2));
        }

        public static void Smaxv_V(EmitterContext context)
        {
            Type[] types = new Type[] { typeof(long), typeof(long) };

            MethodInfo info = typeof(Math).GetMethod(nameof(Math.Max), types);

            EmitVectorAcrossVectorOpSx(context, (op1, op2) => context.Call(info, op1, op2));
        }

        public static void Smin_V(EmitterContext context)
        {
            if (Optimizations.UseSse41)
            {
                OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

                Operand n = GetVec(op.Rn);
                Operand m = GetVec(op.Rm);

                Instruction minInst = X86PminsInstruction[op.Size];

                Operand res = context.AddIntrinsic(minInst, n, m);

                if (op.RegisterSize == RegisterSize.Simd64)
                {
                    res = context.VectorZeroUpper64(res);
                }

                context.Copy(GetVec(op.Rd), res);
            }
            else
            {
                Type[] types = new Type[] { typeof(long), typeof(long) };

                MethodInfo info = typeof(Math).GetMethod(nameof(Math.Min), types);

                EmitVectorBinaryOpSx(context, (op1, op2) => context.Call(info, op1, op2));
            }
        }

        public static void Sminp_V(EmitterContext context)
        {
            Type[] types = new Type[] { typeof(long), typeof(long) };

            MethodInfo info = typeof(Math).GetMethod(nameof(Math.Min), types);

            EmitVectorPairwiseOpSx(context, (op1, op2) => context.Call(info, op1, op2));
        }

        public static void Sminv_V(EmitterContext context)
        {
            Type[] types = new Type[] { typeof(long), typeof(long) };

            MethodInfo info = typeof(Math).GetMethod(nameof(Math.Min), types);

            EmitVectorAcrossVectorOpSx(context, (op1, op2) => context.Call(info, op1, op2));
        }

        public static void Smlal_V(EmitterContext context)
        {
            OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

            if (Optimizations.UseSse41 && op.Size < 2)
            {
                Operand d = GetVec(op.Rd);
                Operand n = GetVec(op.Rn);
                Operand m = GetVec(op.Rm);

                if (op.RegisterSize == RegisterSize.Simd128)
                {
                    n = context.AddIntrinsic(Instruction.X86Psrldq, n, Const(8));
                    m = context.AddIntrinsic(Instruction.X86Psrldq, m, Const(8));
                }

                Instruction movInst = X86PmovsxInstruction[op.Size];

                n = context.AddIntrinsic(movInst, n);
                m = context.AddIntrinsic(movInst, m);

                Instruction mullInst = op.Size == 0 ? Instruction.X86Pmullw
                                                    : Instruction.X86Pmulld;

                Operand res = context.AddIntrinsic(mullInst, n, m);

                Instruction addInst = X86PaddInstruction[op.Size + 1];

                context.Copy(d, context.AddIntrinsic(addInst, d, res));
            }
            else
            {
                EmitVectorWidenRnRmTernaryOpSx(context, (op1, op2, op3) =>
                {
                    return context.Add(op1, context.Multiply(op2, op3));
                });
            }
        }

        public static void Smlal_Ve(EmitterContext context)
        {
            EmitVectorWidenTernaryOpByElemSx(context, (op1, op2, op3) =>
            {
                return context.Add(op1, context.Multiply(op2, op3));
            });
        }

        public static void Smlsl_V(EmitterContext context)
        {
            OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

            if (Optimizations.UseSse41 && op.Size < 2)
            {
                Operand d = GetVec(op.Rd);
                Operand n = GetVec(op.Rn);
                Operand m = GetVec(op.Rm);

                if (op.RegisterSize == RegisterSize.Simd128)
                {
                    n = context.AddIntrinsic(Instruction.X86Psrldq, n, Const(8));
                    m = context.AddIntrinsic(Instruction.X86Psrldq, m, Const(8));
                }

                Instruction movInst = op.Size == 0
                    ? Instruction.X86Pmovsxbw
                    : Instruction.X86Pmovsxwd;

                n = context.AddIntrinsic(movInst, n);
                m = context.AddIntrinsic(movInst, m);

                Instruction mullInst = op.Size == 0 ? Instruction.X86Pmullw
                                                    : Instruction.X86Pmulld;

                Operand res = context.AddIntrinsic(mullInst, n, m);

                Instruction subInst = X86PsubInstruction[op.Size + 1];

                context.Copy(d, context.AddIntrinsic(subInst, d, res));
            }
            else
            {
                EmitVectorWidenRnRmTernaryOpSx(context, (op1, op2, op3) =>
                {
                    return context.Subtract(op1, context.Multiply(op2, op3));
                });
            }
        }

        public static void Smlsl_Ve(EmitterContext context)
        {
            EmitVectorWidenTernaryOpByElemSx(context, (op1, op2, op3) =>
            {
                return context.Subtract(op1, context.Multiply(op2, op3));
            });
        }

        public static void Smull_V(EmitterContext context)
        {
            EmitVectorWidenRnRmBinaryOpSx(context, (op1, op2) => context.Multiply(op1, op2));
        }

        public static void Smull_Ve(EmitterContext context)
        {
            EmitVectorWidenBinaryOpByElemSx(context, (op1, op2) => context.Multiply(op1, op2));
        }

        public static void Sqabs_S(EmitterContext context)
        {
            EmitScalarSaturatingUnaryOpSx(context, (op1) => EmitAbs(context, op1));
        }

        public static void Sqabs_V(EmitterContext context)
        {
            EmitVectorSaturatingUnaryOpSx(context, (op1) => EmitAbs(context, op1));
        }

        public static void Sqadd_S(EmitterContext context)
        {
            EmitScalarSaturatingBinaryOpSx(context, SaturatingFlags.Add);
        }

        public static void Sqadd_V(EmitterContext context)
        {
            EmitVectorSaturatingBinaryOpSx(context, SaturatingFlags.Add);
        }

        public static void Sqdmulh_S(EmitterContext context)
        {
            EmitSaturatingBinaryOp(context, (op1, op2) => EmitDoublingMultiplyHighHalf(context, op1, op2, round: false), SaturatingFlags.ScalarSx);
        }

        public static void Sqdmulh_V(EmitterContext context)
        {
            EmitSaturatingBinaryOp(context, (op1, op2) => EmitDoublingMultiplyHighHalf(context, op1, op2, round: false), SaturatingFlags.VectorSx);
        }

        public static void Sqneg_S(EmitterContext context)
        {
            EmitScalarSaturatingUnaryOpSx(context, (op1) => context.Negate(op1));
        }

        public static void Sqneg_V(EmitterContext context)
        {
            EmitVectorSaturatingUnaryOpSx(context, (op1) => context.Negate(op1));
        }

        public static void Sqrdmulh_S(EmitterContext context)
        {
            EmitSaturatingBinaryOp(context, (op1, op2) => EmitDoublingMultiplyHighHalf(context, op1, op2, round: true), SaturatingFlags.ScalarSx);
        }

        public static void Sqrdmulh_V(EmitterContext context)
        {
            EmitSaturatingBinaryOp(context, (op1, op2) => EmitDoublingMultiplyHighHalf(context, op1, op2, round: true), SaturatingFlags.VectorSx);
        }

        public static void Sqsub_S(EmitterContext context)
        {
            EmitScalarSaturatingBinaryOpSx(context, SaturatingFlags.Sub);
        }

        public static void Sqsub_V(EmitterContext context)
        {
            EmitVectorSaturatingBinaryOpSx(context, SaturatingFlags.Sub);
        }

        public static void Sqxtn_S(EmitterContext context)
        {
            EmitSaturatingNarrowOp(context, SaturatingNarrowFlags.ScalarSxSx);
        }

        public static void Sqxtn_V(EmitterContext context)
        {
            EmitSaturatingNarrowOp(context, SaturatingNarrowFlags.VectorSxSx);
        }

        public static void Sqxtun_S(EmitterContext context)
        {
            EmitSaturatingNarrowOp(context, SaturatingNarrowFlags.ScalarSxZx);
        }

        public static void Sqxtun_V(EmitterContext context)
        {
            EmitSaturatingNarrowOp(context, SaturatingNarrowFlags.VectorSxZx);
        }

        public static void Srhadd_V(EmitterContext context)
        {
            OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

            if (Optimizations.UseSse2 && op.Size < 2)
            {
                Operand n = GetVec(op.Rn);
                Operand m = GetVec(op.Rm);

                Operand mask = X86GetAllElements(context, (int)(op.Size == 0 ? 0x80808080u : 0x80008000u));

                Instruction subInst = X86PsubInstruction[op.Size];

                Operand nMinusMask = context.AddIntrinsic(subInst, n, mask);
                Operand mMinusMask = context.AddIntrinsic(subInst, m, mask);

                Instruction avgInst = op.Size == 0 ? Instruction.X86Pavgb
                                                   : Instruction.X86Pavgw;

                Operand res = context.AddIntrinsic(avgInst, nMinusMask, mMinusMask);

                Instruction addInst = X86PaddInstruction[op.Size];

                res = context.AddIntrinsic(addInst, mask, res);

                if (op.RegisterSize == RegisterSize.Simd64)
                {
                    res = context.VectorZeroUpper64(res);
                }

                context.Copy(GetVec(op.Rd), res);
            }
            else
            {
                EmitVectorBinaryOpSx(context, (op1, op2) =>
                {
                    Operand res = context.Add(op1, op2);

                    res = context.Add(res, Const(1L));

                    return context.ShiftRightSI(res, Const(1));
                });
            }
        }

        public static void Ssubl_V(EmitterContext context)
        {
            if (Optimizations.UseSse41)
            {
                OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

                Operand n = GetVec(op.Rn);
                Operand m = GetVec(op.Rm);

                if (op.RegisterSize == RegisterSize.Simd128)
                {
                    n = context.AddIntrinsic(Instruction.X86Psrldq, n, Const(8));
                    m = context.AddIntrinsic(Instruction.X86Psrldq, m, Const(8));
                }

                Instruction movInst = X86PmovsxInstruction[op.Size];

                n = context.AddIntrinsic(movInst, n);
                m = context.AddIntrinsic(movInst, m);

                Instruction subInst = X86PsubInstruction[op.Size + 1];

                context.Copy(GetVec(op.Rd), context.AddIntrinsic(subInst, n, m));
            }
            else
            {
                EmitVectorWidenRnRmBinaryOpSx(context, (op1, op2) => context.Subtract(op1, op2));
            }
        }

        public static void Ssubw_V(EmitterContext context)
        {
            if (Optimizations.UseSse41)
            {
                OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

                Operand n = GetVec(op.Rn);
                Operand m = GetVec(op.Rm);

                if (op.RegisterSize == RegisterSize.Simd128)
                {
                    m = context.AddIntrinsic(Instruction.X86Psrldq, m, Const(8));
                }

                Instruction movInst = X86PmovsxInstruction[op.Size];

                m = context.AddIntrinsic(movInst, m);

                Instruction subInst = X86PsubInstruction[op.Size + 1];

                context.Copy(GetVec(op.Rd), context.AddIntrinsic(subInst, n, m));
            }
            else
            {
                EmitVectorWidenRmBinaryOpSx(context, (op1, op2) => context.Subtract(op1, op2));
            }
        }

        public static void Sub_S(EmitterContext context)
        {
            EmitScalarBinaryOpZx(context, (op1, op2) => context.Subtract(op1, op2));
        }

        public static void Sub_V(EmitterContext context)
        {
            if (Optimizations.UseSse2)
            {
                OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

                Operand n = GetVec(op.Rn);
                Operand m = GetVec(op.Rm);

                Instruction subInst = X86PsubInstruction[op.Size];

                Operand res = context.AddIntrinsic(subInst, n, m);

                if (op.RegisterSize == RegisterSize.Simd64)
                {
                    res = context.VectorZeroUpper64(res);
                }

                context.Copy(GetVec(op.Rd), res);
            }
            else
            {
                EmitVectorBinaryOpZx(context, (op1, op2) => context.Subtract(op1, op2));
            }
        }

        public static void Subhn_V(EmitterContext context)
        {
            EmitHighNarrow(context, (op1, op2) => context.Subtract(op1, op2), round: false);
        }

        public static void Suqadd_S(EmitterContext context)
        {
            EmitScalarSaturatingBinaryOpSx(context, SaturatingFlags.Accumulate);
        }

        public static void Suqadd_V(EmitterContext context)
        {
            EmitVectorSaturatingBinaryOpSx(context, SaturatingFlags.Accumulate);
        }

        public static void Uaba_V(EmitterContext context)
        {
            EmitVectorTernaryOpZx(context, (op1, op2, op3) =>
            {
                return context.Add(op1, EmitAbs(context, context.Subtract(op2, op3)));
            });
        }

        public static void Uabal_V(EmitterContext context)
        {
            EmitVectorWidenRnRmTernaryOpZx(context, (op1, op2, op3) =>
            {
                return context.Add(op1, EmitAbs(context, context.Subtract(op2, op3)));
            });
        }

        public static void Uabd_V(EmitterContext context)
        {
            if (Optimizations.UseSse41)
            {
                OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

                Operand n = GetVec(op.Rn);
                Operand m = GetVec(op.Rm);

                EmitSse41Uabd(context, op, n, m, op.Size);
            }
            else
            {
                EmitVectorBinaryOpZx(context, (op1, op2) =>
                {
                    return EmitAbs(context, context.Subtract(op1, op2));
                });
            }
        }

        public static void Uabdl_V(EmitterContext context)
        {
            OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

            if (Optimizations.UseSse41 && op.Size < 2)
            {
                Operand n = GetVec(op.Rn);
                Operand m = GetVec(op.Rm);

                if (op.RegisterSize == RegisterSize.Simd128)
                {
                    n = context.AddIntrinsic(Instruction.X86Psrldq, n, Const(8));
                    m = context.AddIntrinsic(Instruction.X86Psrldq, m, Const(8));
                }

                Instruction movInst = op.Size == 0
                    ? Instruction.X86Pmovzxbw
                    : Instruction.X86Pmovzxwd;

                n = context.AddIntrinsic(movInst, n);
                m = context.AddIntrinsic(movInst, m);

                EmitSse41Uabd(context, op, n, m, op.Size + 1);
            }
            else
            {
                EmitVectorWidenRnRmBinaryOpZx(context, (op1, op2) =>
                {
                    return EmitAbs(context, context.Subtract(op1, op2));
                });
            }
        }

        public static void Uadalp_V(EmitterContext context)
        {
            EmitAddLongPairwise(context, signed: false, accumulate: true);
        }

        public static void Uaddl_V(EmitterContext context)
        {
            if (Optimizations.UseSse41)
            {
                OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

                Operand n = GetVec(op.Rn);
                Operand m = GetVec(op.Rm);

                if (op.RegisterSize == RegisterSize.Simd128)
                {
                    n = context.AddIntrinsic(Instruction.X86Psrldq, n, Const(8));
                    m = context.AddIntrinsic(Instruction.X86Psrldq, m, Const(8));
                }

                Instruction movInst = X86PmovzxInstruction[op.Size];

                n = context.AddIntrinsic(movInst, n);
                m = context.AddIntrinsic(movInst, m);

                Instruction addInst = X86PaddInstruction[op.Size + 1];

                context.Copy(GetVec(op.Rd), context.AddIntrinsic(addInst, n, m));
            }
            else
            {
                EmitVectorWidenRnRmBinaryOpZx(context, (op1, op2) => context.Add(op1, op2));
            }
        }

        public static void Uaddlp_V(EmitterContext context)
        {
            EmitAddLongPairwise(context, signed: false, accumulate: false);
        }

        public static void Uaddlv_V(EmitterContext context)
        {
            EmitVectorLongAcrossVectorOpZx(context, (op1, op2) => context.Add(op1, op2));
        }

        public static void Uaddw_V(EmitterContext context)
        {
            if (Optimizations.UseSse41)
            {
                OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

                Operand n = GetVec(op.Rn);
                Operand m = GetVec(op.Rm);

                if (op.RegisterSize == RegisterSize.Simd128)
                {
                    m = context.AddIntrinsic(Instruction.X86Psrldq, m, Const(8));
                }

                Instruction movInst = X86PmovzxInstruction[op.Size];

                m = context.AddIntrinsic(movInst, m);

                Instruction addInst = X86PaddInstruction[op.Size + 1];

                context.Copy(GetVec(op.Rd), context.AddIntrinsic(addInst, n, m));
            }
            else
            {
                EmitVectorWidenRmBinaryOpZx(context, (op1, op2) => context.Add(op1, op2));
            }
        }

        public static void Uhadd_V(EmitterContext context)
        {
            OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

            if (Optimizations.UseSse2 && op.Size > 0)
            {
                Operand n = GetVec(op.Rn);
                Operand m = GetVec(op.Rm);

                Operand res  = context.AddIntrinsic(Instruction.X86Pand, n, m);
                Operand res2 = context.AddIntrinsic(Instruction.X86Pxor, n, m);

                Instruction shiftInst = op.Size == 1 ? Instruction.X86Psrlw
                                                     : Instruction.X86Psrld;

                res2 = context.AddIntrinsic(shiftInst, res2, Const(1));

                Instruction addInst = X86PaddInstruction[op.Size];

                res = context.AddIntrinsic(addInst, res, res2);

                if (op.RegisterSize == RegisterSize.Simd64)
                {
                    res = context.VectorZeroUpper64(res);
                }

                context.Copy(GetVec(op.Rd), res);
            }
            else
            {
                EmitVectorBinaryOpZx(context, (op1, op2) =>
                {
                    return context.ShiftRightUI(context.Add(op1, op2), Const(1));
                });
            }
        }

        public static void Uhsub_V(EmitterContext context)
        {
            OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

            if (Optimizations.UseSse2 && op.Size < 2)
            {
                Operand n = GetVec(op.Rn);
                Operand m = GetVec(op.Rm);

                Instruction avgInst = op.Size == 0 ? Instruction.X86Pavgb
                                                   : Instruction.X86Pavgw;

                Operand res = context.AddIntrinsic(avgInst, n, m);

                Instruction subInst = X86PsubInstruction[op.Size];

                res = context.AddIntrinsic(subInst, n, res);

                if (op.RegisterSize == RegisterSize.Simd64)
                {
                    res = context.VectorZeroUpper64(res);
                }

                context.Copy(GetVec(op.Rd), res);
            }
            else
            {
                EmitVectorBinaryOpZx(context, (op1, op2) =>
                {
                    return context.ShiftRightUI(context.Subtract(op1, op2), Const(1));
                });
            }
        }

        public static void Umax_V(EmitterContext context)
        {
            if (Optimizations.UseSse41)
            {
                OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

                Operand n = GetVec(op.Rn);
                Operand m = GetVec(op.Rm);

                Instruction maxInst = X86PmaxuInstruction[op.Size];

                Operand res = context.AddIntrinsic(maxInst, n, m);

                if (op.RegisterSize == RegisterSize.Simd64)
                {
                    res = context.VectorZeroUpper64(res);
                }

                context.Copy(GetVec(op.Rd), res);
            }
            else
            {
                Type[] types = new Type[] { typeof(ulong), typeof(ulong) };

                MethodInfo info = typeof(Math).GetMethod(nameof(Math.Max), types);

                EmitVectorBinaryOpZx(context, (op1, op2) => context.Call(info, op1, op2));
            }
        }

        public static void Umaxp_V(EmitterContext context)
        {
            Type[] types = new Type[] { typeof(ulong), typeof(ulong) };

            MethodInfo info = typeof(Math).GetMethod(nameof(Math.Max), types);

            EmitVectorPairwiseOpZx(context, (op1, op2) => context.Call(info, op1, op2));
        }

        public static void Umaxv_V(EmitterContext context)
        {
            Type[] types = new Type[] { typeof(ulong), typeof(ulong) };

            MethodInfo info = typeof(Math).GetMethod(nameof(Math.Max), types);

            EmitVectorAcrossVectorOpZx(context, (op1, op2) => context.Call(info, op1, op2));
        }

        public static void Umin_V(EmitterContext context)
        {
            if (Optimizations.UseSse41)
            {
                OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

                Operand n = GetVec(op.Rn);
                Operand m = GetVec(op.Rm);

                Instruction minInst = X86PminuInstruction[op.Size];

                Operand res = context.AddIntrinsic(minInst, n, m);

                if (op.RegisterSize == RegisterSize.Simd64)
                {
                    res = context.VectorZeroUpper64(res);
                }

                context.Copy(GetVec(op.Rd), res);
            }
            else
            {
                Type[] types = new Type[] { typeof(ulong), typeof(ulong) };

                MethodInfo info = typeof(Math).GetMethod(nameof(Math.Min), types);

                EmitVectorBinaryOpZx(context, (op1, op2) => context.Call(info, op1, op2));
            }
        }

        public static void Uminp_V(EmitterContext context)
        {
            Type[] types = new Type[] { typeof(ulong), typeof(ulong) };

            MethodInfo info = typeof(Math).GetMethod(nameof(Math.Min), types);

            EmitVectorPairwiseOpZx(context, (op1, op2) => context.Call(info, op1, op2));
        }

        public static void Uminv_V(EmitterContext context)
        {
            Type[] types = new Type[] { typeof(ulong), typeof(ulong) };

            MethodInfo info = typeof(Math).GetMethod(nameof(Math.Min), types);

            EmitVectorAcrossVectorOpZx(context, (op1, op2) => context.Call(info, op1, op2));
        }

        public static void Umlal_V(EmitterContext context)
        {
            OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

            if (Optimizations.UseSse41 && op.Size < 2)
            {
                Operand d = GetVec(op.Rd);
                Operand n = GetVec(op.Rn);
                Operand m = GetVec(op.Rm);

                if (op.RegisterSize == RegisterSize.Simd128)
                {
                    n = context.AddIntrinsic(Instruction.X86Psrldq, n, Const(8));
                    m = context.AddIntrinsic(Instruction.X86Psrldq, m, Const(8));
                }

                Instruction movInst = X86PmovzxInstruction[op.Size];

                n = context.AddIntrinsic(movInst, n);
                m = context.AddIntrinsic(movInst, m);

                Instruction mullInst = op.Size == 0 ? Instruction.X86Pmullw
                                                    : Instruction.X86Pmulld;

                Operand res = context.AddIntrinsic(mullInst, n, m);

                Instruction addInst = X86PaddInstruction[op.Size + 1];

                context.Copy(d, context.AddIntrinsic(addInst, d, res));
            }
            else
            {
                EmitVectorWidenRnRmTernaryOpZx(context, (op1, op2, op3) =>
                {
                    return context.Add(op1, context.Multiply(op2, op3));
                });
            }
        }

        public static void Umlal_Ve(EmitterContext context)
        {
            EmitVectorWidenTernaryOpByElemZx(context, (op1, op2, op3) =>
            {
                return context.Add(op1, context.Multiply(op2, op3));
            });
        }

        public static void Umlsl_V(EmitterContext context)
        {
            OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

            if (Optimizations.UseSse41 && op.Size < 2)
            {
                Operand d = GetVec(op.Rd);
                Operand n = GetVec(op.Rn);
                Operand m = GetVec(op.Rm);

                if (op.RegisterSize == RegisterSize.Simd128)
                {
                    n = context.AddIntrinsic(Instruction.X86Psrldq, n, Const(8));
                    m = context.AddIntrinsic(Instruction.X86Psrldq, m, Const(8));
                }

                Instruction movInst = op.Size == 0
                    ? Instruction.X86Pmovzxbw
                    : Instruction.X86Pmovzxwd;

                n = context.AddIntrinsic(movInst, n);
                m = context.AddIntrinsic(movInst, m);

                Instruction mullInst = op.Size == 0 ? Instruction.X86Pmullw
                                                    : Instruction.X86Pmulld;

                Operand res = context.AddIntrinsic(mullInst, n, m);

                Instruction subInst = X86PsubInstruction[op.Size + 1];

                context.Copy(d, context.AddIntrinsic(subInst, d, res));
            }
            else
            {
                EmitVectorWidenRnRmTernaryOpZx(context, (op1, op2, op3) =>
                {
                    return context.Subtract(op1, context.Multiply(op2, op3));
                });
            }
        }

        public static void Umlsl_Ve(EmitterContext context)
        {
            EmitVectorWidenTernaryOpByElemZx(context, (op1, op2, op3) =>
            {
                return context.Subtract(op1, context.Multiply(op2, op3));
            });
        }

        public static void Umull_V(EmitterContext context)
        {
            EmitVectorWidenRnRmBinaryOpZx(context, (op1, op2) => context.Multiply(op1, op2));
        }

        public static void Umull_Ve(EmitterContext context)
        {
            EmitVectorWidenBinaryOpByElemZx(context, (op1, op2) => context.Multiply(op1, op2));
        }

        public static void Uqadd_S(EmitterContext context)
        {
            EmitScalarSaturatingBinaryOpZx(context, SaturatingFlags.Add);
        }

        public static void Uqadd_V(EmitterContext context)
        {
            EmitVectorSaturatingBinaryOpZx(context, SaturatingFlags.Add);
        }

        public static void Uqsub_S(EmitterContext context)
        {
            EmitScalarSaturatingBinaryOpZx(context, SaturatingFlags.Sub);
        }

        public static void Uqsub_V(EmitterContext context)
        {
            EmitVectorSaturatingBinaryOpZx(context, SaturatingFlags.Sub);
        }

        public static void Uqxtn_S(EmitterContext context)
        {
            EmitSaturatingNarrowOp(context, SaturatingNarrowFlags.ScalarZxZx);
        }

        public static void Uqxtn_V(EmitterContext context)
        {
            EmitSaturatingNarrowOp(context, SaturatingNarrowFlags.VectorZxZx);
        }

        public static void Urhadd_V(EmitterContext context)
        {
            OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

            if (Optimizations.UseSse2 && op.Size < 2)
            {
                Operand n = GetVec(op.Rn);
                Operand m = GetVec(op.Rm);

                Instruction avgInst = op.Size == 0 ? Instruction.X86Pavgb
                                                   : Instruction.X86Pavgw;

                Operand res = context.AddIntrinsic(avgInst, n, m);

                if (op.RegisterSize == RegisterSize.Simd64)
                {
                    res = context.VectorZeroUpper64(res);
                }

                context.Copy(GetVec(op.Rd), res);
            }
            else
            {
                EmitVectorBinaryOpSx(context, (op1, op2) =>
                {
                    Operand res = context.Add(op1, op2);

                    res = context.Add(res, Const(1L));

                    return context.ShiftRightUI(res, Const(1));
                });
            }
        }

        public static void Usqadd_S(EmitterContext context)
        {
            EmitScalarSaturatingBinaryOpZx(context, SaturatingFlags.Accumulate);
        }

        public static void Usqadd_V(EmitterContext context)
        {
            EmitVectorSaturatingBinaryOpZx(context, SaturatingFlags.Accumulate);
        }

        public static void Usubl_V(EmitterContext context)
        {
            if (Optimizations.UseSse41)
            {
                OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

                Operand n = GetVec(op.Rn);
                Operand m = GetVec(op.Rm);

                if (op.RegisterSize == RegisterSize.Simd128)
                {
                    n = context.AddIntrinsic(Instruction.X86Psrldq, n, Const(8));
                    m = context.AddIntrinsic(Instruction.X86Psrldq, m, Const(8));
                }

                Instruction movInst = X86PmovzxInstruction[op.Size];

                n = context.AddIntrinsic(movInst, n);
                m = context.AddIntrinsic(movInst, m);

                Instruction subInst = X86PsubInstruction[op.Size + 1];

                context.Copy(GetVec(op.Rd), context.AddIntrinsic(subInst, n, m));
            }
            else
            {
                EmitVectorWidenRnRmBinaryOpZx(context, (op1, op2) => context.Subtract(op1, op2));
            }
        }

        public static void Usubw_V(EmitterContext context)
        {
            if (Optimizations.UseSse41)
            {
                OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

                Operand n = GetVec(op.Rn);
                Operand m = GetVec(op.Rm);

                if (op.RegisterSize == RegisterSize.Simd128)
                {
                    m = context.AddIntrinsic(Instruction.X86Psrldq, m, Const(8));
                }

                Instruction movInst = X86PmovzxInstruction[op.Size];

                m = context.AddIntrinsic(movInst, m);

                Instruction subInst = X86PsubInstruction[op.Size + 1];

                context.Copy(GetVec(op.Rd), context.AddIntrinsic(subInst, n, m));
            }
            else
            {
                EmitVectorWidenRmBinaryOpZx(context, (op1, op2) => context.Subtract(op1, op2));
            }
        }

        private static Operand EmitAbs(EmitterContext context, Operand value)
        {
            Operand isPositive = context.ICompareGreaterOrEqual(value, Const(value.Type, 0));

            return context.ConditionalSelect(isPositive, value, context.Negate(value));
        }

        private static void EmitAddLongPairwise(EmitterContext context, bool signed, bool accumulate)
        {
            OpCodeSimd op = (OpCodeSimd)context.CurrOp;

            Operand res = context.VectorZero();

            int pairs = op.GetPairsCount() >> op.Size;

            for (int index = 0; index < pairs; index++)
            {
                int pairIndex = index << 1;

                Operand ne0 = EmitVectorExtract(context, op.Rn, pairIndex,     op.Size, signed);
                Operand ne1 = EmitVectorExtract(context, op.Rn, pairIndex + 1, op.Size, signed);

                Operand e = context.Add(ne0, ne1);

                if (accumulate)
                {
                    Operand de = EmitVectorExtract(context, op.Rd, index, op.Size + 1, signed);

                    e = context.Add(e, de);
                }

                res = EmitVectorInsert(context, res, e, index, op.Size + 1);
            }

            context.Copy(GetVec(op.Rd), res);
        }

        private static Operand EmitDoublingMultiplyHighHalf(
            EmitterContext context,
            Operand n,
            Operand m,
            bool round)
        {
            OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

            int eSize = 8 << op.Size;

            Operand res = context.Multiply(n, m);

            if (!round)
            {
                res = context.ShiftRightSI(res, Const(eSize - 1));
            }
            else
            {
                long roundConst = 1L << (eSize - 1);

                res = context.ShiftLeft(res, Const(1));

                res = context.Add(res, Const(roundConst));

                res = context.ShiftRightSI(res, Const(eSize));

                Operand isIntMin = context.ICompareEqual(res, Const((long)int.MinValue));

                res = context.ConditionalSelect(isIntMin, context.Negate(res), res);
            }

            return res;
        }

        private static void EmitHighNarrow(EmitterContext context, Func2I emit, bool round)
        {
            OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

            int elems = 8 >> op.Size;
            int eSize = 8 << op.Size;

            int part = op.RegisterSize == RegisterSize.Simd128 ? elems : 0;

            Operand res = part == 0 ? context.VectorZero() : context.Copy(GetVec(op.Rd));

            long roundConst = 1L << (eSize - 1);

            for (int index = 0; index < elems; index++)
            {
                Operand ne = EmitVectorExtractZx(context, op.Rn, index, op.Size + 1);
                Operand me = EmitVectorExtractZx(context, op.Rm, index, op.Size + 1);

                Operand de = emit(ne, me);

                if (round)
                {
                    de = context.Add(de, Const(roundConst));
                }

                de = context.ShiftRightUI(de, Const(eSize));

                res = EmitVectorInsert(context, res, de, part + index, op.Size);
            }

            context.Copy(GetVec(op.Rd), res);
        }

        public static void EmitScalarRoundOpF(EmitterContext context, FPRoundingMode roundMode)
        {
            OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

            Operand n = GetVec(op.Rn);

            Instruction inst = (op.Size & 1) != 0 ? Instruction.X86Roundsd
                                                  : Instruction.X86Roundss;

            Operand res = context.AddIntrinsic(inst, n, Const(X86GetRoundControl(roundMode)));

            if ((op.Size & 1) != 0)
            {
                res = context.VectorZeroUpper64(res);
            }
            else
            {
                res = context.VectorZeroUpper96(res);
            }

            context.Copy(GetVec(op.Rd), res);
        }

        public static void EmitVectorRoundOpF(EmitterContext context, FPRoundingMode roundMode)
        {
            OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

            Operand n = GetVec(op.Rn);

            Instruction inst = (op.Size & 1) != 0 ? Instruction.X86Roundpd
                                                  : Instruction.X86Roundps;

            Operand res = context.AddIntrinsic(inst, n, Const(X86GetRoundControl(roundMode)));

            if (op.RegisterSize == RegisterSize.Simd64)
            {
                res = context.VectorZeroUpper64(res);
            }

            context.Copy(GetVec(op.Rd), res);
        }

        private enum AddSub
        {
            None,
            Add,
            Subtract
        }

        private static void EmitSse41Mul_AddSub(EmitterContext context, AddSub addSub)
        {
            OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

            Operand n = GetVec(op.Rn);
            Operand m = GetVec(op.Rm);

            Operand res = null;

            if (op.Size == 0)
            {
                Operand ns8 = context.AddIntrinsic(Instruction.X86Psrlw, n, Const(8));
                Operand ms8 = context.AddIntrinsic(Instruction.X86Psrlw, m, Const(8));

                res = context.AddIntrinsic(Instruction.X86Pmullw, ns8, ms8);

                res = context.AddIntrinsic(Instruction.X86Psllw, res, Const(8));

                Operand res2 = context.AddIntrinsic(Instruction.X86Pmullw, n, m);

                Operand mask = X86GetAllElements(context, 0x00FF00FF);

                res = context.AddIntrinsic(Instruction.X86Pblendvb, res, res2, mask);
            }
            else if (op.Size == 1)
            {
                res = context.AddIntrinsic(Instruction.X86Pmullw, n, m);
            }
            else
            {
                res = context.AddIntrinsic(Instruction.X86Pmulld, n, m);
            }

            Operand d = GetVec(op.Rd);

            if (addSub == AddSub.Add)
            {
                switch (op.Size)
                {
                    case 0: res = context.AddIntrinsic(Instruction.X86Paddb, d, res); break;
                    case 1: res = context.AddIntrinsic(Instruction.X86Paddw, d, res); break;
                    case 2: res = context.AddIntrinsic(Instruction.X86Paddd, d, res); break;
                    case 3: res = context.AddIntrinsic(Instruction.X86Paddq, d, res); break;
                }
            }
            else if (addSub == AddSub.Subtract)
            {
                switch (op.Size)
                {
                    case 0: res = context.AddIntrinsic(Instruction.X86Psubb, d, res); break;
                    case 1: res = context.AddIntrinsic(Instruction.X86Psubw, d, res); break;
                    case 2: res = context.AddIntrinsic(Instruction.X86Psubd, d, res); break;
                    case 3: res = context.AddIntrinsic(Instruction.X86Psubq, d, res); break;
                }
            }

            if (op.RegisterSize == RegisterSize.Simd64)
            {
                res = context.VectorZeroUpper64(res);
            }

            context.Copy(d, res);
        }

        private static void EmitSse41Sabd(
            EmitterContext context,
            OpCodeSimdReg op,
            Operand n,
            Operand m,
            int size)
        {
            Instruction cmpgtInst = X86PcmpgtInstruction[size];

            Operand cmpMask = context.AddIntrinsic(cmpgtInst, n, m);

            Instruction subInst = X86PsubInstruction[size];

            Operand res = context.AddIntrinsic(subInst, n, m);

            res = context.AddIntrinsic(Instruction.X86Pand, cmpMask, res);

            Operand res2 = context.AddIntrinsic(subInst, m, n);

            res2 = context.AddIntrinsic(Instruction.X86Pandn, cmpMask, res2);

            res = context.AddIntrinsic(Instruction.X86Por, res, res2);

            if (op.RegisterSize == RegisterSize.Simd64)
            {
                res = context.VectorZeroUpper64(res);
            }

            context.Copy(GetVec(op.Rd), res);
        }

        private static void EmitSse41Uabd(
            EmitterContext context,
            OpCodeSimdReg op,
            Operand n,
            Operand m,
            int size)
        {
            Instruction maxInst = X86PmaxuInstruction[size];

            Operand max = context.AddIntrinsic(maxInst, m, n);

            Instruction cmpeqInst = X86PcmpeqInstruction[size];

            Operand cmpMask = context.AddIntrinsic(cmpeqInst, max, m);

            Operand onesMask = X86GetAllElements(context, -1L);

            cmpMask = context.AddIntrinsic(Instruction.X86Pand, cmpMask, onesMask);

            Instruction subInst = X86PsubInstruction[size];

            Operand res  = context.AddIntrinsic(subInst, n, m);
            Operand res2 = context.AddIntrinsic(subInst, m, n);

            res  = context.AddIntrinsic(Instruction.X86Pand,  cmpMask, res);
            res2 = context.AddIntrinsic(Instruction.X86Pandn, cmpMask, res2);

            res = context.AddIntrinsic(Instruction.X86Por, res, res2);

            if (op.RegisterSize == RegisterSize.Simd64)
            {
                res = context.VectorZeroUpper64(res);
            }

            context.Copy(GetVec(op.Rd), res);
        }
    }
}
