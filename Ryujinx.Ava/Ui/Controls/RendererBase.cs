using Avalonia;
using Avalonia.Controls;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using Avalonia.Platform;
using Avalonia.Threading;
using OpenTK.Graphics.OpenGL;
using SPB.Windowing;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ryujinx.Ava.Ui.Controls
{
    public abstract class RendererBase : OpenGlControlBase
    {
        protected int Image { get; private set; }
        public SwappableNativeWindowBase Window { get; private set; }

        public event EventHandler<EventArgs> GlInitialized;
        public event EventHandler<Size> SizeChanged;

        private IntPtr _waitFence = IntPtr.Zero;

        private ManualResetEventSlim _waitEvent;

        private CancellationToken _token;
        private CancellationTokenSource _tokenSource;

        protected Size RenderSize { get;private set; }

        public RendererBase()
        {
            IObservable<Rect> resizeObservable = this.GetObservable(BoundsProperty);

            resizeObservable.Subscribe(Resized);
            _waitEvent = new ManualResetEventSlim(false);

            Focusable = true;

            _tokenSource = new CancellationTokenSource();
            _token = _tokenSource.Token;
        }

        private void Resized(Rect rect)
        {
            SizeChanged?.Invoke(this, rect.Size);

            RenderSize = rect.Size * Program.WindowScaleFactor;
        }

        protected override void OnOpenGlInit(GlInterface gl, int fb)
        {
            base.OnOpenGlInit(gl, fb);

            if (OperatingSystem.IsWindows())
            {
                var window = ((this.VisualRoot as TopLevel).PlatformImpl as Avalonia.Win32.WindowImpl).Handle.Handle;

                Window = new SPB.Platform.WGL.WGLWindow(new NativeHandle(window));
            }
            else if (OperatingSystem.IsLinux())
            {
                var platform = (this.VisualRoot as TopLevel).PlatformImpl;
                var window = (IPlatformHandle)platform.GetType().GetProperty("Handle").GetValue(platform);
                var display = platform.GetType().GetField("_x11", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(platform);
                var displayHandle = (IntPtr)display.GetType().GetProperty("Display").GetValue(display);

                Window = new SPB.Platform.GLX.GLXWindow(new NativeHandle(displayHandle), new NativeHandle(window.Handle));
            }
        }

        protected override void OnOpenGlRender(GlInterface gl, int fb)
        {
            if (_waitFence != IntPtr.Zero)
            {
                GL.WaitSync(_waitFence, WaitSyncFlags.None, ulong.MaxValue);
                GL.DeleteSync(_waitFence);
                _waitFence = IntPtr.Zero;
            }

            OnRender(gl, fb);

            _waitEvent.Set();
        }

        protected abstract void OnRender(GlInterface gl, int fb);

        protected override void OnOpenGlDeinit(GlInterface gl, int fb)
        {
            base.OnOpenGlDeinit(gl, fb);

            if (_waitFence != IntPtr.Zero)
            {
                GL.DeleteSync(_waitFence);
                _waitFence = IntPtr.Zero;
            }
        }

        protected void OnInitialized(GlInterface gl)
        {
            GL.LoadBindings(new OpenToolkitBindingsContext(gl.GetProcAddress));
            GlInitialized?.Invoke(this, EventArgs.Empty);
        }

        public void QueueRender()
        {
            Dispatcher.UIThread.InvokeAsync(InvalidateVisual, DispatcherPriority.Render);
        }

        public void Continue()
        {
            _tokenSource.Cancel();
        }

        internal void Present(int image)
        {
            Image = image;
            _waitFence = GL.FenceSync(SyncCondition.SyncGpuCommandsComplete, WaitSyncFlags.None);
            _waitEvent.Reset();
            QueueRender();
            try
            {
                _waitEvent.Wait(16, _token);
            }
            catch(OperationCanceledException){
            
            }
        }
    }
}
