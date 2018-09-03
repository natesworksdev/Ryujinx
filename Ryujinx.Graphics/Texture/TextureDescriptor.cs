using Ryujinx.Graphics.Gal;

namespace Ryujinx.Graphics.Texture
{
    struct TextureDescriptor
    {
        public TextureReaderDelegate Reader;

        public GalImageFormat? Snorm;
        public GalImageFormat? Unorm;
        public GalImageFormat? Sint;
        public GalImageFormat? Uint;
        public GalImageFormat? Float;
        public GalImageFormat? Snorm_Force_Fp16;
        public GalImageFormat? Unorm_Force_Fp16;

        public TextureDescriptor(
            TextureReaderDelegate Reader,
            GalImageFormat?       Snorm            = null,
            GalImageFormat?       Unorm            = null,
            GalImageFormat?       Sint             = null,
            GalImageFormat?       Uint             = null,
            GalImageFormat?       Float            = null,
            GalImageFormat?       Snorm_Force_Fp16 = null,
            GalImageFormat?       Unorm_Force_Fp16 = null)
        {
            this.Reader           = Reader;
            this.Snorm            = Snorm;
            this.Unorm            = Unorm;
            this.Sint             = Sint;
            this.Uint             = Uint;
            this.Float            = Float;
            this.Snorm_Force_Fp16 = Snorm_Force_Fp16;
            this.Unorm_Force_Fp16 = Unorm_Force_Fp16;
        }
    }
}