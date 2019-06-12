using ARMeilleure.Decoders;
using ARMeilleure.IntermediateRepresentation;
using ARMeilleure.State;
using ARMeilleure.Translation;
using System;
using System.Reflection;

using static ARMeilleure.Instructions.InstEmitHelper;
using static ARMeilleure.Instructions.InstEmitMemoryHelper;
using static ARMeilleure.Instructions.InstEmitSimdHelper;
using static ARMeilleure.IntermediateRepresentation.OperandHelper;

namespace ARMeilleure.Instructions
{
    static partial class InstEmit
    {
        public static void Fmov_Si(EmitterContext context)
        {
            OpCodeSimdFmov op = (OpCodeSimdFmov)context.CurrOp;

            Operand imm;

            if (op.Size != 0)
            {
                imm = Const(op.Immediate);
            }
            else
            {
                imm = Const((int)op.Immediate);
            }

            context.Copy(GetVec(op.Rd), imm);
        }
    }
}