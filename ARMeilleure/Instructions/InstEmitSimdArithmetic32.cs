using ARMeilleure.Decoders;
using ARMeilleure.Translation;
using System;
using System.Collections.Generic;
using System.Text;

using static ARMeilleure.Instructions.InstEmitHelper;
using static ARMeilleure.Instructions.InstEmitSimdHelper;
using static ARMeilleure.Instructions.InstEmitSimdHelper32;
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

        public static void Vmov_S(ArmEmitterContext context)
        {
            EmitScalarUnaryOpF32(context, (op1) => op1);
        }

        public static void Vneg_S(ArmEmitterContext context)
        {
            EmitScalarUnaryOpF32(context, (op1) => context.Negate(op1));
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
            EmitVectorBinaryOpZx32(context, (op1, op2) => context.Multiply(op1, op2));
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
