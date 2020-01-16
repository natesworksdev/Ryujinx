using ARMeilleure.Decoders;
using ARMeilleure.IntermediateRepresentation;
using ARMeilleure.Translation;
using System;
using System.Collections.Generic;
using System.Text;

using static ARMeilleure.Instructions.InstEmitHelper;
using static ARMeilleure.Instructions.InstEmitSimdHelper;
using static ARMeilleure.Instructions.InstEmitSimdHelper32;
using static ARMeilleure.Instructions.InstEmitFlowHelper;
using static ARMeilleure.IntermediateRepresentation.OperandHelper;
using System.Diagnostics;

namespace ARMeilleure.Instructions
{
    //TODO: SSE2 path
    static partial class InstEmit32
    {
        public static void Vabs_S(ArmEmitterContext context)
        {
            EmitScalarUnaryOpF32(context, (op1) => EmitUnaryMathCall(context, MathF.Abs, Math.Abs, op1));
        }

        public static void Vabs_V(ArmEmitterContext context)
        {
            OpCode32Simd op = (OpCode32Simd)context.CurrOp;
            if (op.F)
            {
                EmitVectorUnaryOpF32(context, (op1) => EmitUnaryMathCall(context, MathF.Abs, Math.Abs, op1));
            } 
            else
            {
                EmitVectorUnaryOpSx32(context, (op1) => EmitAbs(context, op1));
            }
        }

        private static Operand EmitAbs(ArmEmitterContext context, Operand value)
        {
            Operand isPositive = context.ICompareGreaterOrEqual(value, Const(value.Type, 0));

            return context.ConditionalSelect(isPositive, value, context.Negate(value));
        }

        public static void Vadd_S(ArmEmitterContext context)
        {
            EmitScalarBinaryOpF32(context, (op1, op2) => context.Add(op1, op2));
        }

        public static void Vadd_V(ArmEmitterContext context)
        {
            EmitVectorBinaryOpF32(context, (op1, op2) => context.Add(op1, op2));
        }

        public static void Vadd_I(ArmEmitterContext context)
        {
            EmitVectorBinaryOpZx32(context, (op1, op2) => context.Add(op1, op2));
        }

        public static void Vand_I(ArmEmitterContext context)
        {
            EmitVectorBinaryOpZx32(context, (op1, op2) => context.BitwiseAnd(op1, op2));
        }

        public static void Vdup(ArmEmitterContext context)
        {
            OpCode32SimdVdupGP op = (OpCode32SimdVdupGP)context.CurrOp;

            Operand insert = GetIntA32(context, op.Rt);

            // zero extend into an I64, then replicate. Saves the most time over elementwise inserts
            switch (op.Size)
            {
                case 2:
                    insert = context.Multiply(context.ZeroExtend32(OperandType.I64, insert), Const(0x0000000100000001u));
                    break;
                case 1:
                    insert = context.Multiply(context.ZeroExtend16(OperandType.I64, insert), Const(0x0001000100010001u));
                    break;
                case 0:
                    insert = context.Multiply(context.ZeroExtend8(OperandType.I64, insert), Const(0x0101010101010101u));
                    break;
                default:
                    throw new Exception("Unknown Vdup Size!");
            }

            InsertScalar(context, op.Vd, insert);
            if (op.Q)
            {
                InsertScalar(context, op.Vd + 1, insert);
            }
        }

        public static void Vdup_1(ArmEmitterContext context)
        {
            OpCode32SimdDupElem op = (OpCode32SimdDupElem)context.CurrOp;

            Operand insert = EmitVectorExtractZx32(context, op.Vm >> 1, ((op.Vm & 1) << (3 - op.Size)) + op.Index, op.Size);

            // zero extend into an I64, then replicate. Saves the most time over elementwise inserts
            switch (op.Size)
            {
                case 2:
                    insert = context.Multiply(context.ZeroExtend32(OperandType.I64, insert), Const(0x0000000100000001u));
                    break;
                case 1:
                    insert = context.Multiply(context.ZeroExtend16(OperandType.I64, insert), Const(0x0001000100010001u));
                    break;
                case 0:
                    insert = context.Multiply(context.ZeroExtend8(OperandType.I64, insert), Const(0x0101010101010101u));
                    break;
                default:
                    throw new Exception("Unknown Vdup Size!");
            }

            InsertScalar(context, op.Vd, insert);
            if (op.Q)
            {
                InsertScalar(context, op.Vd | 1, insert);
            }
        }

