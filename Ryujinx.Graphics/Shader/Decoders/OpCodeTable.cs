using Ryujinx.Graphics.Shader.Instructions;
using System;

namespace Ryujinx.Graphics.Shader.Decoders
{
    static class OpCodeTable
    {
        private const int EncodingBits = 14;

        private class TableEntry
        {
            public InstEmitter Emitter { get; }

            public Type OpCodeType { get; }

            public int XBits { get; }

            public TableEntry(InstEmitter emitter, Type opCodeType, int xBits)
            {
                Emitter    = emitter;
                OpCodeType = opCodeType;
                XBits      = xBits;
            }
        }

        private static TableEntry[] _opCodes;

        static OpCodeTable()
        {
            _opCodes = new TableEntry[1 << EncodingBits];

#region Instructions
            Set("1110111111011x", InstEmit.Ald,    typeof(OpCodeAttribute));
            Set("1110111111110x", InstEmit.Ast,    typeof(OpCodeAttribute));
            Set("0100110000000x", InstEmit.Bfe,    typeof(OpCodeAluCbuf));
            Set("0011100x00000x", InstEmit.Bfe,    typeof(OpCodeAluImm));
            Set("0101110000000x", InstEmit.Bfe,    typeof(OpCodeAluReg));
            Set("111000100100xx", InstEmit.Bra,    typeof(OpCodeBranch));
            Set("111000110000xx", InstEmit.Exit,   typeof(OpCodeExit));
            Set("0100110010101x", InstEmit.F2F,    typeof(OpCodeFArithCbuf));
            Set("0011100x10101x", InstEmit.F2F,    typeof(OpCodeFArithImm));
            Set("0101110010101x", InstEmit.F2F,    typeof(OpCodeFArithReg));
            Set("0100110010110x", InstEmit.F2I,    typeof(OpCodeFArithCbuf));
            Set("0011100x10110x", InstEmit.F2I,    typeof(OpCodeFArithImm));
            Set("0101110010110x", InstEmit.F2I,    typeof(OpCodeFArithReg));
            Set("0100110001011x", InstEmit.Fadd,   typeof(OpCodeFArithCbuf));
            Set("0011100x01011x", InstEmit.Fadd,   typeof(OpCodeFArithImm));
            Set("000010xxxxxxxx", InstEmit.Fadd,   typeof(OpCodeFArithImm32));
            Set("0101110001011x", InstEmit.Fadd,   typeof(OpCodeFArithReg));
            Set("010010011xxxxx", InstEmit.Ffma,   typeof(OpCodeFArithCbuf));
            Set("0011001x1xxxxx", InstEmit.Ffma,   typeof(OpCodeFArithImm));
            Set("010100011xxxxx", InstEmit.Ffma,   typeof(OpCodeFArithRegCbuf));
            Set("010110011xxxxx", InstEmit.Ffma,   typeof(OpCodeFArithReg));
            Set("0100110001100x", InstEmit.Fmnmx,  typeof(OpCodeFArithCbuf));
            Set("0011100x01100x", InstEmit.Fmnmx,  typeof(OpCodeFArithImm));
            Set("0101110001100x", InstEmit.Fmnmx,  typeof(OpCodeFArithReg));
            Set("0100110001101x", InstEmit.Fmul,   typeof(OpCodeFArithCbuf));
            Set("0011100x01101x", InstEmit.Fmul,   typeof(OpCodeFArithImm));
            Set("00011110xxxxxx", InstEmit.Fmul,   typeof(OpCodeFArithImm32));
            Set("0101110001101x", InstEmit.Fmul,   typeof(OpCodeFArithReg));
            Set("0100100xxxxxxx", InstEmit.Fset,   typeof(OpCodeSetCbuf));
            Set("0011000xxxxxxx", InstEmit.Fset,   typeof(OpCodeFsetImm));
            Set("01011000xxxxxx", InstEmit.Fset,   typeof(OpCodeSetReg));
            Set("010010111011xx", InstEmit.Fsetp,  typeof(OpCodeSetCbuf));
            Set("0011011x1011xx", InstEmit.Fsetp,  typeof(OpCodeFsetImm));
            Set("010110111011xx", InstEmit.Fsetp,  typeof(OpCodeSetReg));
            Set("0100110010111x", InstEmit.I2F,    typeof(OpCodeAluCbuf));
            Set("0011100x10111x", InstEmit.I2F,    typeof(OpCodeAluImm));
            Set("0101110010111x", InstEmit.I2F,    typeof(OpCodeAluReg));
            Set("0100110011100x", InstEmit.I2I,    typeof(OpCodeAluCbuf));
            Set("0011100x11100x", InstEmit.I2I,    typeof(OpCodeAluImm));
            Set("0101110011100x", InstEmit.I2I,    typeof(OpCodeAluReg));
            Set("0100110000010x", InstEmit.Iadd,   typeof(OpCodeAluCbuf));
            Set("0011100000010x", InstEmit.Iadd,   typeof(OpCodeAluImm));
            Set("0001110x0xxxxx", InstEmit.Iadd,   typeof(OpCodeAluImm32));
            Set("0101110000010x", InstEmit.Iadd,   typeof(OpCodeAluReg));
            Set("010011001100xx", InstEmit.Iadd3,  typeof(OpCodeAluCbuf));
            Set("001110001100xx", InstEmit.Iadd3,  typeof(OpCodeAluImm));
            Set("010111001100xx", InstEmit.Iadd3,  typeof(OpCodeAluReg));
            Set("0100110000100x", InstEmit.Imnmx,  typeof(OpCodeAluCbuf));
            Set("0011100x00100x", InstEmit.Imnmx,  typeof(OpCodeAluImm));
            Set("0101110000100x", InstEmit.Imnmx,  typeof(OpCodeAluReg));
            Set("11100000xxxxxx", InstEmit.Ipa,    typeof(OpCodeIpa));
            Set("0100110000011x", InstEmit.Iscadd, typeof(OpCodeAluCbuf));
            Set("0011100x00011x", InstEmit.Iscadd, typeof(OpCodeAluImm));
            Set("000101xxxxxxxx", InstEmit.Iscadd, typeof(OpCodeAluImm32));
            Set("0101110000011x", InstEmit.Iscadd, typeof(OpCodeAluReg));
            Set("010010110101xx", InstEmit.Iset,   typeof(OpCodeSetCbuf));
            Set("001101100101xx", InstEmit.Iset,   typeof(OpCodeSetImm));
            Set("010110110101xx", InstEmit.Iset,   typeof(OpCodeSetReg));
            Set("010010110110xx", InstEmit.Isetp,  typeof(OpCodeSetCbuf));
            Set("0011011x0110xx", InstEmit.Isetp,  typeof(OpCodeSetImm));
            Set("010110110110xx", InstEmit.Isetp,  typeof(OpCodeSetReg));
            Set("111000110011xx", InstEmit.Kil,    typeof(OpCodeExit));
            Set("1110111110010x", InstEmit.Ldc,    typeof(OpCodeLdc));
            Set("0100110001000x", InstEmit.Lop,    typeof(OpCodeLopCbuf));
            Set("0011100001000x", InstEmit.Lop,    typeof(OpCodeLopImm));
            Set("000001xxxxxxxx", InstEmit.Lop,    typeof(OpCodeLopImm32));
            Set("0101110001000x", InstEmit.Lop,    typeof(OpCodeLopReg));
            Set("0010000xxxxxxx", InstEmit.Lop3,   typeof(OpCodeLopCbuf));
            Set("001111xxxxxxxx", InstEmit.Lop3,   typeof(OpCodeLopImm));
            Set("0101101111100x", InstEmit.Lop3,   typeof(OpCodeLopReg));
            Set("0100110010011x", InstEmit.Mov,    typeof(OpCodeAluCbuf));
            Set("0011100x10011x", InstEmit.Mov,    typeof(OpCodeAluImm));
            Set("000000010000xx", InstEmit.Mov,    typeof(OpCodeAluImm32));
            Set("0101110010011x", InstEmit.Mov,    typeof(OpCodeAluReg));
            Set("0101000010000x", InstEmit.Mufu,   typeof(OpCodeFArith));
            Set("1111101111100x", InstEmit.Out,    typeof(OpCode));
            Set("0101000010010x", InstEmit.Psetp,  typeof(OpCodePsetp));
            Set("0100110010010x", InstEmit.Rro,    typeof(OpCodeFArithCbuf));
            Set("0011100x10010x", InstEmit.Rro,    typeof(OpCodeFArithImm));
            Set("0101110010010x", InstEmit.Rro,    typeof(OpCodeFArithReg));
            Set("0100110010100x", InstEmit.Sel,    typeof(OpCodeAluCbuf));
            Set("0011100010100x", InstEmit.Sel,    typeof(OpCodeAluImm));
            Set("0101110010100x", InstEmit.Sel,    typeof(OpCodeAluReg));
            Set("0100110001001x", InstEmit.Shl,    typeof(OpCodeAluCbuf));
            Set("0011100x01001x", InstEmit.Shl,    typeof(OpCodeAluImm));
            Set("0101110001001x", InstEmit.Shl,    typeof(OpCodeAluReg));
            Set("0100110000101x", InstEmit.Shr,    typeof(OpCodeAluCbuf));
            Set("0011100x00101x", InstEmit.Shr,    typeof(OpCodeAluImm));
            Set("0101110000101x", InstEmit.Shr,    typeof(OpCodeAluReg));
            Set("111000101001xx", InstEmit.Ssy,    typeof(OpCodeSsy));
            Set("1111000011111x", InstEmit.Sync,   typeof(OpCodeSync));
            Set("110000xxxx111x", InstEmit.Tex,    typeof(OpCodeTex));
            Set("1101x00xxxxxxx", InstEmit.Texs,   typeof(OpCodeTexs));
            Set("0100111xxxxxxx", InstEmit.Xmad,   typeof(OpCodeAluCbuf));
            Set("0011011x00xxxx", InstEmit.Xmad,   typeof(OpCodeAluImm));
            Set("010100010xxxxx", InstEmit.Xmad,   typeof(OpCodeAluRegCbuf));
            Set("0101101100xxxx", InstEmit.Xmad,   typeof(OpCodeAluReg));
#endregion
        }

