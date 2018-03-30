using System;

namespace Ryujinx.Graphics.Gal.Shader
{
    static class ShaderOpCodeTable
    {
        private const int EncodingBits = 14;

        private static ShaderDecodeFunc[] OpCodes;

        static ShaderOpCodeTable()
        {
            OpCodes = new ShaderDecodeFunc[1 << EncodingBits];

#region Instructions
            Set("111000110000xx", ShaderDecode.Exit);
            Set("0101110001011x", ShaderDecode.Fadd_R);
            Set("0100110001011x", ShaderDecode.Fadd_C);
            Set("0011100x01011x", ShaderDecode.Fadd_Imm);
            Set("010110011xxxxx", ShaderDecode.Ffma_RR);
            Set("010100011xxxxx", ShaderDecode.Ffma_RC);
            Set("010010011xxxxx", ShaderDecode.Ffma_CR);
            Set("001100101xxxxx", ShaderDecode.Ffma_Imm);
            Set("0101110001101x", ShaderDecode.Fmul_R);
            Set("0100110001101x", ShaderDecode.Fmul_C);
            Set("0011100x01101x", ShaderDecode.Fmul_Imm);
            Set("010010111011xx", ShaderDecode.Fsetp_C);
            Set("11100000xxxxxx", ShaderDecode.Ipa);
            Set("111000110011xx", ShaderDecode.Kil);
            Set("1110111111011x", ShaderDecode.Ld_A);
            Set("0101000010000x", ShaderDecode.Mufu);
            Set("1110111111110x", ShaderDecode.St_A);
            Set("1101100xxxxxxx", ShaderDecode.Texs);
#endregion
        }

        private static void Set(string Encoding, ShaderDecodeFunc Func)
        {
            if (Encoding.Length != EncodingBits)
            {
                throw new ArgumentException(nameof(Encoding));
            }

            int Bit   = Encoding.Length - 1;
            int Value = 0;
            int XMask = 0;
            int XBits = 0;

            int[] XPos = new int[Encoding.Length];

            for (int Index = 0; Index < Encoding.Length; Index++, Bit--)
            {
                char Chr = Encoding[Index];

                if (Chr == '1')
                {
                    Value |= 1 << Bit;
                }
                else if (Chr == 'x')
                {
                    XMask |= 1 << Bit;

                    XPos[XBits++] = Bit;
                }
            }

            XMask = ~XMask;

            for (int Index = 0; Index < (1 << XBits); Index++)
            {
                Value &= XMask;

                for (int X = 0; X < XBits; X++)
                {
                    Value |= ((Index >> X) & 1) << XPos[X];
                }

                OpCodes[Value] = Func;
            }
        }

        public static ShaderDecodeFunc GetDecoder(long OpCode)
        {
            return OpCodes[(ulong)OpCode >> (64 - EncodingBits)];
        }
    }
}