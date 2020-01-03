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
    }
}
