using Silk.NET.OpenGL;
using Ryujinx.Graphics.GAL;

namespace Ryujinx.Graphics.OpenGL.Image
{
    class TextureBase
    {
        public uint Handle { get; protected set; }

        public TextureCreateInfo Info { get; }

        public int Width => Info.Width;
        public int Height => Info.Height;

        public Target Target => Info.Target;
        public Format Format => Info.Format;

        private protected GL _api;

        public TextureBase(GL api, TextureCreateInfo info)
        {
            _api = api;
            Info = info;

            Handle = _api.GenTexture();
        }

        public void Bind(uint unit)
        {
            Bind(Target.Convert(), unit);
        }

        protected void Bind(TextureTarget target, uint unit)
        {
            _api.ActiveTexture((TextureUnit)((uint)TextureUnit.Texture0 + unit));
            _api.BindTexture(target, Handle);
        }

        public static void ClearBinding(GL api, uint unit)
        {
            api.ActiveTexture((TextureUnit)((uint)TextureUnit.Texture0 + unit));
            api.BindTextureUnit(unit, 0);
        }
    }
}
