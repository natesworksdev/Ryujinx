namespace Ryujinx.Graphics.Gal
{
    public struct GalTextureSampler
    {
        public GalTextureWrap AddressU { get; private set; }
        public GalTextureWrap AddressV { get; private set; }
        public GalTextureWrap AddressP { get; private set; }

        public GalTextureFilter    MinFilter { get; private set; }
        public GalTextureFilter    MagFilter { get; private set; }
        public GalTextureMipFilter MipFilter { get; private set; }

        public GalColorF BorderColor { get; private set; }

        public GalTextureSampler(
            GalTextureWrap      addressU,
            GalTextureWrap      addressV,
            GalTextureWrap      addressP,
            GalTextureFilter    minFilter,
            GalTextureFilter    magFilter,
            GalTextureMipFilter mipFilter,
            GalColorF           borderColor)
        {
            this.AddressU    = addressU;
            this.AddressV    = addressV;
            this.AddressP    = addressP;
            this.MinFilter   = minFilter;
            this.MagFilter   = magFilter;
            this.MipFilter   = mipFilter;
            this.BorderColor = borderColor;
        }
    }
}