        public static void Vext(ArmEmitterContext context)
        {
            OpCode32SimdVext op = (OpCode32SimdVext)context.CurrOp;

            int elems = op.GetBytesCount();
            int byteOff = op.Immediate;

            (int vn, int en) = GetQuadwordAndSubindex(op.Vn, op.RegisterSize);
            (int vm, int em) = GetQuadwordAndSubindex(op.Vm, op.RegisterSize);
            (int vd, int ed) = GetQuadwordAndSubindex(op.Vd, op.RegisterSize);

            Operand res = GetVecA32(vd);

            for (int index = 0; index < elems; index++)
            {
                Operand extract;

                if (byteOff >= elems)
                {
                    extract = EmitVectorExtractZx32(context, vm, (byteOff - elems) + em * elems, op.Size);
                }
                else
                {
                    extract = EmitVectorExtractZx32(context, vn, byteOff + en * elems, op.Size);
                }
                byteOff++;

                res = EmitVectorInsert(context, res, extract, index + ed * elems, op.Size);
            }

            context.Copy(GetVecA32(vd), res);
        }

        public static void Vorr_I(ArmEmitterContext context)
        {
            EmitVectorBinaryOpZx32(context, (op1, op2) => context.BitwiseOr(op1, op2));
        }

        public static void Vbsl(ArmEmitterContext context)
        {
            EmitVectorTernaryOpZx32(context, (op1, op2, op3) =>
            {
                return context.BitwiseExclusiveOr(
                    context.BitwiseAnd(op1,
                    context.BitwiseExclusiveOr(op2, op3)), op3);
            });
        }

        public static void Vbif(ArmEmitterContext context)
        {
            EmitBifBit(context, true);
        }

        public static void Vbit(ArmEmitterContext context)
        {
            EmitBifBit(context, false);
        }

        private static void EmitBifBit(ArmEmitterContext context, bool notRm)
        {
            OpCode32SimdReg op = (OpCode32SimdReg)context.CurrOp;

            /*
            if (Optimizations.UseSse2)
            {
                Operand d = GetVec(op.Rd);
                Operand n = GetVec(op.Rn);
                Operand m = GetVec(op.Rm);

                Operand res = context.AddIntrinsic(Intrinsic.X86Pxor, n, d);

                if (notRm)
                {
                    res = context.AddIntrinsic(Intrinsic.X86Pandn, m, res);
                }
                else
                {
                    res = context.AddIntrinsic(Intrinsic.X86Pand, m, res);
                }

                res = context.AddIntrinsic(Intrinsic.X86Pxor, d, res);

                if (op.RegisterSize == RegisterSize.Simd64)
                {
                    res = context.VectorZeroUpper64(res);
                }

                context.Copy(d, res);
            }
            */

            EmitVectorTernaryOpZx32(context, (d, n, m) =>
            {
                if (notRm)
                {
                    m = context.BitwiseNot(m);
                }
                return context.BitwiseExclusiveOr(
                    context.BitwiseAnd(m,
                    context.BitwiseExclusiveOr(d, n)), d);
            });
        }

        public static void Vmov_S(ArmEmitterContext context)
        {
            EmitScalarUnaryOpF32(context, (op1) => op1);
        }

        public static void Vneg_S(ArmEmitterContext context)
        {
            EmitScalarUnaryOpF32(context, (op1) => context.Negate(op1));
        }

