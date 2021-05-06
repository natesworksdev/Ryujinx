using OpenTK.Windowing.GraphicsLibraryFramework;
using Ryujinx.Graphics.OpenGL;

namespace Ryujinx.Ava.Common
{
    internal unsafe class AvaloniaOpenGLContext : IOpenGLContext
    {
        internal AvaloniaOpenGLContext(Window* window)
        {
            Window = window;
        }

        public Window* Window { get; }

        public void Dispose()
        {
            GLFW.DestroyWindow(Window);
        }

        public void MakeCurrent()
        {
            GLFW.MakeContextCurrent(Window);
        }
    }
}
