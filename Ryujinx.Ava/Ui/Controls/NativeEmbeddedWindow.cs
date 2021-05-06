using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Platform;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Ryujinx.Ava.Input.Glfw;
using Ryujinx.Ava.Ui.Windows;
using Ryujinx.Common.Configuration;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using KeyModifiers = Avalonia.Input.KeyModifiers;
using TextInputEventArgs = OpenTK.Windowing.Common.TextInputEventArgs;
using Window = OpenTK.Windowing.GraphicsLibraryFramework.Window;

namespace Ryujinx.Ava.Ui.Controls
{
    public class NativeEmbeddedWindow : NativeControlHost
    {
        private static bool _glfwInitialized;

        public event EventHandler<KeyEventArgs> KeyPressed;
        public event EventHandler<KeyEventArgs> KeyReleased;
        public event EventHandler<TextInputEventArgs> TextInput;
        public event EventHandler<MouseButtonEventArgs> MouseDown;
        public event EventHandler<MouseButtonEventArgs> MouseUp;
        public event EventHandler<(double X, double Y)> MouseMove;
        public event EventHandler<IntPtr> WindowCreated;
        public event EventHandler<Size> SizeChanged;

        private bool _init;
        private bool _isFullScreen;
        private double _scale;

        protected int Major { get; init; }
        protected int Minor { get; init; }
        protected GraphicsDebugLevel DebugLevel { get; init; }

        protected IntPtr WindowHandle { get; set; }
        protected IntPtr X11Display { get; set; }

        public GameWindow GlfwWindow { get; private set; }

        private IPlatformHandle _handle;

        public bool RendererFocused => GlfwWindow.IsFocused;

        public NativeEmbeddedWindow(double scale)
        {
            _scale = scale;

            IObservable<Rect> stateObservable = this.GetObservable(BoundsProperty);
            HorizontalAlignment = HorizontalAlignment.Stretch;
            VerticalAlignment = VerticalAlignment.Stretch;
            Margin = new Thickness();

            stateObservable.Subscribe(StateChanged);

            IObservable<Rect> resizeObservable = this.GetObservable(BoundsProperty);

            resizeObservable.Subscribe(Resized);

            if(!_glfwInitialized)
            {
                _glfwInitialized = true;

                GLFW.Init();
            }
        }

        private void Resized(Rect rect)
        {
            SizeChanged?.Invoke(this, rect.Size);
        }

        public unsafe bool IsFullscreen
        {
            get
            {
                return _isFullScreen;
            }
            set
            {
                if (_isFullScreen != value)
                {
                    _isFullScreen = value;

                    UpdateSizes(_scale);
                }
            }
        }

        private unsafe void UpdateSizes(double scale)
        {
            _scale = scale;

            if (GlfwWindow != null)
            {
                if (!_isFullScreen)
                {
                    GlfwWindow.Size = new Vector2i((int)(Bounds.Width * scale), (int)(Bounds.Height * scale));
                }
                else
                {
                    var mode = GLFW.GetVideoMode(GLFW.GetPrimaryMonitor());

                    GlfwWindow.Size = new Vector2i(mode->Width, mode->Height);

                    if (VisualRoot != null)
                    {
                        var position = this.PointToScreen(Bounds.Position);
                        GlfwWindow.Location = new Vector2i(position.X, position.Y);
                    }
                }
                
                Focus();
            }
        }

        protected virtual void OnWindowDestroyed() { }

        protected virtual void OnWindowDestroying()
        {
            WindowHandle = IntPtr.Zero;
            X11Display = IntPtr.Zero;
        }

        public virtual void OnWindowCreated() { }

        protected override void OnGotFocus(GotFocusEventArgs e)
        {
            base.OnGotFocus(e);
            
            GlfwWindow.Focus();
        }

        public void Start()
        {
            if (OperatingSystem.IsLinux())
            {
                _handle = CreateLinux();
            }
            else if (OperatingSystem.IsWindows())
            {
                _handle = CreateWin32();
            }
        }

        public void Destroy()
        {
            OnWindowDestroying();

            Task.Run(async () =>
            {
                if (OperatingSystem.IsLinux())
                {
                    // Delay deleting the actual window, because avalonia does not release it early enough on Linux.
                    await Task.Delay(2000);
                }

                GlfwWindow.Dispose();

                OnWindowDestroyed();
            });
        }

        public void SetCursor(bool isInvisible)
        {
            GlfwWindow.CursorVisible = !isInvisible;
        }

        private async void StateChanged(Rect rect)
        {
            SizeChanged?.Invoke(this, rect.Size);

            if (!_init && WindowHandle != IntPtr.Zero && rect.Size != default)
            {
                _init = true;
                await Task.Run(() =>
                {
                    OnWindowCreated();

                    WindowCreated?.Invoke(this, WindowHandle);
                });
            }
        }

        protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
        {
            try
            {
                if (_handle != null)
                {
                    return _handle;
                }
            }
            finally
            {
                Task.Run(ProcessWindowEvents);
            }

            return base.CreateNativeControlCore(parent);
        }

        protected override void DestroyNativeControlCore(IPlatformHandle control) { }

        private unsafe IPlatformHandle CreateLinux()
        {
            CreateWindow();

            WindowHandle = (IntPtr)GLFW.GetX11Window(GlfwWindow.WindowPtr);
            X11Display = (IntPtr)GlfwGetX11Display(GlfwWindow.WindowPtr);

            return new PlatformHandle(WindowHandle, "X11");
        }

        private unsafe IPlatformHandle CreateWin32()
        {
            CreateWindow();

            WindowHandle = GLFW.GetWin32Window(GlfwWindow.WindowPtr);

            return new PlatformHandle(WindowHandle, "HWND");
        }

        [DllImport("libglfw.so.3", EntryPoint = "glfwGetX11Display")]
        public static extern unsafe uint GlfwGetX11Display(Window* window);

        private unsafe void CreateWindow()
        {
            ContextFlags flags = DebugLevel != GraphicsDebugLevel.None
                ? ContextFlags.Debug
                : ContextFlags.ForwardCompatible;
            flags |= ContextFlags.ForwardCompatible;
            GlfwWindow = new GameWindow(
                new GameWindowSettings {IsMultiThreaded = true, RenderFrequency = 60, UpdateFrequency = 60},
                new NativeWindowSettings
                {
                    API = this is OpenGlEmbeddedWindow ? ContextAPI.OpenGL : ContextAPI.NoAPI,
                    APIVersion = new Version(Major, Minor),
                    Profile = ContextProfile.Core,
                    IsEventDriven = true,
                    Flags = flags,
                    AutoLoadBindings = false,
                    Size = new Vector2i(200, 200),
                    StartVisible = false,
                    Title = "Renderer"
                });

            GlfwWindow.WindowBorder = WindowBorder.Hidden;

            GLFW.MakeContextCurrent(null);

            GlfwWindow.MouseDown += Window_MouseDown;
            GlfwWindow.MouseUp += Window_MouseUp;
            GlfwWindow.MouseMove += Window_MouseMove;
            GlfwWindow.TextInput += GlfwWindow_TextInput;

            // Glfw Mouse Passthrough doesn't work on linux, so we pass events to the keyboard driver the hard way
            GlfwWindow.KeyDown += Window_KeyDown;
            GlfwWindow.KeyUp += Window_KeyUp;
        }

        private void GlfwWindow_TextInput(TextInputEventArgs obj)
        {
            TextInput?.Invoke(this, obj);
        }

        private void Window_KeyUp(KeyboardKeyEventArgs obj)
        {
            GlfwKey key = Enum.Parse<GlfwKey>(obj.Key.ToString());
            KeyEventArgs keyEvent = new() {Key = (Key)key};
            
            keyEvent.KeyModifiers |= obj.Alt     ? KeyModifiers.Alt     : keyEvent.KeyModifiers;
            keyEvent.KeyModifiers |= obj.Control ? KeyModifiers.Control : keyEvent.KeyModifiers;
            keyEvent.KeyModifiers |= obj.Shift   ? KeyModifiers.Shift   : keyEvent.KeyModifiers;
            keyEvent.KeyModifiers |= obj.Command ? KeyModifiers.Meta    : keyEvent.KeyModifiers;

            KeyReleased?.Invoke(this, keyEvent);
        }

        private void Window_KeyDown(KeyboardKeyEventArgs obj)
        {
            GlfwKey key = Enum.Parse<GlfwKey>(obj.Key.ToString());
            KeyEventArgs keyEvent = new() {Key = (Key)key};
            
            keyEvent.KeyModifiers |= obj.Alt     ? KeyModifiers.Alt     : keyEvent.KeyModifiers;
            keyEvent.KeyModifiers |= obj.Control ? KeyModifiers.Control : keyEvent.KeyModifiers;
            keyEvent.KeyModifiers |= obj.Shift   ? KeyModifiers.Shift   : keyEvent.KeyModifiers;
            keyEvent.KeyModifiers |= obj.Command ? KeyModifiers.Meta    : keyEvent.KeyModifiers;

            KeyPressed?.Invoke(this, keyEvent);
        }

        public void ProcessWindowEvents()
        {
            while (WindowHandle != IntPtr.Zero)
            {
                GlfwWindow.ProcessEvents();
            }
        }

        private void Window_MouseMove(MouseMoveEventArgs obj)
        {
            MouseMove?.Invoke(this, (obj.X, obj.Y));
        }

        private void Window_MouseUp(MouseButtonEventArgs obj)
        {
            MouseUp?.Invoke(this, obj);
        }

        private void Window_MouseDown(MouseButtonEventArgs obj)
        {
            MouseDown?.Invoke(this, obj);
        }

        public virtual void Present() { }
    }
}