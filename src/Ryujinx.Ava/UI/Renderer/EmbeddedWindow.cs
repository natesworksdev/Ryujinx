using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform;
using Ryujinx.Common.Configuration;
using Ryujinx.Ui.Common.Configuration;
using Ryujinx.Ui.Common.Helper;
using SPB.Graphics;
using SPB.Platform;
using SPB.Platform.GLX;
using SPB.Platform.X11;
using SPB.Windowing;
using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using static Ryujinx.Ava.UI.Helpers.Win32NativeInterop;
using CursorStates = Ryujinx.Ava.Input.AvaloniaMouse.CursorStates;

namespace Ryujinx.Ava.UI.Renderer
{
    public class EmbeddedWindow : NativeControlHost
    {
        private WindowProc _wndProcDelegate;
        private string _className;

        protected GLXWindow X11Window { get; set; }

        protected IntPtr WindowHandle { get; set; }
        protected IntPtr X11Display { get; set; }
        protected IntPtr NsView { get; set; }
        protected IntPtr MetalLayer { get; set; }

        public delegate void UpdateBoundsCallbackDelegate(Rect rect);
        private UpdateBoundsCallbackDelegate _updateBoundsCallback;

        public event EventHandler<IntPtr> WindowCreated;
        public event EventHandler<Size> SizeChanged;

        [SupportedOSPlatform("windows")]
        private readonly IntPtr InvisibleCursorWin = CreateEmptyCursor();
        [SupportedOSPlatform("windows")]
        private readonly IntPtr DefaultCursorWin = CreateArrowCursor();
        [SupportedOSPlatform("windows")]
        private CursorStates _cursorState = !ConfigurationState.Instance.Hid.EnableMouse.Value ?
                                             CursorStates.CursorIsVisible : CursorStates.CursorIsHidden;
        [SupportedOSPlatform("windows")]
        private bool _trackingCursor = false;
        [SupportedOSPlatform("windows")]
        private TRACKMOUSEEVENT track = TRACKMOUSEEVENT.Empty;

        public EmbeddedWindow()
        {
            this.GetObservable(BoundsProperty).Subscribe(StateChanged);

            Initialized += OnNativeEmbeddedWindowCreated;
        }

        public virtual void OnWindowCreated() { }

        protected virtual void OnWindowDestroyed() { }

        protected virtual void OnWindowDestroying()
        {
            WindowHandle = IntPtr.Zero;
            X11Display = IntPtr.Zero;
            NsView = IntPtr.Zero;
            MetalLayer = IntPtr.Zero;
        }

        private void OnNativeEmbeddedWindowCreated(object sender, EventArgs e)
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
            _updateBoundsCallback?.Invoke(rect);
        }

        protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle control)
        {
            if (OperatingSystem.IsLinux())
            {
                return CreateLinux(control);
            }

            if (OperatingSystem.IsWindows())
            {
                return CreateWin32(control);
            }

            if (OperatingSystem.IsMacOS())
            {
                return CreateMacOS();
            }

            return base.CreateNativeControlCore(control);
        }

        protected override void DestroyNativeControlCore(IPlatformHandle control)
        {
            OnWindowDestroying();

            if (OperatingSystem.IsLinux())
            {
                DestroyLinux();
            }
            else if (OperatingSystem.IsWindows())
            {
                DestroyWin32(control);
            }
            else if (OperatingSystem.IsMacOS())
            {
                DestroyMacOS();
            }
            else
            {
                base.DestroyNativeControlCore(control);
            }

            OnWindowDestroyed();
        }

        [SupportedOSPlatform("linux")]
        private IPlatformHandle CreateLinux(IPlatformHandle control)
        {
            if (ConfigurationState.Instance.Graphics.GraphicsBackend.Value == GraphicsBackend.Vulkan)
            {
                X11Window = new GLXWindow(new NativeHandle(X11.DefaultDisplay), new NativeHandle(control.Handle));
                X11Window.Hide();
            }
            else
            {
                X11Window = PlatformHelper.CreateOpenGLWindow(new FramebufferFormat(new ColorFormat(8, 8, 8, 0), 16, 0, ColorFormat.Zero, 0, 2, false), 0, 0, 100, 100) as GLXWindow;
            }

            WindowHandle = X11Window.WindowHandle.RawHandle;
            X11Display = X11Window.DisplayHandle.RawHandle;

            return new PlatformHandle(WindowHandle, "X11");
        }

        [SupportedOSPlatform("windows")]
        IPlatformHandle CreateWin32(IPlatformHandle control)
        {
            _className = "NativeWindow-" + Guid.NewGuid();

            _wndProcDelegate = delegate (IntPtr hWnd, WindowsMessages msg, IntPtr wParam, IntPtr lParam)
            {
                if (VisualRoot != null)
                {
                    if (msg == WindowsMessages.Lbuttondown ||
                        msg == WindowsMessages.Rbuttondown ||
                        msg == WindowsMessages.Lbuttonup ||
                        msg == WindowsMessages.Rbuttonup ||
                        msg == WindowsMessages.Mousemove ||
                        msg == WindowsMessages.Mouseleave ||
                        msg == WindowsMessages.Setcursor)
                    {
                        var _x = ((long)lParam & 0xFFFF) / Program.WindowScaleFactor;
                        var _y = ((long)lParam >> 16 & 0xFFFF) / Program.WindowScaleFactor;
                        Point rootVisualPosition = this.TranslatePoint(new Point(_x, _y), VisualRoot).Value;
                        Pointer pointer = new(0, PointerType.Mouse, true);
                        IntPtr activeWindow = GetActiveWindow();

                        switch (msg)
                        {
                            case WindowsMessages.Lbuttondown:
                            case WindowsMessages.Rbuttondown:
                                {
                                    bool isLeft = msg == WindowsMessages.Lbuttondown;
                                    RawInputModifiers pointerPointModifier = isLeft ? RawInputModifiers.LeftMouseButton : RawInputModifiers.RightMouseButton;
                                    PointerPointProperties properties = new(pointerPointModifier, isLeft ? PointerUpdateKind.LeftButtonPressed : PointerUpdateKind.RightButtonPressed);

                                    var evnt = new PointerPressedEventArgs(
                                        this,
                                        pointer,
                                        VisualRoot,
                                        rootVisualPosition,
                                        (ulong)Environment.TickCount64,
                                        properties,
                                        KeyModifiers.None);

                                    RaiseEvent(evnt);

                                    break;
                                }
                            case WindowsMessages.Lbuttonup:
                            case WindowsMessages.Rbuttonup:
                                {
                                    bool isLeft = msg == WindowsMessages.Lbuttonup;
                                    RawInputModifiers pointerPointModifier = isLeft ? RawInputModifiers.LeftMouseButton : RawInputModifiers.RightMouseButton;
                                    PointerPointProperties properties = new(pointerPointModifier, isLeft ? PointerUpdateKind.LeftButtonReleased : PointerUpdateKind.RightButtonReleased);

                                    var evnt = new PointerReleasedEventArgs(
                                        this,
                                        pointer,
                                        VisualRoot,
                                        rootVisualPosition,
                                        (ulong)Environment.TickCount64,
                                        properties,
                                        KeyModifiers.None,
                                        isLeft ? MouseButton.Left : MouseButton.Right);

                                    RaiseEvent(evnt);

                                    break;
                                }
                            case WindowsMessages.Mousemove:
                                {
                                    if (!_trackingCursor)
                                    {
                                        _trackingCursor = true;
                                        _cursorState = CursorStates.ForceChangeCursor;

                                        track.hwndTrack = hWnd;
                                        track.cbSize = (uint)Marshal.SizeOf(track);
                                        track.dwFlags = WindowsMessages.Cursorleave;
                                        TrackMouseEvent(ref track);
                                    }

                                    var evnt = new PointerEventArgs(
                                        PointerMovedEvent,
                                        this,
                                        pointer,
                                        VisualRoot,
                                        rootVisualPosition,
                                        (ulong)Environment.TickCount64,
                                        new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.Other),
                                        KeyModifiers.None);

                                    RaiseEvent(evnt);

                                    break;
                                }
                            case WindowsMessages.Mouseleave:
                                {
                                    if (_trackingCursor)
                                        _trackingCursor = false;

                                    break;
                                }
                            case WindowsMessages.Setcursor:
                                {
                                    if (activeWindow != 0)
                                    {
                                        if (ConfigurationState.Instance.Hid.EnableMouse.Value)
                                        {
                                            if (ConfigurationState.Instance.HideCursor.Value == HideCursorMode.Never)
                                            {
                                                if (_cursorState != CursorStates.CursorIsVisible)
                                                {
                                                    SetCursor(DefaultCursorWin);
                                                    _cursorState = CursorStates.CursorIsVisible;
                                                }
                                            }
                                            else if (_cursorState != CursorStates.CursorIsHidden)
                                            {
                                                SetCursor(InvisibleCursorWin);
                                                _cursorState = CursorStates.CursorIsHidden;
                                            }
                                        }
                                        else
                                        {
                                            if (ConfigurationState.Instance.HideCursor.Value == HideCursorMode.Always)
                                            {
                                                if (_cursorState != CursorStates.CursorIsHidden)
                                                {
                                                    SetCursor(InvisibleCursorWin);
                                                    _cursorState = CursorStates.CursorIsHidden;
                                                }
                                            }
                                            else if (_cursorState != CursorStates.CursorIsVisible)
                                            {
                                                SetCursor(DefaultCursorWin);
                                                _cursorState = CursorStates.CursorIsVisible;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (_cursorState != CursorStates.CursorIsVisible)
                                        {
                                            SetCursor(DefaultCursorWin);
                                            _cursorState = CursorStates.CursorIsVisible;
                                        }
                                    }

                                    return 1;
                                }
                        }
                    }
                }

                return DefWindowProc(hWnd, msg, wParam, lParam);
            };

            WndClassEx wndClassEx = new()
            {
                cbSize = Marshal.SizeOf<WndClassEx>(),
                hInstance = GetModuleHandle(null),
                lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_wndProcDelegate),
                style = ClassStyles.CsOwndc,
                lpszClassName = Marshal.StringToHGlobalUni(_className),
                hCursor = DefaultCursorWin
            };

            RegisterClassEx(ref wndClassEx);

            WindowHandle = CreateWindowEx(0, _className, "NativeWindow", WindowStyles.WsChild, 0, 0, 640, 480, control.Handle, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);

            Marshal.FreeHGlobal(wndClassEx.lpszClassName);

            return new PlatformHandle(WindowHandle, "HWND");
        }

        [SupportedOSPlatform("macos")]
        IPlatformHandle CreateMacOS()
        {
            // Create a new CAMetalLayer.
            ObjectiveC.Object layerObject = new("CAMetalLayer");
            ObjectiveC.Object metalLayer = layerObject.GetFromMessage("alloc");
            metalLayer.SendMessage("init");

            // Create a child NSView to render into.
            ObjectiveC.Object nsViewObject = new("NSView");
            ObjectiveC.Object child = nsViewObject.GetFromMessage("alloc");
            child.SendMessage("init", new ObjectiveC.NSRect(0, 0, 0, 0));

            // Make its renderer our metal layer.
            child.SendMessage("setWantsLayer:", 1);
            child.SendMessage("setLayer:", metalLayer);
            metalLayer.SendMessage("setContentsScale:", Program.DesktopScaleFactor);

            // Ensure the scale factor is up to date.
            _updateBoundsCallback = rect =>
            {
                metalLayer.SendMessage("setContentsScale:", Program.DesktopScaleFactor);
            };

            IntPtr nsView = child.ObjPtr;
            MetalLayer = metalLayer.ObjPtr;
            NsView = nsView;

            return new PlatformHandle(nsView, "NSView");
        }

        [SupportedOSPlatform("Linux")]
        void DestroyLinux()
        {
            X11Window?.Dispose();
        }

        [SupportedOSPlatform("windows")]
        void DestroyWin32(IPlatformHandle handle)
        {
            DestroyWindow(handle.Handle);
            UnregisterClass(_className, GetModuleHandle(null));
        }

        [SupportedOSPlatform("macos")]
#pragma warning disable CA1822 // Mark member as static
        void DestroyMacOS()
        {
            // TODO
        }
#pragma warning restore CA1822
    }
}