        public static void Vnmul_S(ArmEmitterContext context)
        {
            EmitScalarBinaryOpF32(context, (op1, op2) => context.Negate(context.Multiply(op1, op2)));
        }

        public static void Vnmla_S(ArmEmitterContext context)
        {
            if (Optimizations.FastFP)
            {
                EmitScalarTernaryOpF32(context, (op1, op2, op3) =>
                {
                    return context.Negate(context.Add(op1, context.Multiply(op2, op3)));
                });
            }
            else
            {
                EmitScalarTernaryOpF32(context, (op1, op2, op3) =>
                {
                    return EmitSoftFloatCall(context, SoftFloat32.FPNegMulAdd, SoftFloat64.FPNegMulAdd, op1, op2, op3);
                });
            }
        }

        public static void Vnmls_S(ArmEmitterContext context)
        {
            if (Optimizations.FastFP)
            {
                EmitScalarTernaryOpF32(context, (op1, op2, op3) =>
                {
                    return context.Add(context.Negate(op1), context.Multiply(op2, op3));
                });
            }
            else
            {
                EmitScalarTernaryOpF32(context, (op1, op2, op3) =>
                {
                    return EmitSoftFloatCall(context, SoftFloat32.FPNegMulSub, SoftFloat64.FPNegMulSub, op1, op2, op3);
                });
            }
        }

        public static void Vneg_V(ArmEmitterContext context)
        {
            if ((context.CurrOp as OpCode32Simd).F)
            {
                EmitVectorUnaryOpF32(context, (op1) => context.Negate(op1));
            } 
            else
            {
                EmitVectorUnaryOpSx32(context, (op1) => context.Negate(op1));
            }
        }

        public static void Vdiv_S(ArmEmitterContext context)
        {
            if (Optimizations.FastFP)
            {
                EmitScalarBinaryOpF32(context, (op1, op2) => context.Divide(op1, op2));
            }
            else
            {
                EmitScalarBinaryOpF32(context, (op1, op2) =>
                {
                    return EmitSoftFloatCall(context, SoftFloat32.FPDiv, SoftFloat64.FPDiv, op1, op2);
                });
            }
        }

        public static void Vdiv_V(ArmEmitterContext context)
        {
            if (Optimizations.FastFP)
            {
                EmitVectorBinaryOpF32(context, (op1, op2) => context.Divide(op1, op2));
            }
            else
            {
                EmitVectorBinaryOpF32(context, (op1, op2) =>
                {
                    return EmitSoftFloatCall(context, SoftFloat32.FPDiv, SoftFloat64.FPDiv, op1, op2);
                });
            }
        }

        public static void Vmax_V(ArmEmitterContext context)
        {
            EmitVectorBinaryOpF32(context, (op1, op2) =>
            {
                return EmitSoftFloatCall(context, SoftFloat32.FPMax, SoftFloat64.FPMax, op1, op2);
            });
        }

        public static void Vmax_I(ArmEmitterContext context)
        {
            OpCode32SimdReg op = (OpCode32SimdReg)context.CurrOp;
            if (op.U)
            {
                EmitVectorBinaryOpZx32(context, (op1, op2) => context.ConditionalSelect(context.ICompareGreaterUI(op1, op2), op1, op2));
            } 
            else
            {
                EmitVectorBinaryOpSx32(context, (op1, op2) => context.ConditionalSelect(context.ICompareGreater(op1, op2), op1, op2));
            }
        }

        public static void Vmin_V(ArmEmitterContext context)
        {
            EmitVectorBinaryOpF32(context, (op1, op2) =>
            {
                return EmitSoftFloatCall(context, SoftFloat32.FPMin, SoftFloat64.FPMin, op1, op2);
            });
        }

        public static void Vmin_I(ArmEmitterContext context)
        {
            OpCode32SimdReg op = (OpCode32SimdReg)context.CurrOp;
            if (op.U)
            {
                EmitVectorBinaryOpZx32(context, (op1, op2) => context.ConditionalSelect(context.ICompareLessUI(op1, op2), op1, op2));
            }
            else
            {
                EmitVectorBinaryOpSx32(context, (op1, op2) => context.ConditionalSelect(context.ICompareLess(op1, op2), op1, op2));
            }
        }

