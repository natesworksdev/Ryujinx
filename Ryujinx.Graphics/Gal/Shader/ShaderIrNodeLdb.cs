namespace Ryujinx.Graphics.Gal.Shader
{
    class ShaderIrNodeLdb : ShaderIrNode
    {
        public int Cbuf { get; private set; }
        public int Offs { get; private set; }

        public ShaderIrNodeLdb(int Cbuf, int Offs) : base(ShaderIrInst.Ld)
        {
            this.Cbuf = Cbuf;
            this.Offs = Offs;
        }
    }
}