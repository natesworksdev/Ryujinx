using OpenTK.Graphics.OpenGL;
using Ryujinx.Common.Configuration;
using SPB.Graphics;
using SPB.Graphics.OpenGL;
using SPB.Platform;
using SPB.Platform.GLX;
using SPB.Platform.WGL;
using SPB.Platform.Win32;
using SPB.Windowing;
using System.Runtime.InteropServices;

namespace Ryujinx.Ava.Ui.Controls
{
    public class OpenGLEmbeddedWindow : EmbeddedWindow
    {
        private readonly int _major;
        private readonly int _minor;
        private readonly GraphicsDebugLevel _graphicsDebugLevel;
        private SwappableNativeWindowBase _window;

        public bool IsSecond { get; set; }
        public OpenGLContextBase Context { get; set; }

        public OpenGLEmbeddedWindow(int major, int minor, GraphicsDebugLevel graphicsDebugLevel)
        {
            _major = major;
            _minor = minor;
            _graphicsDebugLevel = graphicsDebugLevel;
        }

        protected override void OnWindowDestroying()
        {
            base.OnWindowDestroying();

            Context.Dispose();
        }

        public override void OnWindowCreated()
        {
            base.OnWindowCreated();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                _window = new WGLWindow(new NativeHandle(WindowHandle));
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                _window = new GLXWindow(new NativeHandle(X11Display),new NativeHandle(WindowHandle));
            
            var flags = OpenGLContextFlags.Compat;
            if (_graphicsDebugLevel != GraphicsDebugLevel.None)
            {
                flags |= OpenGLContextFlags.Debug;
            }

            Context = PlatformHelper.CreateOpenGLContext(FramebufferFormat.Default, _major, _minor, flags);

            Context.Initialize(_window);
            Context.MakeCurrent(_window);

            var bindingsContext = new OpenToolkitBindingsContext(Context.GetProcAddress);

            GL.LoadBindings(bindingsContext);
            OpenTK.Graphics.OpenGL.GL.LoadBindings(bindingsContext);
        }

        public void MakeCurrent()
        {
            Context.MakeCurrent(_window);
        }

        public void MakeCurrent(NativeWindowBase window)
        {
            Context.MakeCurrent(window);
        }

        public void SwapBuffers()
        {
            _window.SwapBuffers();
        }
    }
}