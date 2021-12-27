using OpenTK.Graphics.OpenGL;
using System;

namespace Ryujinx.Graphics.OpenGL.Helper
{
    public struct RenderTarget : IDisposable
    {
        public int Framebuffer { get; private set; }
        public int Renderbuffer { get; private set; }
        public int Texture { get; private set; }

        public RenderTarget(int framebuffer, int renderbuffer, int texture)
        {
            Framebuffer = framebuffer;
            Renderbuffer = renderbuffer;
            Texture = texture;
        }

        public void Dispose()
        {
            if (Framebuffer != 0)
            {
                GL.DeleteFramebuffer(Framebuffer);
                GL.DeleteRenderbuffer(Renderbuffer);
                GL.DeleteTexture(Texture);
            }

            Framebuffer = 0;
            Renderbuffer = 0;
            Texture = 0;
        }
    }
}