        private static void Set(string encoding, InstEmitter emitter, Type opCodeType)
        {
            if (encoding.Length != EncodingBits)
            {
                throw new ArgumentException(nameof(encoding));
            }

            int bit   = encoding.Length - 1;
            int value = 0;
            int xMask = 0;
            int xBits = 0;

            int[] xPos = new int[encoding.Length];

            for (int index = 0; index < encoding.Length; index++, bit--)
            {
                char chr = encoding[index];

                if (chr == '1')
                {
                    value |= 1 << bit;
                }
                else if (chr == 'x')
                {
                    xMask |= 1 << bit;

                    xPos[xBits++] = bit;
                }
            }

            xMask = ~xMask;

            TableEntry entry = new TableEntry(emitter, opCodeType, xBits);

            for (int index = 0; index < (1 << xBits); index++)
            {
                value &= xMask;

                for (int X = 0; X < xBits; X++)
                {
                    value |= ((index >> X) & 1) << xPos[X];
                }

                if (_opCodes[value] == null || _opCodes[value].XBits > xBits)
                {
                    _opCodes[value] = entry;
                }
            }
        }

        public static (InstEmitter emitter, Type opCodeType) GetEmitter(long OpCode)
        {
            TableEntry entry = _opCodes[(ulong)OpCode >> (64 - EncodingBits)];

            if (entry != null)
            {
                return (entry.Emitter, entry.OpCodeType);
            }

            return (null, null);
        }
    }
}