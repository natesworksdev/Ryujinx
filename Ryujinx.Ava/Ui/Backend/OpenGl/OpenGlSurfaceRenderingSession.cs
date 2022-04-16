using Avalonia;
using OpenTK.Graphics.OpenGL;
using System;

namespace Ryujinx.Ava.Ui.Backend.OpenGL
{
    public class OpenGLSurfaceRenderingSession : IDisposable
    {
        private readonly OpenGLSurface _window;

        public OpenGLSurfaceRenderingSession(OpenGLSurface window, float scaling)
        {
            _window = window;
            Scaling = scaling;
            _window.MakeCurrent();
        }

        public PixelSize Size => _window.Size;

        public PixelSize CurrentSize => _window.CurrentSize;

        public float Scaling { get; }

        public bool IsYFlipped { get; } = false;

        public void Dispose()
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            _window.SwapBuffers();
            _window.ReleaseCurrent();
        }
    }
}