        public static void VmaxminNm_S(ArmEmitterContext context)
        {
            bool max = (context.CurrOp.RawOpCode & (1 << 6)) == 0;
            _F32_F32_F32 f32 = max ? new _F32_F32_F32(SoftFloat32.FPMaxNum) : new _F32_F32_F32(SoftFloat32.FPMinNum);
            _F64_F64_F64 f64 = max ? new _F64_F64_F64(SoftFloat64.FPMaxNum) : new _F64_F64_F64(SoftFloat64.FPMinNum);

            EmitScalarBinaryOpF32(context, (op1, op2) => EmitSoftFloatCall(context, f32, f64, op1, op2));
        }

        public static void VmaxminNm_V(ArmEmitterContext context)
        {
            bool max = (context.CurrOp.RawOpCode & (1 << 21)) == 0;
            _F32_F32_F32 f32 = max ? new _F32_F32_F32(SoftFloat32.FPMaxNum) : new _F32_F32_F32(SoftFloat32.FPMinNum);
            _F64_F64_F64 f64 = max ? new _F64_F64_F64(SoftFloat64.FPMaxNum) : new _F64_F64_F64(SoftFloat64.FPMinNum);

            EmitVectorBinaryOpSx32(context, (op1, op2) => EmitSoftFloatCall(context, f32, f64, op1, op2));
        }

        public static void Vmul_S(ArmEmitterContext context)
        {
            if (Optimizations.FastFP)
            {
                EmitScalarBinaryOpF32(context, (op1, op2) => context.Multiply(op1, op2));
            }
            else
            {
                EmitScalarBinaryOpF32(context, (op1, op2) =>
                {
                    return EmitSoftFloatCall(context, SoftFloat32.FPMul, SoftFloat64.FPMul, op1, op2);
                });
            }
        }

        public static void Vmul_V(ArmEmitterContext context)
        {
            if (Optimizations.FastFP)
            {
                EmitVectorBinaryOpF32(context, (op1, op2) => context.Multiply(op1, op2));
            }
            else
            {
                EmitVectorBinaryOpF32(context, (op1, op2) =>
                {
                    return EmitSoftFloatCall(context, SoftFloat32.FPMul, SoftFloat64.FPMul, op1, op2);
                });
            }
        }

        public static void Vmul_I(ArmEmitterContext context)
        {
            if (((context.CurrOp.RawOpCode >> 24) & 1) != 0) throw new Exception("Polynomial mode not supported");
            EmitVectorBinaryOpSx32(context, (op1, op2) => context.Multiply(op1, op2));
        }

        public static void Vmul_1(ArmEmitterContext context)
        {
            OpCode32SimdRegElem op = (OpCode32SimdRegElem)context.CurrOp;
            if (op.F)
            {
                if (Optimizations.FastFP)
                {
                    EmitVectorByScalarOpF32(context, (op1, op2) => context.Multiply(op1, op2));
                }
                else
                {
                    EmitVectorByScalarOpF32(context, (op1, op2) => EmitSoftFloatCall(context, SoftFloat32.FPMul, SoftFloat64.FPMul, op1, op2));
                }
            } 
            else
            {
                EmitVectorByScalarOpI32(context, (op1, op2) => context.Multiply(op1, op2), false);
            }
        }

        public static void Vmla_S(ArmEmitterContext context)
        {
            if (Optimizations.FastFP)
            {
                EmitScalarTernaryOpF32(context, (op1, op2, op3) =>
                {
                    return context.Add(op1, context.Multiply(op2, op3));
                });
            }
            else
            {
                EmitScalarTernaryOpF32(context, (op1, op2, op3) =>
                {
                    return EmitSoftFloatCall(context, SoftFloat32.FPMulAdd, SoftFloat64.FPMulAdd, op1, op2, op3);
                });
            }
        }

