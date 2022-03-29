using Avalonia;
using OpenTK.Graphics.OpenGL;
using System;
using System.Threading;

namespace Ryujinx.Ava.Ui.Backend.OpenGl
{
    public class OpenGlSurfaceRenderingSession : IDisposable
    {
        private readonly OpenGlSurface _window;

        public OpenGlSurfaceRenderingSession(OpenGlSurface window, float scaling)
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
