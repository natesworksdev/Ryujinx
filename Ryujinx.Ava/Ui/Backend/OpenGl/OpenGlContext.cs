using SPB.Graphics.OpenGL;
using SPB.Platform;
using SPB.Platform.GLX;
using SPB.Platform.WGL;
using SPB.Windowing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ryujinx.Ava.Ui.Backend.OpenGl
{
    internal class OpenGlContext : IDisposable
    {
        public OpenGLContextBase BaseContext { get; }
        private static IntPtr _defaultDisplay = IntPtr.Zero;

        public static IntPtr DefaultDisplay => SPB.Platform.X11.X11.DefaultDisplay;

        public OpenGlContext()
        {
            BaseContext = PlatformHelper.CreateOpenGLContext(OpenGlSurface.GetFramebufferFormat(), 3, 2, OpenGLContextFlags.Compat);
            BaseContext.Initialize();
        }

        public void MakeCurrent(SwappableNativeWindowBase window)
        {
            if(window == null)
            {
                return;
            }

            Monitor.Enter(BaseContext);

            if(OperatingSystem.IsWindows())
            {
                if(window  is WGLWindow wGLWindow)
                {
                    MakeCurrent(wGLWindow.DisplayHandle.RawHandle, BaseContext.ContextHandle);
                }
            }
            else if(OperatingSystem.IsLinux())
            {
                BaseContext.MakeCurrent(window);
            }
        }

        public void ReleaseCurrent()
        {
            BaseContext.MakeCurrent(null);
            Monitor.Exit(BaseContext);
        }


        [DllImport("OPENGL32.DLL", EntryPoint = "wglMakeCurrent", SetLastError = true)]
        private extern static bool MakeCurrent(IntPtr hDc, IntPtr newContext);

        public void Dispose()
        {
            BaseContext?.Dispose();
            if (_defaultDisplay != IntPtr.Zero)
            {
                Interop.XCloseDisplay(_defaultDisplay);
            }
        }
    }
}
