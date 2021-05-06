using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Ryujinx.Common.Configuration;

namespace Ryujinx.Ava.Ui.Controls
{
    public class OpenGlEmbeddedWindow : NativeEmbeddedWindow
    {
        public OpenGlEmbeddedWindow(int major, int minor, GraphicsDebugLevel graphicsDebugLevel, double scale) : base(scale)
        {
            Major = major;
            Minor = minor;
            DebugLevel = graphicsDebugLevel;

            GLFW.WindowHint(WindowHintClientApi.ClientApi,      ClientApi.OpenGlApi);
            GLFW.WindowHint(WindowHintInt.ContextVersionMajor,  major);
            GLFW.WindowHint(WindowHintInt.ContextVersionMinor,  minor);
            GLFW.WindowHint(WindowHintBool.OpenGLForwardCompat, true);

            if (DebugLevel != GraphicsDebugLevel.None)
            {
                GLFW.WindowHint(WindowHintBool.OpenGLDebugContext, true);
            }
        }

        public override unsafe void OnWindowCreated()
        {
            base.OnWindowCreated();

            GlfwWindow.MakeCurrent();

            GL.LoadBindings(new OpenToolkitBindingsContext());
            GLFW.MakeContextCurrent(null);
        }

        public void MakeCurrent()
        {
            GlfwWindow.MakeCurrent();
        }

        public unsafe void MakeCurrent(Window* window)
        {
            GLFW.MakeContextCurrent(window);
        }

        public override void Present()
        {
            GlfwWindow.SwapBuffers();
        }
    }
}