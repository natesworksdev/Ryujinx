namespace Ryujinx.Graphics.Gal.Shader
{
    internal class ShaderIrMetaIpa : ShaderIrMeta
    {
        public ShaderIpaMode Mode { get; private set; }

        public ShaderIrMetaIpa(ShaderIpaMode mode)
        {
            this.Mode = mode;
        }
    }
}