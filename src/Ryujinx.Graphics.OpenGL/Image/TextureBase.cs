using Silk.NET.OpenGL.Legacy;
using Ryujinx.Graphics.GAL;

namespace Ryujinx.Graphics.OpenGL.Image
{
    class TextureBase
    {
        private readonly protected OpenGLRenderer _gd;
        public uint Handle { get; protected set; }

        public TextureCreateInfo Info { get; }

        public int Width => Info.Width;
        public int Height => Info.Height;

        public Target Target => Info.Target;
        public Format Format => Info.Format;

        public TextureBase(OpenGLRenderer gd, TextureCreateInfo info)
        {
            _gd = gd;
            Info = info;

            Handle = _gd.Api.GenTexture();
        }

        public void Bind(uint unit)
        {
            Bind(Target.Convert(), unit);
        }

        protected void Bind(TextureTarget target, uint unit)
        {
            _gd.Api.ActiveTexture((TextureUnit)((uint)TextureUnit.Texture0 + unit));
            _gd.Api.BindTexture(target, Handle);
        }

        public static void ClearBinding(GL api, uint unit)
        {
            api.ActiveTexture((TextureUnit)((uint)TextureUnit.Texture0 + unit));
            api.BindTextureUnit(unit, 0);
        }
    }
}
