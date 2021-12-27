using OpenTK.Graphics.OpenGL;
using System;

namespace Ryujinx.Graphics.OpenGL.Helper
{
    public static class GLHelper
    {
        public static RenderTarget GenerateRenderTarget(int width, int height)
        {
            int framebuffer = GL.GenFramebuffer();
            int texture = GL.GenTexture();

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, framebuffer);
            GL.BindTexture(TextureTarget.Texture2D, texture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBaseLevel, 0);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMaxLevel, 0);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, texture, 0);

            int renderbuffer = GL.GenRenderbuffer();
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, renderbuffer);
            GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.Depth24Stencil8, width, height);
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);

            GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthStencilAttachment, RenderbufferTarget.Renderbuffer, renderbuffer);

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.BindTexture(TextureTarget.Texture2D, 0);

            return new RenderTarget(framebuffer, renderbuffer, texture);
        }

        public static void BlitFramebuffer(int readFramebuffer, int drawFramebuffer, BlitRegion source, BlitRegion destination)
        {
            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, drawFramebuffer);
            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, readFramebuffer);

            GL.BlitFramebuffer(source.X0,
                               source.Y0,
                               source.X1,
                               source.Y1,
                               destination.X0,
                               destination.Y0,
                               destination.X1,
                               destination.Y1,
                               ClearBufferMask.ColorBufferBit,
                               BlitFramebufferFilter.Linear);

            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);
            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, 0);
        }
    }
}
