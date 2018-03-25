namespace Ryujinx.Graphics.Gal.Shader
{
    class ShaderIrOperImm : ShaderIrNode
    {
        public int Imm { get; private set; }

        public ShaderIrOperImm(int Imm)
        {
            this.Imm = Imm;
        }
    }
}