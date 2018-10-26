namespace Ryujinx.Graphics.Gal.Shader
{
    internal class ShaderIrAsg : ShaderIrNode
    {
        public ShaderIrNode Dst { get; set; }
        public ShaderIrNode Src { get; set; }

        public ShaderIrAsg(ShaderIrNode dst, ShaderIrNode src)
        {
            Dst = dst;
            Src = src;
        }
    }
}