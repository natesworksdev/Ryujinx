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
            Set("0100110001011x", ShaderDecode.Fadd);
            Set("010010011xxxxx", ShaderDecode.Ffma);
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