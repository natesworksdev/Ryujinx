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
            Bind(Target.Convert(), unit);
        }

        protected void Bind(TextureTarget target, int unit)
        {
            GL.ActiveTexture(TextureUnit.Texture0 + unit);
            GL.BindTexture(target, Handle);
        }

        public static void ClearBinding(int unit)
        {
            GL.ActiveTexture(TextureUnit.Texture0 + unit);

            // Clear all possible targets since we don't know which one the shader will access.
            GL.BindTexture(TextureTarget.Texture1D, 0);
            GL.BindTexture(TextureTarget.Texture2D, 0);
            GL.BindTexture(TextureTarget.Texture3D, 0);
            GL.BindTexture(TextureTarget.Texture1DArray, 0);
            GL.BindTexture(TextureTarget.Texture2DArray, 0);
            GL.BindTexture(TextureTarget.Texture2DMultisample, 0);
            GL.BindTexture(TextureTarget.Texture2DMultisampleArray, 0);
            GL.BindTexture(TextureTarget.TextureCubeMap, 0);
            GL.BindTexture(TextureTarget.TextureCubeMapArray, 0);
            GL.BindTexture(TextureTarget.TextureBuffer, 0);
        }
    }
}
