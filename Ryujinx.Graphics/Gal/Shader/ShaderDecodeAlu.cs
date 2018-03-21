namespace Ryujinx.Graphics.Gal.Shader
{
    static partial class ShaderDecode
    {
        public static void Fadd(ShaderIrBlock Block, long OpCode)
        {
            int Rd = (int)(OpCode >>  0) & 0xff;
            int Ra = (int)(OpCode >>  8) & 0xff;
            int Ob = (int)(OpCode >> 20) & 0x3fff;
            int Cb = (int)(OpCode >> 34) & 0x1f;

            bool Nb = ((OpCode >> 45) & 1) != 0;
            bool Aa = ((OpCode >> 46) & 1) != 0;
            bool Na = ((OpCode >> 48) & 1) != 0;
            bool Ab = ((OpCode >> 49) & 1) != 0;
            bool Ad = ((OpCode >> 50) & 1) != 0;

            Block.AddNode(new ShaderIrNodeLdr(Ra));

            if (Aa)
            {
                Block.AddNode(new ShaderIrNode(ShaderIrInst.Fabs));
            }

            if (Na)
            {
                Block.AddNode(new ShaderIrNode(ShaderIrInst.Fneg));
            }

            Block.AddNode(new ShaderIrNodeLdb(Cb, Ob));

            if (Ab)
            {
                Block.AddNode(new ShaderIrNode(ShaderIrInst.Fabs));
            }

            if (Nb)
            {
                Block.AddNode(new ShaderIrNode(ShaderIrInst.Fneg));
            }

            Block.AddNode(new ShaderIrNode(ShaderIrInst.Fadd));

            if (Ad)
            {
                Block.AddNode(new ShaderIrNode(ShaderIrInst.Fabs));
            }

            Block.AddNode(new ShaderIrNodeStr(Rd));
        }

        public static void Ffma(ShaderIrBlock Block, long OpCode)
        {
            int Rd = (int)(OpCode >>  0) & 0xff;
            int Ra = (int)(OpCode >>  8) & 0xff;
            int Ob = (int)(OpCode >> 20) & 0x3fff;
            int Cb = (int)(OpCode >> 34) & 0x1f;
            int Rc = (int)(OpCode >> 39) & 0xff;

            bool Nb = ((OpCode >> 48) & 1) != 0;
            bool Nc = ((OpCode >> 49) & 1) != 0;

            Block.AddNode(new ShaderIrNodeLdr(Ra));
            Block.AddNode(new ShaderIrNodeLdb(Cb, Ob));

            if (Nb)
            {
                Block.AddNode(new ShaderIrNode(ShaderIrInst.Fneg));
            }

            Block.AddNode(new ShaderIrNode(ShaderIrInst.Fmul));
            Block.AddNode(new ShaderIrNodeLdr(Rc));

            if (Nc)
            {
                Block.AddNode(new ShaderIrNode(ShaderIrInst.Fneg));
            }

            Block.AddNode(new ShaderIrNode(ShaderIrInst.Fadd));
            Block.AddNode(new ShaderIrNodeStr(Rd));
        }
    }
}