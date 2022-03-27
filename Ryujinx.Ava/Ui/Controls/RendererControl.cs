using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using Avalonia.Threading;
using OpenTK.Graphics.OpenGL;
using Ryujinx.Common.Configuration;
using SkiaSharp;
using SPB.Graphics;
using SPB.Graphics.OpenGL;
using SPB.Platform;
using SPB.Windowing;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ryujinx.Ava.Ui.Controls
{
    public class RendererControl : Control
    {
        protected int Image { get; set; }
        public SwappableNativeWindowBase Window { get; private set; }

        public event EventHandler<EventArgs> GlInitialized;
        public event EventHandler<Size> SizeChanged;
        public event EventHandler Rendered;

        protected Size RenderSize { get;private set; }
        public bool IsStarted { get; private set; }

        public int Major { get; }
        public int Minor { get; }
        public GraphicsDebugLevel DebugLevel { get; }
        public OpenGLContextBase GameContext { get; set; }

        public OpenGLContextBase PrimaryContext =>
                AvaloniaLocator.Current.GetService<IPlatformOpenGlInterface>().PrimaryContext.AsOpenGLContextBase();

        private ManualResetEventSlim _preFrameResetEvent;
        private ManualResetEventSlim _postFrameResetEvent;
        private SwappableNativeWindowBase _gameBackgroundWindow;

        private bool _isInitialized;

        public RendererControl(int major, int minor, GraphicsDebugLevel graphicsDebugLevel)
        {
            Major = major;
            Minor = minor;
            DebugLevel = graphicsDebugLevel;
            IObservable<Rect> resizeObservable = this.GetObservable(BoundsProperty);

            resizeObservable.Subscribe(Resized);

            Focusable = true;
        }

        private void Resized(Rect rect)
        {
            SizeChanged?.Invoke(this, rect.Size);

            RenderSize = rect.Size * Program.WindowScaleFactor;
        }

        public override void Render(DrawingContext context)
        {
            if (!_isInitialized)
            {
                CreateWindow();

                OnGlInitialized();

                Window.SwapInterval = 0;
                _isInitialized = true;
            }

            if (GameContext == null || !IsStarted || Image == 0)
            {
                _preFrameResetEvent.Set();
                return;
            }

            context.Custom(new GlDrawOperation(this, Image, new Rect(new Point(), RenderSize), new Rect(new Point(), RenderSize)));
            base.Render(context);

            _postFrameResetEvent.Set();
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            _preFrameResetEvent?.Set();
            _preFrameResetEvent?.Dispose();
            _postFrameResetEvent?.Set();
            _postFrameResetEvent?.Dispose();

            Window.SwapInterval = 1;
            base.OnDetachedFromVisualTree(e);
        }

        protected void OnGlInitialized()
        {
            _preFrameResetEvent = new ManualResetEventSlim(false);
            _postFrameResetEvent = new ManualResetEventSlim(false);

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

            MakeCurrent();
            GL.LoadBindings(new OpenToolkitBindingsContext(GameContext.GetProcAddress));
            GlInitialized?.Invoke(this, EventArgs.Empty);
            MakeCurrent(null);
        }

        public void QueueRender()
        {
            Dispatcher.UIThread.InvokeAsync(InvalidateVisual, DispatcherPriority.Render);
        }

        internal bool Present(int image)
        {
            Image = image;

            QueueRender();

            _preFrameResetEvent.Reset();

            return true;
        }

        public void OnPreFrame()
        {
            _preFrameResetEvent.Wait();
        }

        public void Wait()
        {
            _gameBackgroundWindow?.SwapBuffers();
        }

        internal void Start()
        {
            IsStarted = true;
            QueueRender();
        }

        internal void Stop()
        {
            IsStarted = false;
        }

        public async Task DestroyBackgroundContext()
        {
            await Task.Delay(1000);
            // WGL hangs here when disposing context
            //Context?.Dispose();
            _gameBackgroundWindow?.Dispose();
        }

        internal void MakeCurrent()
        {
            GameContext.MakeCurrent(_gameBackgroundWindow);
        }
        internal void MakeCurrent(SwappableNativeWindowBase window)
        {
            GameContext.MakeCurrent(window);
        }

        protected void CreateWindow()
        {
            var flags = OpenGLContextFlags.Compat;
            if(DebugLevel != GraphicsDebugLevel.None)
            {
                flags |= OpenGLContextFlags.Debug;
            }
            _gameBackgroundWindow = PlatformHelper.CreateOpenGLWindow(FramebufferFormat.Default, 0, 0, (int)Bounds.Width, (int)Bounds.Height);
            _gameBackgroundWindow.Hide();

            GameContext = PlatformHelper.CreateOpenGLContext(FramebufferFormat.Default, Major, Minor, flags, shareContext: PrimaryContext);
            GameContext.Initialize(_gameBackgroundWindow);
        }

        private class GlDrawOperation : ICustomDrawOperation
        {
            private int _texture;
            private int _framebuffer;

            public Rect Bounds => _control.Bounds;

            private readonly RendererControl _control;
            private readonly Rect _srcRect;
            private readonly Rect _dstRect;

            public GlDrawOperation(RendererControl control, int texture, Rect srcRect, Rect dstRect)
            {
                _control = control;
                _srcRect = srcRect;
                _dstRect = dstRect;
                _texture = texture;
            }

            public void Dispose()
            {
                GL.DeleteFramebuffer(_framebuffer);
            }

            public bool Equals(ICustomDrawOperation other)
            {
                return other is GlDrawOperation operation && _texture == operation._texture && Equals(this, other);
            }

            public bool HitTest(Point p)
            {
                return Bounds.Contains(p);
            }

            private void CreateRenderTarget()
            {
                _framebuffer = GL.GenFramebuffer();

                int currentFramebuffer = GL.GetInteger(GetPName.FramebufferBinding);

                GL.BindFramebuffer(FramebufferTarget.Framebuffer, _framebuffer);
                GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, _texture, 0);
                GL.BindFramebuffer( FramebufferTarget.Framebuffer, currentFramebuffer);
            }

            public void Render(IDrawingContextImpl context)
            {
                if (_texture == 0)
                    return;

                if (_framebuffer == 0)
                {
                    CreateRenderTarget();
                }

                if (context is not ISkiaDrawingContextImpl skiaDrawingContextImpl)
                    return;

                var imageInfo = new SKImageInfo((int)Bounds.Width, (int)Bounds.Height, SKColorType.Rgba8888);
                var glInfo = new GRGlFramebufferInfo((uint)_framebuffer, SKColorType.Rgba8888.ToGlSizedFormat());

                var stencils = GL.GetInteger(GetPName.StencilBits);

                using (var backendTexture = new GRBackendRenderTarget(imageInfo.Width, imageInfo.Height, 1, stencils, glInfo))
                using (var surface = SKSurface.Create(skiaDrawingContextImpl.GrContext, backendTexture,
                    GRSurfaceOrigin.BottomLeft, SKColorType.Rgba8888))
                {
                    if (surface == null)
                        return;

                    using (var snapshot = surface.Snapshot())
                        skiaDrawingContextImpl.SkCanvas.DrawImage(snapshot, _srcRect.ToSKRect(), _dstRect.ToSKRect(), new SKPaint());

                    _control._preFrameResetEvent.Set();
                }
            }
        }
    }
}
