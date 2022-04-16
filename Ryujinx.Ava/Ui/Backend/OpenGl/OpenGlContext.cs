using SPB.Graphics.OpenGL;
using SPB.Platform;
using SPB.Platform.WGL;
using SPB.Windowing;
using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;

namespace Ryujinx.Ava.Ui.Backend.OpenGL
{
    internal class OpenGLContext : IDisposable
    {
        public OpenGLContextBase BaseContext { get; }

        [SupportedOSPlatform("linux")]
        internal static IntPtr X11DefaultDisplay => SPB.Platform.X11.X11.DefaultDisplay;

        public OpenGLContext()
        {
            BaseContext = PlatformHelper.CreateOpenGLContext(OpenGLSurface.GetFramebufferFormat(), 4, 3, OpenGLContextFlags.Compat);
            BaseContext.Initialize();
        }

        public void MakeCurrent(SwappableNativeWindowBase window)
        {
            if (window == null)
            {
                return;
            }

            Monitor.Enter(BaseContext);

            if (OperatingSystem.IsWindows())
            {
                if (window is WGLWindow wGLWindow)
                {
                    MakeCurrent(wGLWindow.DisplayHandle.RawHandle, BaseContext.ContextHandle);
                }
            }
            else if (OperatingSystem.IsLinux())
            {
                BaseContext.MakeCurrent(window);
            }
        }

        public void ReleaseCurrent()
        {
            bool isCurrent = BaseContext.IsCurrent;

            BaseContext.MakeCurrent(null);

            if (isCurrent)
            {
                Monitor.Exit(BaseContext);
            }
        }

        [DllImport("OPENGL32.DLL", EntryPoint = "wglMakeCurrent", SetLastError = true)]
        private extern static bool MakeCurrent(IntPtr hDc, IntPtr newContext);

        public void Dispose()
        {
            BaseContext?.Dispose();
        }
    }
}
