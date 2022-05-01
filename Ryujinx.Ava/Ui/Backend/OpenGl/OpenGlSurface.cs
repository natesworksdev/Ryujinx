using Avalonia;
using SPB.Graphics;
using SPB.Graphics.OpenGL;
using SPB.Platform;
using SPB.Windowing;
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Ryujinx.Ava.Ui.Backend.OpenGL
{
    public class OpenGLSurface : BackendSurface
    {
        public SwappableNativeWindowBase Window { get; }

        private OpenGLSkiaGpu _gpu;
        private AutoResetEvent _swapEvent;

        private static readonly ConcurrentDictionary<IntPtr, AutoResetEvent> _swapEvents;

        static OpenGLSurface()
        {
            _swapEvents = new ConcurrentDictionary<IntPtr, AutoResetEvent>();
        }

        public static AutoResetEvent GetWindowSwapEvent(IntPtr windowHandle)
        {
            if (_swapEvents.TryGetValue(windowHandle, out var swapEvent))
            {
                return swapEvent;
            }

            return null;
        }

        public OpenGLSurface(IntPtr handle) : base(handle)
        {
            if (OperatingSystem.IsWindows())
            {
                Window = new SPB.Platform.WGL.WGLWindow(new NativeHandle(Handle));
            }
            else if (OperatingSystem.IsLinux())
            {
                Window = new SPB.Platform.GLX.GLXWindow(new NativeHandle(OpenGLContext.X11DefaultDisplay), new NativeHandle(Handle));
            }

            _gpu = AvaloniaLocator.Current.GetService<OpenGLSkiaGpu>();

            var context = PlatformHelper.CreateOpenGLContext(GetFramebufferFormat(), 4, 3, OpenGLContextFlags.Default);
            context.Initialize(Window);

            context.Dispose();

            _swapEvent = new AutoResetEvent(true);

            _swapEvents.TryAdd(handle, _swapEvent);
        }

        internal static FramebufferFormat GetFramebufferFormat()
        {
            return FramebufferFormat.Default;
        }

        public OpenGLSurfaceRenderingSession BeginDraw()
        {
            return new OpenGLSurfaceRenderingSession(this, (float)Program.WindowScaleFactor);
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
            _swapEvent.Set();
            _swapEvent.Dispose();
            _swapEvents.TryRemove(Handle, out _);
            base.Dispose();
        }
    }
}