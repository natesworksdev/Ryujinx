namespace Ryujinx.Graphics.Gal.Shader
{
    class ShaderIrNode
    {
        public ShaderIrOper Dst { get; set; }
        public ShaderIrOper Src { get; set; }

        public ShaderIrNode(ShaderIrOper Dst, ShaderIrOper Src)
        {
            this.Dst = Dst;
            this.Src = Src;
        }
    }
}