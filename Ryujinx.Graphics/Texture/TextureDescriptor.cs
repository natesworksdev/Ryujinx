using Ryujinx.Graphics.Gal;

namespace Ryujinx.Graphics.Texture
{
    struct TextureDescriptor
    {
        public TextureReaderDelegate Reader;

        public GalImageFormat Format;

        public TextureDescriptor(
            TextureReaderDelegate Reader,
            GalImageFormat        Format)
        {
            this.Reader = Reader;
            this.Format = Format;
        }
    }
}