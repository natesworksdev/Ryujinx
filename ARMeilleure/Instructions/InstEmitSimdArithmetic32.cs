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

namespace ARMeilleure.Instructions
{
    //TODO: SSE2 path
    static partial class InstEmit32
    {
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
                InsertScalar(context, op.Vd | 1, insert);
            }
        }

        public static void Vorr_I(ArmEmitterContext context)
        {
            EmitVectorBinaryOpZx32(context, (op1, op2) => context.BitwiseOr(op1, op2));
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
            if (false) //Optimizations.FastFP)
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
            if (false)//Optimizations.FastFP)
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

        //TODO: probably important to have a fast path for these instead of calling fucking standard math min/max
        public static void VmaxminNm_S(ArmEmitterContext context)
        {
            bool max = (context.CurrOp.RawOpCode & (1 << 6)) == 0;
            Delegate dlg = max ? new _F32_F32_F32(Math.Max) : new _F32_F32_F32(Math.Min);

            EmitScalarBinaryOpF32(context, (op1, op2) => context.Call(dlg, op1, op2));
        }

        public static void VmaxminNm_V(ArmEmitterContext context)
        {
            bool max = (context.CurrOp.RawOpCode & (1 << 21)) == 0;
            Delegate dlg = max ? new _F32_F32_F32(Math.Max) : new _F32_F32_F32(Math.Min);

            EmitVectorBinaryOpSx32(context, (op1, op2) => context.Call(dlg, op1, op2));
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
            EmitVectorBinaryOpSx32(context, (op1, op2) => context.Multiply(op1, op2));
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
            if (false)//Optimizations.FastFP)
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
            if (false)//Optimizations.FastFP)
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

        public static void Vsel(ArmEmitterContext context)
        {
            var op = (OpCode32SimdSel)context.CurrOp;
            EmitScalarBinaryOpI32(context, (op1, op2) =>
            {
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
                return context.ConditionalSelect(condition, op1, op2);
            });
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
                return EmitSoftFloatCall(context, SoftFloat32.FPRSqrtEstimate, SoftFloat64.FPRSqrtEstimate, op1);
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
