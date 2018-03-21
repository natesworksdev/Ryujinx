namespace Ryujinx.Graphics.Gal.Shader
{
    class ShaderIrNode
    {
        public ShaderIrInst Inst;

        public ShaderIrNode(ShaderIrInst Inst)
        {
            this.Inst = Inst;
        }
    }
}