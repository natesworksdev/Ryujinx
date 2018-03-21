namespace Ryujinx.Graphics.Gal.Shader
{
    class ShaderIrOperReg : ShaderIrOper
    {
        public int GprIndex { get; private set; }

        public ShaderIrOperReg(int GprIndex)
        {
            this.GprIndex = GprIndex;
        }
    }
}