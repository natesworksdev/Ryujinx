namespace Ryujinx.Graphics.Gal.Shader
{
    class ShaderIrMetaTexq : ShaderIrMetaTex
    {
        public ShaderTexqInfo Info { get; private set; }

        public ShaderIrMetaTexq(ShaderTexqInfo Info, ShaderTextureType Type, ShaderIrNode Index, int Elem)
            : base(Type, Index, Elem)
        {
            this.Info = Info;
        }
    }
}