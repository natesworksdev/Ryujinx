using Avalonia;
using Avalonia.Platform;
using OpenTK.Graphics.OpenGL;
using PInvoke;
using Ryujinx.Ava.Ui.Controls;
using SPB.Graphics;
using SPB.Graphics.OpenGL;
using SPB.Platform;
using SPB.Windowing;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using Ryujinx.Ava.Ui.Backend;

namespace Ryujinx.Ava.Ui.Backend.OpenGl
{

    public class OpenGlSurface : BackendSurface
    {
        public SwappableNativeWindowBase Window { get; }

        private int _texture;
        private OpenGlSkiaGpu _gpu;

        public OpenGlSurface(IntPtr handle) : base(handle)
        {
            if (OperatingSystem.IsWindows())
            {
                Window = new SPB.Platform.WGL.WGLWindow(new NativeHandle(Handle));
            }
            else if (OperatingSystem.IsLinux())
            {
                Window = new SPB.Platform.GLX.GLXWindow(new NativeHandle(Display), new NativeHandle(Handle));
            }

            _gpu = AvaloniaLocator.Current.GetService<OpenGlSkiaGpu>();

            var context = PlatformHelper.CreateOpenGLContext(OpenGlSurface.GetFramebufferFormat(), 3, 2, OpenGLContextFlags.Compat);
            context.Initialize(Window);

            context.Dispose();
        }

        internal static FramebufferFormat GetFramebufferFormat()
        {
            return FramebufferFormat.Default;
        }

        public OpenGlSurfaceRenderingSession BeginDraw()
        {
            return new OpenGlSurfaceRenderingSession(this, (float)Program.WindowScaleFactor);
        }

        public void MakeCurrent()
        {
            _gpu.PrimaryContext.MakeCurrent(Window);
        }

        public void SwapBuffers()
        {
            Window.SwapBuffers();
        }

        public void ReleaseCurrent()
        {
            _gpu.PrimaryContext.ReleaseCurrent();
        }

        public override void Dispose()
        {
            base.Dispose();
        }
    }
}