namespace Ryujinx.Graphics.Gal.Shader
{
    class ShaderIrNodeLdr : ShaderIrNode
    {
        public int GprIndex { get; private set; }

        public ShaderIrNodeLdr(int GprIndex) : base(ShaderIrInst.Ld)
        {
            this.GprIndex = GprIndex;
        }
    }
}