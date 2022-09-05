using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using SPB.Platform.Win32;
using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Ryujinx.Ava.Ui.Controls
{
    public class EmbeddedWindow : NativeControlHost
    {
        protected IntPtr WindowHandle { get; set; }

        protected IntPtr X11Display{ get; set; }

        public event EventHandler<IntPtr> WindowCreated;

        public event EventHandler<Size> SizeChanged;

        protected virtual void OnWindowDestroyed() { }
        protected virtual void OnWindowDestroying() 
        {
            WindowHandle = IntPtr.Zero;
            X11Display = IntPtr.Zero;
        }

        public EmbeddedWindow()
        {
            var stateObserverable = this.GetObservable(Control.BoundsProperty);

            stateObserverable.Subscribe(StateChanged);

            this.Initialized += NativeEmbeddedWindow_Initialized;
        }

        public virtual void OnWindowCreated()
        {
        }

        private void NativeEmbeddedWindow_Initialized(object sender, EventArgs e)
        {
            Task.Run(() =>
            {
                OnWindowCreated();

                WindowCreated?.Invoke(this, WindowHandle);
            });
        }

        private void StateChanged(Rect rect)
        {
            SizeChanged?.Invoke(this, rect.Size);
        }

        protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return CreateLinux(parent);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return CreateWin32(parent);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return CreateOSX(parent);
            return base.CreateNativeControlCore(parent);
        }

        protected override void DestroyNativeControlCore(IPlatformHandle control)
        {
            OnWindowDestroying();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                DestroyLinux(control);
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                DestroyWin32(control);
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                DestroyOSX(control);
            else
                base.DestroyNativeControlCore(control);

            OnWindowDestroyed();
        }

        IPlatformHandle CreateLinux(IPlatformHandle parent)
        {
            var window = base.CreateNativeControlCore(parent);

            WindowHandle = window.Handle;

            BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.NonPublic;
            FieldInfo field = window.GetType().GetField("_display", bindFlags);
            var display = field.GetValue(window);

            X11Display = (IntPtr)display;

            return window;
        }

        IPlatformHandle CreateOSX(IPlatformHandle parent)
        {
            return null;
        }

        IPlatformHandle CreateWin32(IPlatformHandle parent)
        {
            WindowHandle = parent.Handle;

            return new PlatformHandle(WindowHandle, "HWND");
        }

        void DestroyLinux(IPlatformHandle handle)
        {
            base.DestroyNativeControlCore(handle);
        }

        void DestroyOSX(IPlatformHandle handle)
        {
        }

        [DllImport("user32.dll")]
        static extern bool DestroyWindow(IntPtr handle);

        void DestroyWin32(IPlatformHandle handle)
        {
            DestroyWindow(handle.Handle);
        }
    }
}