namespace Ryujinx.Graphics.Gal.Shader
{
    internal class ShaderIrOperImmf : ShaderIrNode
    {
        public float Value { get; private set; }

        public ShaderIrOperImmf(float value)
        {
            this.Value = value;
        }
    }
}