namespace Ryujinx.Graphics.Gal.Shader
{
    class ShaderIrOperGpr : ShaderIrNode
    {
        public const int ZRIndex = 0xff;

        public static ShaderIrOperGpr MakeTemporary() => new ShaderIrOperGpr(0x100);

        public bool IsConst => Index == ZRIndex;

        public int Index { get; set; }

        public ShaderIrOperGpr(int Index)
        {
            this.Index = Index;
        }
    }
}