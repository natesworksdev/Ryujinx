using System;

namespace Ryujinx.Graphics.Shader.Decoders
{
    static class DecoderHelper
    {
        public static int DecodeS20Immediate(long opCode)
        {
            int imm = opCode.Extract(20, 19);

            bool negate = opCode.Extract(56);

            if (negate)
            {
                imm = -imm;
            }

            return imm;
        }

        public static float DecodeF20Immediate(long opCode)
        {
            int imm = opCode.Extract(20, 19);

            bool negate = opCode.Extract(56);

            imm <<= 12;

            if (negate)
            {
                imm |= 1 << 31;
            }

            return BitConverter.Int32BitsToSingle(imm);
        }
    }
}