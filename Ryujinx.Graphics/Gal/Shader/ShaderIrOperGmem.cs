namespace Ryujinx.Graphics.Gal.Shader
{
    class ShaderIrOperGmem : ShaderIrNode
    {
        public ShaderIrNode BaseAddress { get; private set; }

        public int Offset { get; private set; }

        public ShaderIrOperGmem(ShaderIrNode BaseAddress, int Offset)
        {
            this.BaseAddress = BaseAddress;
            this.Offset      = Offset;
        }
    }
}