using ARMeilleure.Decoders;
using ARMeilleure.IntermediateRepresentation;
using ARMeilleure.State;
using ARMeilleure.Translation;
using System;

using static ARMeilleure.Instructions.InstEmitHelper;
using static ARMeilleure.Instructions.InstEmitMemoryHelper;
using static ARMeilleure.IntermediateRepresentation.OperandHelper;

namespace ARMeilleure.Instructions
{
    static partial class InstEmit32
    {

        public static void Vldm(ArmEmitterContext context)
        {
            OpCode32SimdMemMult op = (OpCode32SimdMemMult)context.CurrOp;

            Operand n = GetIntA32(context, op.Rn);

            Operand baseAddress = context.Add(n, Const(op.Offset));

            bool writesToPc = (op.RegisterRange & (1 << RegisterAlias.Aarch32Pc)) != 0;

            bool writeBack = op.PostOffset != 0 && (op.Rn != RegisterAlias.Aarch32Pc || !writesToPc);

            if (writeBack)
            {
                SetIntA32(context, op.Rn, context.Add(n, Const(op.PostOffset)));
            }

            int range = op.RegisterRange;
            int sReg = (op.DoubleWidth) ? (op.Vd << 1) : op.Vd;
            int offset = 0;
            int size = (op.DoubleWidth) ? DWordSizeLog2 : WordSizeLog2;
            int byteSize = 4;
            
            for (int num = 0; num < range; num++, sReg++)
            {
                Operand address = context.Add(baseAddress, Const(offset));
                Operand vec = GetVecA32(sReg >> 2);

                EmitLoadSimd(context, address, vec, sReg >> 2, sReg & 3, 2);
                offset += byteSize;
            }
        }

        public static void Vstm(ArmEmitterContext context)
        {
            OpCode32SimdMemMult op = (OpCode32SimdMemMult)context.CurrOp;

            Operand n = GetIntA32(context, op.Rn);

            Operand baseAddress = context.Add(n, Const(op.Offset));

            bool writeBack = op.PostOffset != 0;

            if (writeBack)
            {
                SetIntA32(context, op.Rn, context.Add(n, Const(op.PostOffset)));
            }

            int offset = 0;

            int range = op.RegisterRange;
            int sReg = (op.DoubleWidth) ? (op.Vd << 1) : op.Vd;
            int size = (op.DoubleWidth) ? DWordSizeLog2 : WordSizeLog2;
            int byteSize = 4;

            for (int num = 0; num < range; num++, sReg++)
            {
                Operand address = context.Add(baseAddress, Const(offset));

                EmitStoreSimd(context, address, sReg >> 2, sReg & 3, 2);

                offset += byteSize;
            }
        }
    }
}
