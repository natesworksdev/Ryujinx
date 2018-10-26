using System;

using static Ryujinx.Graphics.Gal.Shader.ShaderDecodeHelper;

namespace Ryujinx.Graphics.Gal.Shader
{
    internal static partial class ShaderDecode
    {
        private const int TempRegStart = 0x100;

        private const int ____ = 0x0;
        private const int R = 0x1;
        private const int G = 0x2;
        private const int Rg = 0x3;
        private const int B = 0x4;
        private const int Rgb = 0x7;
        private const int A = 0x8;
        private const int RA = 0x9;
        private const int GA = 0xa;
        private const int RgA = 0xb;
        private const int Ba = 0xc;
        private const int RBa = 0xd;
        private const int Gba = 0xe;
        private const int Rgba = 0xf;

        private static int[,] _maskLut = new int[,]
        {
            { ____, ____, ____, ____, ____, ____, ____, ____ },
            { R, G, B, A, Rg, RA, GA, Ba },
            { R, G, B, A, Rg, ____, ____, ____ },
            { Rgb, RgA, RBa, Gba, Rgba, ____, ____, ____ }
        };

        public static void Ld_A(ShaderIrBlock block, long opCode, int position)
        {
            ShaderIrNode[] opers = opCode.Abuf20();

            //Used by GS
            ShaderIrOperGpr vertex = opCode.Gpr39();

            int index = 0;

            foreach (ShaderIrNode operA in opers)
            {
                ShaderIrOperGpr operD = opCode.Gpr0();

                operD.Index += index++;

                block.AddNode(opCode.PredNode(new ShaderIrAsg(operD, operA)));
            }
        }

        public static void Ld_C(ShaderIrBlock block, long opCode, int position)
        {
            int cbufPos   = opCode.Read(22, 0x3fff);
            int cbufIndex = opCode.Read(36, 0x1f);
            int type      = opCode.Read(48, 7);

            if (type > 5)
            {
                throw new InvalidOperationException();
            }

            ShaderIrOperGpr temp = ShaderIrOperGpr.MakeTemporary();

            block.AddNode(new ShaderIrAsg(temp, opCode.Gpr8()));

            int count = type == 5 ? 2 : 1;

            for (int index = 0; index < count; index++)
            {
                ShaderIrOperCbuf operA = new ShaderIrOperCbuf(cbufIndex, cbufPos, temp);

                ShaderIrOperGpr operD = opCode.Gpr0();

                operA.Pos   += index;
                operD.Index += index;

                if (!operD.IsValidRegister)
                {
                    break;
                }

                ShaderIrNode node = operA;

                if (type < 4)
                {
                    //This is a 8 or 16 bits type.
                    bool signed = (type & 1) != 0;

                    int size = 8 << (type >> 1);

                    node = ExtendTo32(node, signed, size);
                }

                block.AddNode(opCode.PredNode(new ShaderIrAsg(operD, node)));
            }
        }

        public static void St_A(ShaderIrBlock block, long opCode, int position)
        {
            ShaderIrNode[] opers = opCode.Abuf20();

            int index = 0;

            foreach (ShaderIrNode operA in opers)
            {
                ShaderIrOperGpr operD = opCode.Gpr0();

                operD.Index += index++;

                block.AddNode(opCode.PredNode(new ShaderIrAsg(operA, operD)));
            }
        }

        public static void Texq(ShaderIrBlock block, long opCode, int position)
        {
            ShaderIrNode operD = opCode.Gpr0();
            ShaderIrNode operA = opCode.Gpr8();

            ShaderTexqInfo info = (ShaderTexqInfo)opCode.Read(22, 0x1f);

            ShaderIrMetaTexq meta0 = new ShaderIrMetaTexq(info, 0);
            ShaderIrMetaTexq meta1 = new ShaderIrMetaTexq(info, 1);

            ShaderIrNode operC = opCode.Imm13_36();

            ShaderIrOp op0 = new ShaderIrOp(ShaderIrInst.Texq, operA, null, operC, meta0);
            ShaderIrOp op1 = new ShaderIrOp(ShaderIrInst.Texq, operA, null, operC, meta1);

            block.AddNode(opCode.PredNode(new ShaderIrAsg(operD, op0)));
            block.AddNode(opCode.PredNode(new ShaderIrAsg(operA, op1))); //Is this right?
        }

        public static void Tex(ShaderIrBlock block, long opCode, int position)
        {
            EmitTex(block, opCode, false);
        }

        public static void Tex_B(ShaderIrBlock block, long opCode, int position)
        {
            EmitTex(block, opCode, true);
        }