        public static void Vmla_V(ArmEmitterContext context)
        {
            if (Optimizations.FastFP)
            {
                EmitVectorTernaryOpF32(context, (op1, op2, op3) => context.Add(op1, context.Multiply(op2, op3)));
            }
            else
            {
                EmitVectorTernaryOpF32(context, (op1, op2, op3) =>
                {
                    return EmitSoftFloatCall(context, SoftFloat32.FPMulAdd, SoftFloat64.FPMulAdd, op1, op2, op3);
                });
            }
        }

        public static void Vmla_I(ArmEmitterContext context)
        {
            EmitVectorTernaryOpZx32(context, (op1, op2, op3) => context.Add(op1, context.Multiply(op2, op3)));
        }

        public static void Vmla_1(ArmEmitterContext context)
        {
            OpCode32SimdRegElem op = (OpCode32SimdRegElem)context.CurrOp;
            if (op.F)
            {
                if (Optimizations.FastFP)
                {
                    EmitVectorsByScalarOpF32(context, (op1, op2, op3) => context.Add(op1, context.Multiply(op2, op3)));
                }
                else
                {
                    EmitVectorsByScalarOpF32(context, (op1, op2, op3) => EmitSoftFloatCall(context, SoftFloat32.FPMulAdd, SoftFloat64.FPMulAdd, op1, op2, op3));
                }
            }
            else
            {
                EmitVectorsByScalarOpI32(context, (op1, op2, op3) => context.Add(op1, context.Multiply(op2, op3)), false);
            }
        }

        public static void Vmls_S(ArmEmitterContext context)
        {
            if (Optimizations.FastFP)
            {
                EmitScalarTernaryOpF32(context, (op1, op2, op3) =>
                {
                    return context.Subtract(op1, context.Multiply(op2, op3));
                });
            }
            else
            {
                EmitScalarTernaryOpF32(context, (op1, op2, op3) =>
                {
                    return EmitSoftFloatCall(context, SoftFloat32.FPMulSub, SoftFloat64.FPMulSub, op1, op2, op3);
                });
            }
        }

        public static void Vmls_V(ArmEmitterContext context)
        {
            if (Optimizations.FastFP)
            {
                EmitVectorTernaryOpF32(context, (op1, op2, op3) => context.Subtract(op1, context.Multiply(op2, op3)));
            }
            else
            {
                EmitVectorTernaryOpF32(context, (op1, op2, op3) =>
                {
                    return EmitSoftFloatCall(context, SoftFloat32.FPMulSub, SoftFloat64.FPMulSub, op1, op2, op3);
                });
            }
        }

        public static void Vmls_I(ArmEmitterContext context)
        {
            EmitVectorTernaryOpZx32(context, (op1, op2, op3) => context.Subtract(op1, context.Multiply(op2, op3)));
        }

        public static void Vmls_1(ArmEmitterContext context)
        {
            OpCode32SimdRegElem op = (OpCode32SimdRegElem)context.CurrOp;
            if (op.F)
            {
                if (Optimizations.FastFP)
                {
                    EmitVectorsByScalarOpF32(context, (op1, op2, op3) => context.Subtract(op1, context.Multiply(op2, op3)));
                }
                else
                {
                    EmitVectorsByScalarOpF32(context, (op1, op2, op3) => EmitSoftFloatCall(context, SoftFloat32.FPMulSub, SoftFloat64.FPMulSub, op1, op2, op3));
                }
            }
            else
            {
                EmitVectorsByScalarOpI32(context, (op1, op2, op3) => context.Subtract(op1, context.Multiply(op2, op3)), false);
            }
        }

        public static void Vpadd_V(ArmEmitterContext context)
        {
            EmitVectorPairwiseOpF32(context, (op1, op2) => context.Add(op1, op2));
        }

