using ARMeilleure.Decoders;
using ARMeilleure.IntermediateRepresentation;
using ARMeilleure.Translation;
using System;
using System.Collections.Generic;

using static ARMeilleure.Instructions.InstEmitHelper;
using static ARMeilleure.Instructions.InstEmitSimdHelper;
using static ARMeilleure.Instructions.InstEmitSimdHelper32;
using static ARMeilleure.IntermediateRepresentation.OperandHelper;

namespace ARMeilleure.Instructions
{
    static partial class InstEmit32
    {
        public static void Vmov_I(ArmEmitterContext context)
        {
            EmitVectorImmUnaryOp32(context, (op1) => op1);
        }

        public static void Vmov_GS(ArmEmitterContext context)
        {
            OpCode32SimdMovGp op = (OpCode32SimdMovGp)context.CurrOp;
            Operand vec = GetVecA32(op.Vn >> 2);
            if (op.Op == 1)
            {
                // to general purpose
                Operand value = context.VectorExtract(OperandType.I32, vec, op.Vn & 0x3);
                SetIntA32(context, op.Rt, value);
            } 
            else
            {
                // from general purpose
                Operand value = GetIntA32(context, op.Rt);
                context.Copy(vec, context.VectorInsert(vec, value, op.Vn & 0x3));
                
            }
        }

        public static void Vmov_G2(ArmEmitterContext context)
        {
            OpCode32SimdMovGpDouble op = (OpCode32SimdMovGpDouble)context.CurrOp;
            Operand vec = GetVecA32(op.Vm >> 1);
            if (op.Op == 1)
            {
                // to general purpose
                Operand lowValue = context.VectorExtract(OperandType.I32, vec, (op.Vm & 1) << 1);
                SetIntA32(context, op.Rt, lowValue);

                Operand highValue = context.VectorExtract(OperandType.I32, vec, ((op.Vm & 1) << 1) | 1);
                SetIntA32(context, op.Rt2, highValue);
            }
            else
            {
                // from general purpose
                Operand lowValue = GetIntA32(context, op.Rt);
                Operand resultVec = context.VectorInsert(vec, lowValue, (op.Vm & 1) << 1);

                Operand highValue = GetIntA32(context, op.Rt2);
                context.Copy(vec, context.VectorInsert(resultVec, highValue, ((op.Vm & 1) << 1) | 1));
            }
        }

        public static void Vmov_GD(ArmEmitterContext context)
        {
            OpCode32SimdMovGpDouble op = (OpCode32SimdMovGpDouble)context.CurrOp;
            Operand vec = GetVecA32(op.Vm >> 1);
            if (op.Op == 1)
            {
                // to general purpose
                Operand value = context.VectorExtract(OperandType.I64, vec, op.Vm & 1);
                SetIntA32(context, op.Rt, context.ConvertI64ToI32(value));
                SetIntA32(context, op.Rt2, context.ConvertI64ToI32(context.ShiftRightUI(value, Const(32))));
            }
            else
            {
                // from general purpose
                Operand lowValue = GetIntA32(context, op.Rt);
                Operand highValue = GetIntA32(context, op.Rt2);

                Operand value = context.BitwiseOr(
                    context.ZeroExtend32(OperandType.I64, lowValue),
                    context.ShiftLeft(context.ZeroExtend32(OperandType.I64, highValue), Const(32))
                    );

                context.Copy(vec, context.VectorInsert(vec, value, op.Vm & 1));
            }
        }
    }
}
