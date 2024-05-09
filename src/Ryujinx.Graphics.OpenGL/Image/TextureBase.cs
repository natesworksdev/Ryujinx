using Silk.NET.OpenGL;
using Ryujinx.Graphics.GAL;

namespace Ryujinx.Graphics.OpenGL.Image
{
    class TextureBase
    {
        private readonly protected GL Api;
        public uint Handle { get; protected set; }

        public TextureCreateInfo Info { get; }

        public int Width => Info.Width;
        public int Height => Info.Height;

        public Target Target => Info.Target;
        public Format Format => Info.Format;

        public TextureBase(GL api, TextureCreateInfo info)
        {
            Api = api;
            Info = info;

            Handle = Api.GenTexture();
        }

        public void Bind(uint unit)
        {
            Bind(Target.Convert(), unit);
        }

        protected void Bind(TextureTarget target, uint unit)
        {
            Api.ActiveTexture((TextureUnit)((uint)TextureUnit.Texture0 + unit));
            Api.BindTexture(target, Handle);
        }

        public static void ClearBinding(GL api, uint unit)
        {
            api.ActiveTexture((TextureUnit)((uint)TextureUnit.Texture0 + unit));
            api.BindTextureUnit(unit, 0);
        }
    }
}
