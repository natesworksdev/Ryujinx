namespace Ryujinx.Graphics.Gal.Shader
{
    class ShaderIrOperReg : ShaderIrOper
    {
        public const int ZRIndex = 0xff;

        public int GprIndex { get; private set; }

        public ShaderIrOperReg(int GprIndex)
        {
            this.GprIndex = GprIndex;
        }
    }
}