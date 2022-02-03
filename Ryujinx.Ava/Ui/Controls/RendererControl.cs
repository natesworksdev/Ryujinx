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
        protected int Image { get; private set; }
        public SwappableNativeWindowBase Window { get; private set; }

        public event EventHandler<EventArgs> GlInitialized;
        public event EventHandler<Size> SizeChanged;

        private bool _presented;
        private IntPtr _guiFence = IntPtr.Zero;
        private IntPtr _gameFence = IntPtr.Zero;

        protected Size RenderSize { get;private set; }

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
                if (_gameFence != IntPtr.Zero)
                {
                    GL.ClientWaitSync(_gameFence, ClientWaitSyncFlags.SyncFlushCommandsBit, Int64.MaxValue);
                    GL.DeleteSync(_gameFence);
                    _gameFence = IntPtr.Zero;
                }
                OnRender(gl, fb);

                _guiFence = GL.FenceSync(SyncCondition.SyncGpuCommandsComplete, WaitSyncFlags.None);

                _presented = true;
            }
        }

        protected abstract void OnRender(GlInterface gl, int fb);

        protected override void OnOpenGlDeinit(GlInterface gl, int fb)
        {
            base.OnOpenGlDeinit(gl, fb);

            if (_guiFence != IntPtr.Zero)
            {
                GL.DeleteSync(_guiFence);
            }

            if (_gameFence != IntPtr.Zero)
            {
                GL.DeleteSync(_gameFence);
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

        internal bool Present(int image)
        {
            if (_guiFence != IntPtr.Zero)
            {
                GL.ClientWaitSync(_guiFence, ClientWaitSyncFlags.SyncFlushCommandsBit, Int64.MaxValue);
                GL.DeleteSync(_guiFence);
                _guiFence = IntPtr.Zero;
            }
            
            bool returnValue = _presented;

            if (_presented)
            {
                lock (this)
                {
                    Image = image;
                    _presented = false;
                }
            }

            if (_gameFence != IntPtr.Zero)
            {
                GL.DeleteSync(_gameFence);
            }
            _gameFence = GL.FenceSync(SyncCondition.SyncGpuCommandsComplete, WaitSyncFlags.None);

            QueueRender();

            return returnValue;
        }
    }
}
