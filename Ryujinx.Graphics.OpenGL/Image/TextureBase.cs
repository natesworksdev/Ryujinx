using OpenTK.Graphics.OpenGL;
using Ryujinx.Graphics.GAL;

namespace Ryujinx.Graphics.OpenGL.Image
{
    class TextureBase
    {
        public int Handle { get; protected set; }

        public TextureCreateInfo Info { get; }

        public int Width => Info.Width;
        public int Height => Info.Height;
        public float ScaleFactor { get; }

        public Target Target => Info.Target;
        public Format Format => Info.Format;

        public TextureBase(TextureCreateInfo info, float scaleFactor = 1f)
        {
            Info = info;
            ScaleFactor = scaleFactor;

            Handle = GL.GenTexture();
        }

        public void Bind(int unit)
        {
            GL.BindTextureUnit(unit, Handle);
        }
    }
}
