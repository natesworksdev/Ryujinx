namespace Ryujinx.Graphics.Gal.Shader
{
    class ShaderIrNodeStr : ShaderIrNode
    {
        public int GprIndex { get; private set; }

        public ShaderIrNodeStr(int GprIndex) : base(ShaderIrInst.St)
        {
            this.GprIndex = GprIndex;
        }
    }
}