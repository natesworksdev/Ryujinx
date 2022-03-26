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
    public abstract class RendererControl : OpenGlControlBase
    {
        protected int Image { get; set; }
        public SwappableNativeWindowBase Window { get; private set; }

        public event EventHandler<EventArgs> GlInitialized;
        public event EventHandler<Size> SizeChanged;
        public event EventHandler Rendered;

        protected Size RenderSize { get;private set; }
        public bool IsStarted { get; private set; }

        protected IntPtr Fence { get; set; } = IntPtr.Zero;
        public bool IsThreaded { get; internal set; }

        private ManualResetEventSlim _preFrameResetEvent;
        private ManualResetEventSlim _postFrameResetEvent;

        public RendererControl()
        {
            IObservable<Rect> resizeObservable = this.GetObservable(BoundsProperty);

            resizeObservable.Subscribe(Resized);

            Focusable = true;
        }

        private void Resized(Rect rect)
        {
            SizeChanged?.Invoke(this, rect.Size);

            RenderSize = rect.Size * Program.WindowScaleFactor;
        }

        protected override void OnOpenGlInit(GlInterface gl, int fb)
        {
            _preFrameResetEvent = new ManualResetEventSlim(false);
            _postFrameResetEvent = new ManualResetEventSlim(false);
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
            lock (this)
            {
                OnRender(gl, fb);

                _postFrameResetEvent.Set();
            }
        }

        protected void CallRenderEvent()
        {
            _preFrameResetEvent.Reset();
            Rendered?.Invoke(this, EventArgs.Empty);
            _preFrameResetEvent.Wait();
        }

        public void Continue()
        {
            _preFrameResetEvent?.Set();
        }

        protected abstract void OnRender(GlInterface gl, int fb);

        protected override void OnOpenGlDeinit(GlInterface gl, int fb)
        {
            base.OnOpenGlDeinit(gl, fb);
            Continue();
            _preFrameResetEvent.Dispose();
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

        internal bool Present(int image)
        {
            Image = image;

            if(Fence != IntPtr.Zero)
            {
                GL.DeleteSync(Fence);
                Fence = IntPtr.Zero;
            }

            Fence = GL.FenceSync(SyncCondition.SyncGpuCommandsComplete, WaitSyncFlags.None);

            _postFrameResetEvent.Reset();

            Continue();

            if (IsThreaded)
            {
                _postFrameResetEvent.Wait();
            }

            return true;
        }

        internal void Start()
        {
            IsStarted = true;
            QueueRender();
        }

        internal void Stop()
        {
            Continue();
            IsStarted = false;
        }
    }
}
