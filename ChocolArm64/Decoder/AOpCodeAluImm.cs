using ChocolArm64.Instruction;
using System;

namespace ChocolArm64.Decoder
{
    class AOpCodeAluImm : AOpCodeAlu, IaOpCodeAluImm
    {
        public long Imm { get; private set; }

        public AOpCodeAluImm(AInst inst, long position, int opCode) : base(inst, position, opCode)
        {
            if (DataOp == ADataOp.Arithmetic)
            {
                Imm = (opCode >> 10) & 0xfff;

                int shift = (opCode >> 22) & 3;

                Imm <<= shift * 12;
            }
            else if (DataOp == ADataOp.Logical)
            {
                var bm = ADecoderHelper.DecodeBitMask(opCode, true);

                if (bm.IsUndefined)
                {
                    Emitter = AInstEmit.Und;

                    return;
                }

                Imm = bm.WMask;
            }
            else
            {
                throw new ArgumentException(nameof(opCode));
            }
        }
    }
}