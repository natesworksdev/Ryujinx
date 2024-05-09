using Silk.NET.OpenGL;
using Ryujinx.Graphics.GAL;

namespace Ryujinx.Graphics.OpenGL.Image
{
    class ImageArray : IImageArray
    {
        private record struct TextureRef
        {
            public uint Handle;
            public Format Format;
        }

        private readonly TextureRef[] _images;
        private GL _api;

        public ImageArray(GL api, int size)
        {
            _api = api;
            _images = new TextureRef[size];
        }

        public void SetFormats(int index, Format[] imageFormats)
        {
            for (int i = 0; i < imageFormats.Length; i++)
            {
                _images[index + i].Format = imageFormats[i];
            }
        }

        public void SetImages(int index, ITexture[] images)
        {
            for (int i = 0; i < images.Length; i++)
            {
                ITexture image = images[i];

                if (image is TextureBase imageBase)
                {
                    _images[index + i].Handle = imageBase.Handle;
                }
                else
                {
                    _images[index + i].Handle = 0;
                }
            }
        }

        public void Bind(uint baseBinding)
        {
            for (int i = 0; i < _images.Length; i++)
            {
                if (_images[i].Handle == 0)
                {
                    _api.BindImageTexture((uint)(baseBinding + i), 0, 0, true, 0, BufferAccessARB.ReadWrite, InternalFormat.Rgba8);
                }
                else
                {
                    InternalFormat format = (InternalFormat)FormatTable.GetImageFormat(_images[i].Format);

                    if (format != 0)
                    {
                        _api.BindImageTexture((uint)(baseBinding + i), _images[i].Handle, 0, true, 0, BufferAccessARB.ReadWrite, format);
                    }
                }
            }
        }
    }
}
