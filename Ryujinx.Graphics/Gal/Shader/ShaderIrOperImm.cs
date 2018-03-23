namespace Ryujinx.Graphics.Gal.Shader
{
    class ShaderIrOperImm : ShaderIrOper
    {
        public int Imm { get; private set; }

        public ShaderIrOperImm(int Imm)
        {
            this.Imm = Imm;
        }
    }
}