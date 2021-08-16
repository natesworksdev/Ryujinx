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

        public unsafe int Create(bool init)
        {
            int localHandle;

            if (init)
            {
                GL.CreateTextures(Target.Convert(), 1, &localHandle);
            }
            else
            {
                GL.GenTextures(1, &localHandle);
            }

            return localHandle;
        }

        public TextureBase(TextureCreateInfo info, float scaleFactor = 1f, bool init = true)
        {
            Info = info;
            ScaleFactor = scaleFactor;

            Handle = Create(init);
        }

        public void Bind(int unit)
        {
            GL.BindTextureUnit(unit, Handle);
        }
    }
}
