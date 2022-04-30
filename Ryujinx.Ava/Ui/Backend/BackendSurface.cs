using Avalonia;
using System;

using static Ryujinx.Ava.Ui.Backend.Interop;

namespace Ryujinx.Ava.Ui.Backend
{
    public abstract class BackendSurface : IDisposable
    {
        private PixelSize _currentSize;
        public IntPtr Handle { get; protected set; }

        public bool IsDisposed { get; private set; }

        public BackendSurface(IntPtr handle)
        {
            Handle = handle;
        }

        public PixelSize Size
        {
            get
            {
                PixelSize size = new PixelSize();
                if (OperatingSystem.IsWindows())
                {
                    GetClientRect(Handle, out var rect);
                    size = new PixelSize(rect.right, rect.bottom);
                }
                else if (OperatingSystem.IsLinux())
                {
                    XWindowAttributes attributes = new XWindowAttributes();
                    XGetWindowAttributes(OpenGL.OpenGLContext.X11DefaultDisplay, Handle, ref attributes);

                    size = new PixelSize(attributes.width, attributes.height);
                }

                _currentSize = size.Width <= 0 || size.Height <= 0 ? _currentSize : size;

                return _currentSize;
            }
        }

        public PixelSize CurrentSize => _currentSize;

        public virtual void Dispose()
        {
            IsDisposed = true;
        }
    }
}