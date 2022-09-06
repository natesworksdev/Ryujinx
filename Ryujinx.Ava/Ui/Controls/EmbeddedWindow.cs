using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform;
using PInvoke;
using SPB.Graphics;
using SPB.Platform;
using SPB.Platform.GLX;
using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using static PInvoke.User32;

namespace Ryujinx.Ava.Ui.Controls
{
    public unsafe class EmbeddedWindow : NativeControlHost
    {
        protected GLXWindow _bwindow;
        private WndProc _wndProcDelegate;
        private string _className;

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
            OnWindowCreated();

            Task.Run(() =>
            {
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
            {
                return CreateLinux(parent);
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return CreateWin32(parent);
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return CreateOSX(parent);
            }
            return base.CreateNativeControlCore(parent);
        }

        protected override void DestroyNativeControlCore(IPlatformHandle control)
        {
            OnWindowDestroying();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                DestroyLinux(control);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                DestroyWin32(control);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                DestroyOSX(control);
            }
            else
            {
                base.DestroyNativeControlCore(control);
            }

            OnWindowDestroyed();
        }

        [SupportedOSPlatform("linux")]
        IPlatformHandle CreateLinux(IPlatformHandle parent)
        {
            if (this is OpenGLEmbeddedWindow)
            {
                var window = PlatformHelper.CreateOpenGLWindow(FramebufferFormat.Default, 0, 0, 100, 100) as GLXWindow;

                WindowHandle = window.WindowHandle.RawHandle;

                X11Display = window.DisplayHandle.RawHandle;

                return new PlatformHandle(WindowHandle, "X11");
            }
            else 
            {
                var window = base.CreateNativeControlCore(parent);

                WindowHandle = window.Handle;

                BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.NonPublic;
                FieldInfo field = window.GetType().GetField("_display", bindFlags);
                var display = field.GetValue(window);

                X11Display = (IntPtr)display;

                return window;
            }
        }

        IPlatformHandle CreateOSX(IPlatformHandle parent)
        {
            return null;
        }

        unsafe IPlatformHandle CreateWin32(IPlatformHandle parent)
        {
            _className = "NativeWindow-" + Guid.NewGuid();
            _wndProcDelegate = WndProc;
            var wndClassEx = new WNDCLASSEX
            {
                cbSize = Marshal.SizeOf<WNDCLASSEX>(),
                hInstance = GetModuleHandle(null),
                lpfnWndProc = _wndProcDelegate,
                style = ClassStyles.CS_OWNDC
            };

            short atom = 0;

            fixed (char* c = _className)
            {
                wndClassEx.lpszClassName = c;
                atom = RegisterClassEx(ref wndClassEx);
            }

            var handle = CreateWindowEx(
                (WindowStylesEx)0,
                _className,
                "NativeWindow",
                WindowStyles.WS_CHILD,
                0,
                0,
                640,
                480,
                parent.Handle,
                IntPtr.Zero,
                IntPtr.Zero,
                IntPtr.Zero);

            WindowHandle = handle;

            return new PlatformHandle(WindowHandle, "HWND");
        }

        protected IntPtr WndProc(IntPtr hWnd, WindowMessage msg, void* wParam, void* lParam)
        {
            var point = new Point((int)lParam & 0xFFFF, ((int)lParam >> 16) & 0xFFFF);
            var root = this.VisualRoot as Window;
            bool isLeft = false;
            switch (msg)
            {
                case WindowMessage.WM_LBUTTONDOWN:
                case WindowMessage.WM_RBUTTONDOWN:
                    isLeft = msg == WindowMessage.WM_LBUTTONDOWN;
                    this.RaiseEvent(new PointerPressedEventArgs(
                        this,
                        new Avalonia.Input.Pointer(0, PointerType.Mouse, true),
                        root,
                        this.TranslatePoint(point, root).Value,
                        (ulong)Environment.TickCount64,
                        new PointerPointProperties(isLeft ? RawInputModifiers.LeftMouseButton : RawInputModifiers.RightMouseButton, isLeft ? PointerUpdateKind.LeftButtonPressed : PointerUpdateKind.RightButtonPressed),
                        KeyModifiers.None));
                    break;
                case WindowMessage.WM_LBUTTONUP:
                case WindowMessage.WM_RBUTTONUP:
                    isLeft = msg == WindowMessage.WM_LBUTTONUP;
                    this.RaiseEvent(new PointerReleasedEventArgs(
                        this,
                        new Avalonia.Input.Pointer(0, PointerType.Mouse, true),
                        root,
                        this.TranslatePoint(point, root).Value,
                        (ulong)Environment.TickCount64,
                        new PointerPointProperties(isLeft ? RawInputModifiers.LeftMouseButton : RawInputModifiers.RightMouseButton, isLeft ? PointerUpdateKind.LeftButtonReleased : PointerUpdateKind.RightButtonReleased),
                        KeyModifiers.None,
                        isLeft ? MouseButton.Left : MouseButton.Right));
                    break;
                case WindowMessage.WM_MOUSEMOVE:
                    this.RaiseEvent(new PointerEventArgs(
                        UserControl.PointerMovedEvent,
                        this,
                        new Avalonia.Input.Pointer(0, PointerType.Mouse, true),
                        root,
                        this.TranslatePoint(point, root).Value,
                        (ulong)Environment.TickCount64,
                        new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.Other),
                        KeyModifiers.None));
                    SetCursor(LoadCursor(IntPtr.Zero, (IntPtr)Cursors.IDC_ARROW));
                    break;
            }
            return DefWindowProc(hWnd, msg, (IntPtr)wParam, (IntPtr)lParam);
        }

        void DestroyLinux(IPlatformHandle handle)
        {
            if (this is not OpenGLEmbeddedWindow)
            {
                base.DestroyNativeControlCore(handle);
            }
        }

        void DestroyOSX(IPlatformHandle handle)
        {
        }

        [DllImport("kernel32.dll")]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        public static extern IntPtr SetLayeredWindowAttributes(IntPtr hwnd, long crKey, long bAlpha, long dwFlags);

        void DestroyWin32(IPlatformHandle handle)
        {
            DestroyWindow(handle.Handle);
            UnregisterClass(_className, GetModuleHandle(null));
        }
    }
}