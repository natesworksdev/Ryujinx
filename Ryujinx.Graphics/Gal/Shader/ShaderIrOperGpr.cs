namespace Ryujinx.Graphics.Gal.Shader
{
    class ShaderIrOperGpr : ShaderIrNode
    {
        public const int ZRIndex = 0xff;

        public int Index { get; set; }

        public ShaderIrOperGpr(int Index)
        {
            this.Index = Index;
        }
    }
}