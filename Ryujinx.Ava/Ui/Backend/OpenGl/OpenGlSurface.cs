using Avalonia;
using SPB.Graphics;
using SPB.Graphics.OpenGL;
using SPB.Platform;
using SPB.Windowing;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Ryujinx.Ava.Ui.Backend.OpenGl
{

    public class OpenGlSurface : BackendSurface
    {
        public SwappableNativeWindowBase Window { get; }

        private OpenGlSkiaGpu _gpu;
        private AutoResetEvent _swapEvent;

        private static readonly Dictionary<IntPtr, AutoResetEvent> _swapEvents;

        static OpenGlSurface()
        {
            _swapEvents = new Dictionary<IntPtr, AutoResetEvent>();
        }

        public static AutoResetEvent GetWindowSwapEvent(IntPtr windowHandle)
        {
            if (_swapEvents.TryGetValue(windowHandle, out var swapEvent))
            {
                return swapEvent;
            }

            return null;
        }

        public OpenGlSurface(IntPtr handle) : base(handle)
        {
            if (OperatingSystem.IsWindows())
            {
                Window = new SPB.Platform.WGL.WGLWindow(new NativeHandle(Handle));
            }
            else if (OperatingSystem.IsLinux())
            {
                Window = new SPB.Platform.GLX.GLXWindow(new NativeHandle(OpenGl.OpenGlContext.X11DefaultDisplay), new NativeHandle(Handle));
            }

            _gpu = AvaloniaLocator.Current.GetService<OpenGlSkiaGpu>();

            var context = PlatformHelper.CreateOpenGLContext(OpenGlSurface.GetFramebufferFormat(), 4, 3, OpenGLContextFlags.Default);
            context.Initialize(Window);

            context.Dispose();

            _swapEvent = new AutoResetEvent(true);

            _swapEvents.TryAdd(handle, _swapEvent);
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
            _swapEvent.Set();
        }

        public void ReleaseCurrent()
        {
            _gpu.PrimaryContext.ReleaseCurrent();
        }

        public override void Dispose()
        {
            _swapEvent?.Dispose();
            _swapEvents.Remove(Handle);
            base.Dispose();
        }
    }
}