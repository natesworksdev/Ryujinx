namespace Ryujinx.Graphics.Gal.Shader
{
    class ShaderIrOperImmf : ShaderIrOper
    {
        public float Imm { get; private set; }

        public ShaderIrOperImmf(float Imm)
        {
            this.Imm = Imm;
        }
    }
}