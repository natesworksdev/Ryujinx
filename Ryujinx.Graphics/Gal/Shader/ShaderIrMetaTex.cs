namespace Ryujinx.Graphics.Gal.Shader
{
    class ShaderIrMetaTex : ShaderIrMeta
    {
        public ShaderTextureType Type { get; private set; }

        public ShaderIrNode Index { get; private set; }

        public int Elem { get; private set; }

        public ShaderIrMetaTex(ShaderTextureType Type, ShaderIrNode Index, int Elem)
        {
            this.Type  = Type;
            this.Index = Index;
            this.Elem  = Elem;
        }
    }
}