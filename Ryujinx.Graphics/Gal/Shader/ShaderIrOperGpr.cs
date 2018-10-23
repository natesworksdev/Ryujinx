namespace Ryujinx.Graphics.Gal.Shader
{
    class ShaderIrOperGpr : ShaderIrNode
    {
        public const int ZrIndex = 0xff;

        public bool IsConst => Index == ZrIndex;

        public bool IsValidRegister => (Index <= ZrIndex);

        public int Index { get; set; }

        public ShaderIrOperGpr(int index)
        {
            Index = index;
        }

        public static ShaderIrOperGpr MakeTemporary(int index = 0)
        {
            return new ShaderIrOperGpr(0x100 + index);
        }
    }
}