        public static void Vpadd_I(ArmEmitterContext context)
        {
            OpCode32SimdReg op = (OpCode32SimdReg)context.CurrOp;
            EmitVectorPairwiseOpI32(context, (op1, op2) => context.Add(op1, op2), !op.U);
        }

        public static void Vrev(ArmEmitterContext context)
        {
            OpCode32Simd op = (OpCode32Simd)context.CurrOp;
            EmitVectorUnaryOpZx32(context, (op1) =>
            {
                switch (op.Opc)
                {
                    case 0:
                        switch (op.Size) //swap bytes
                        {
                            default:
                                return op1;
                            case 1:
                                return InstEmit.EmitReverseBytes16_32Op(context, op1);
                            case 2:
                            case 3:
                                return context.ByteSwap(op1);
                        }
                    case 1:
                        switch (op.Size)
                        {
                            default:
                                return op1;
                            case 2:
                                return context.BitwiseOr(context.ShiftRightUI(context.BitwiseAnd(op1, Const(0xffff0000)), Const(16)),
                                                         context.ShiftLeft(context.BitwiseAnd(op1, Const(0x0000ffff)), Const(16)));
                            case 3:
                                return context.BitwiseOr(
                                context.BitwiseOr(context.ShiftRightUI(context.BitwiseAnd(op1, Const(0xffff000000000000ul)), Const(48)),
                                                     context.ShiftLeft(context.BitwiseAnd(op1, Const(0x000000000000fffful)), Const(48))),
                                context.BitwiseOr(context.ShiftRightUI(context.BitwiseAnd(op1, Const(0x0000ffff00000000ul)), Const(16)),
                                                     context.ShiftLeft(context.BitwiseAnd(op1, Const(0x00000000ffff0000ul)), Const(16)))
                                );
                        }
                    case 2:
                        //swap upper and lower
                        return context.BitwiseOr(context.ShiftRightUI(context.BitwiseAnd(op1, Const(0xffffffff00000000ul)), Const(32)),
                                                 context.ShiftLeft(context.BitwiseAnd(op1, Const(0x00000000fffffffful)), Const(32)));

                }
                return op1;
            });
        }



        public static void Vrecpe(ArmEmitterContext context)
        {
            EmitVectorUnaryOpF32(context, (op1) =>
            {
                return EmitSoftFloatCall(context, SoftFloat32.FPRecipEstimate, SoftFloat64.FPRecipEstimate, op1);
            });
        }

        public static void Vrecps(ArmEmitterContext context)
        {
            EmitVectorBinaryOpF32(context, (op1, op2) =>
            {
                return EmitSoftFloatCall(context, SoftFloat32.FPRecipStepFused, SoftFloat64.FPRecipStepFused, op1, op2);
            });
        }

        public static void Vrsqrte(ArmEmitterContext context)
        {
            EmitVectorUnaryOpF32(context, (op1) =>
            {
                return EmitSoftFloatCall(context, SoftFloat32.FPRSqrtEstimate, SoftFloat64.FPRSqrtEstimate, op1);
            });
        }

        public static void Vrsqrts(ArmEmitterContext context)
        {
            EmitVectorBinaryOpF32(context, (op1, op2) =>
            {
                return EmitSoftFloatCall(context, SoftFloat32.FPRSqrtStepFused, SoftFloat64.FPRSqrtStepFused, op1, op2);
            });
        }

        public static void Vsel(ArmEmitterContext context)
        {
            var op = (OpCode32SimdSel)context.CurrOp;
            Operand condition = null;
            switch (op.Cc)
            {
                case OpCode32SimdSelMode.Eq:
                    condition = GetCondTrue(context, Condition.Eq);
                    break;
                case OpCode32SimdSelMode.Ge:
                    condition = GetCondTrue(context, Condition.Ge);
                    break;
                case OpCode32SimdSelMode.Gt:
                    condition = GetCondTrue(context, Condition.Gt);
                    break;
                case OpCode32SimdSelMode.Vs:
                    condition = GetCondTrue(context, Condition.Vs);
                    break;
            }
            EmitScalarBinaryOpI32(context, (op1, op2) =>
            {
                return context.ConditionalSelect(condition, op1, op2);
            });
        }

