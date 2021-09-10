using OpenTK.Graphics.OpenGL;
using Ryujinx.Graphics.GAL;

namespace Ryujinx.Graphics.OpenGL.Image
{
    class TextureBase
    {
        private readonly Target _target;

        public int Handle { get; protected set; }

        public TextureBase(Target target)
        {
            _target = target;
            Handle = GL.GenTexture();
        }

        public void Bind(int unit)
        {
            Bind(_target.Convert(), unit);
        }

        protected void Bind(TextureTarget target, int unit)
        {
            GL.ActiveTexture(TextureUnit.Texture0 + unit);
            GL.BindTexture(target, Handle);
        }
    }
}