        private static void EmitTex(ShaderIrBlock block, long opCode, bool gprHandle)
        {
            //TODO: Support other formats.
            ShaderIrOperGpr[] coords = new ShaderIrOperGpr[2];

            for (int index = 0; index < coords.Length; index++)
            {
                coords[index] = opCode.Gpr8();

                coords[index].Index += index;

                if (coords[index].Index > ShaderIrOperGpr.ZrIndex)
                {
                    coords[index].Index = ShaderIrOperGpr.ZrIndex;
                }
            }

            int chMask = opCode.Read(31, 0xf);

            ShaderIrNode operC = gprHandle
                ? (ShaderIrNode)opCode.Gpr20()
                : (ShaderIrNode)opCode.Imm13_36();

            ShaderIrInst inst = gprHandle ? ShaderIrInst.Texb : ShaderIrInst.Texs;

            for (int ch = 0; ch < 4; ch++)
            {
                ShaderIrOperGpr dst = new ShaderIrOperGpr(TempRegStart + ch);

                ShaderIrMetaTex meta = new ShaderIrMetaTex(ch);

                ShaderIrOp op = new ShaderIrOp(inst, coords[0], coords[1], operC, meta);

                block.AddNode(opCode.PredNode(new ShaderIrAsg(dst, op)));
            }

            int regInc = 0;

            for (int ch = 0; ch < 4; ch++)
            {
                if (!IsChannelUsed(chMask, ch))
                {
                    continue;
                }

                ShaderIrOperGpr src = new ShaderIrOperGpr(TempRegStart + ch);

                ShaderIrOperGpr dst = opCode.Gpr0();

                dst.Index += regInc++;

                if (dst.Index >= ShaderIrOperGpr.ZrIndex)
                {
                    continue;
                }

                block.AddNode(opCode.PredNode(new ShaderIrAsg(dst, src)));
            }
        }

        public static void Texs(ShaderIrBlock block, long opCode, int position)
        {
            EmitTexs(block, opCode, ShaderIrInst.Texs);
        }

        public static void Tlds(ShaderIrBlock block, long opCode, int position)
        {
            EmitTexs(block, opCode, ShaderIrInst.Txlf);
        }

        private static void EmitTexs(ShaderIrBlock block, long opCode, ShaderIrInst inst)
        {
            //TODO: Support other formats.
            ShaderIrNode operA = opCode.Gpr8();
            ShaderIrNode operB = opCode.Gpr20();
            ShaderIrNode operC = opCode.Imm13_36();

            int lutIndex;

            lutIndex  = opCode.Gpr0 ().Index != ShaderIrOperGpr.ZrIndex ? 1 : 0;
            lutIndex |= opCode.Gpr28().Index != ShaderIrOperGpr.ZrIndex ? 2 : 0;

            if (lutIndex == 0)
            {
                return;
            }

            int chMask = _maskLut[lutIndex, opCode.Read(50, 7)];

            for (int ch = 0; ch < 4; ch++)
            {
                ShaderIrOperGpr dst = new ShaderIrOperGpr(TempRegStart + ch);

                ShaderIrMetaTex meta = new ShaderIrMetaTex(ch);

                ShaderIrOp op = new ShaderIrOp(inst, operA, operB, operC, meta);

                block.AddNode(opCode.PredNode(new ShaderIrAsg(dst, op)));
            }

            int regInc = 0;

            ShaderIrOperGpr GetDst()
            {
                ShaderIrOperGpr dst;

                switch (lutIndex)
                {
                    case 1: dst = opCode.Gpr0();  break;
                    case 2: dst = opCode.Gpr28(); break;
                    case 3: dst = regInc >> 1 != 0
                        ? opCode.Gpr28()
                        : opCode.Gpr0 (); break;

                    default: throw new InvalidOperationException();
                }

                dst.Index += regInc++ & 1;

                return dst;
            }

            for (int ch = 0; ch < 4; ch++)
            {
                if (!IsChannelUsed(chMask, ch))
                {
                    continue;
                }

                ShaderIrOperGpr src = new ShaderIrOperGpr(TempRegStart + ch);

                ShaderIrOperGpr dst = GetDst();

                if (dst.Index != ShaderIrOperGpr.ZrIndex)
                {
                    block.AddNode(opCode.PredNode(new ShaderIrAsg(dst, src)));
                }
            }
        }

        private static bool IsChannelUsed(int chMask, int ch)
        {
            return (chMask & (1 << ch)) != 0;
        }
    }
}