        public static void Vshl_I(ArmEmitterContext context)
        {
            OpCode32SimdReg op = (OpCode32SimdReg)context.CurrOp;
            //IMPORTANT TODO: does shift left negative do a truncating shift right on x86?
            if (op.U)
            {
                EmitVectorBinaryOpZx32(context, (op1, op2) => EmitShlRegOp(context, op2, op1, op.Size, true));
            } 
            else
            {
                EmitVectorBinaryOpSx32(context, (op1, op2) => EmitShlRegOp(context, op2, op1, op.Size, false));
            }
        }

        private static Operand EmitShlRegOp(ArmEmitterContext context, Operand op, Operand shiftLsB, int size, bool unsigned)
        {
            if (shiftLsB.Type == OperandType.I64) shiftLsB = context.ConvertI64ToI32(shiftLsB);
            shiftLsB = context.SignExtend8(OperandType.I32, shiftLsB);
            Debug.Assert((uint)size < 4u);

            Operand negShiftLsB = context.Negate(shiftLsB);

            Operand isPositive = context.ICompareGreaterOrEqual(shiftLsB, Const(0));

            Operand shl = context.ShiftLeft(op, shiftLsB);
            Operand shr = (unsigned) ? context.ShiftRightUI(op, negShiftLsB) : context.ShiftRightSI(op, negShiftLsB);

            Operand res = context.ConditionalSelect(isPositive, shl, shr);

            if (unsigned)
            {
                Operand isOutOfRange = context.BitwiseOr(
                    context.ICompareGreaterOrEqual(shiftLsB, Const(8 << size)),
                    context.ICompareGreaterOrEqual(negShiftLsB, Const(8 << size)));

                return context.ConditionalSelect(isOutOfRange, Const(op.Type, 0), res);
            } 
            else
            {
                Operand isOutOfRange0 = context.ICompareGreaterOrEqual(shiftLsB, Const(8 << size));
                Operand isOutOfRangeN = context.ICompareGreaterOrEqual(negShiftLsB, Const(8 << size));

                //also zero if shift is too negative, but value was positive
                isOutOfRange0 = context.BitwiseOr(isOutOfRange0, context.BitwiseAnd(isOutOfRangeN, context.ICompareGreaterOrEqual(op, Const(0))));

                Operand min = (op.Type == OperandType.I64) ? Const(-1L) : Const(-1);

                return context.ConditionalSelect(isOutOfRange0, Const(op.Type, 0), context.ConditionalSelect(isOutOfRangeN, min, res));
            }
        }

        public static void Vshl(ArmEmitterContext context)
        {
            OpCode32SimdShift op = (OpCode32SimdShift)context.CurrOp;
            EmitVectorUnaryOpZx32(context, (op1) => context.ShiftLeft(op1, Const(op1.Type, op.Shift)));
        }

        public static void Vsqrt_S(ArmEmitterContext context)
        {
            /*
            if (Optimizations.FastFP && Optimizations.UseSse && sizeF == 0)
            {
                EmitScalarUnaryOpF(context, Intrinsic.X86Rsqrtss, 0);
            } */

            EmitScalarUnaryOpF32(context, (op1) =>
            {
                return EmitSoftFloatCall(context, SoftFloat32.FPSqrt, SoftFloat64.FPSqrt, op1);
            });
        }

        public static void Vsub_S(ArmEmitterContext context)
        {
            EmitScalarBinaryOpF32(context, (op1, op2) => context.Subtract(op1, op2));
        }

        public static void Vsub_V(ArmEmitterContext context)
        {
            EmitVectorBinaryOpF32(context, (op1, op2) => context.Subtract(op1, op2));
        }

        public static void Vsub_I(ArmEmitterContext context)
        {
            EmitVectorBinaryOpZx32(context, (op1, op2) => context.Subtract(op1, op2));
        }
    }
}
