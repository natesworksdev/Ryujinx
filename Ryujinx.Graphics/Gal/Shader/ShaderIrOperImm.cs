namespace Ryujinx.Graphics.Gal.Shader
{
    internal class ShaderIrOperImm : ShaderIrNode
    {
        public int Value { get; private set; }

        public ShaderIrOperImm(int value)
        {
            this.Value = value;
        }
    }
}