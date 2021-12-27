using Gdk;
using Gtk;
using OpenTK.Graphics.OpenGL;
using Ryujinx.Common.Configuration;
using Ryujinx.Configuration;
using Ryujinx.Graphics.OpenGL;
using Ryujinx.Graphics.OpenGL.Helper;
using Ryujinx.Input.HLE;
using SPB.Graphics;
using SPB.Graphics.OpenGL;
using SPB.Platform;
using SPB.Platform.GLX;
using SPB.Platform.WGL;
using SPB.Windowing;
using System;
using System.Runtime.InteropServices;

namespace Ryujinx.Ui
{
    public class GlRenderer : RendererWidgetBase
    {
        private GraphicsDebugLevel _glLogLevel;

        private bool _initializedOpenGL;

        private OpenGLContextBase _gameContext;
        private OpenGLContextBase _renderContext;
        private SwappableNativeWindowBase _nativeWindow;
        private RenderTarget _stagingRenderTarget;

        public GlRenderer(InputManager inputManager, GraphicsDebugLevel glLogLevel) : base(inputManager, glLogLevel)
        {
            _glLogLevel = glLogLevel;
        }

        protected override bool OnDrawn(Cairo.Context cr)
        {
            if (!_initializedOpenGL)
            {
                IntializeOpenGL();
            }

            return true;
        }

        private void IntializeOpenGL()
        {
            _nativeWindow = RetrieveNativeWindow();

            Window.EnsureNative();

            _gameContext = PlatformHelper.CreateOpenGLContext(GetGraphicsMode(), 3, 3, _glLogLevel == GraphicsDebugLevel.None ? OpenGLContextFlags.Compat : OpenGLContextFlags.Compat | OpenGLContextFlags.Debug);
            _gameContext.Initialize(_nativeWindow);
            _gameContext.MakeCurrent(_nativeWindow);

            // Release the GL exclusivity that SPB gave us as we aren't going to use it in GTK Thread.
            _gameContext.MakeCurrent(null);

            _renderContext = PlatformHelper.CreateOpenGLContext(GetGraphicsMode(), 3, 3, _glLogLevel == GraphicsDebugLevel.None ? OpenGLContextFlags.Compat : OpenGLContextFlags.Compat | OpenGLContextFlags.Debug, true, _gameContext);
            _renderContext.Initialize(_nativeWindow);
            _renderContext.MakeCurrent(null);


            WaitEvent.Set();

            _initializedOpenGL = true;
        }

        private SwappableNativeWindowBase RetrieveNativeWindow()
        {
            if (OperatingSystem.IsWindows())
            {
                IntPtr windowHandle = gdk_win32_window_get_handle(Window.Handle);

                return new WGLWindow(new NativeHandle(windowHandle));
            }
            else if (OperatingSystem.IsLinux())
            {
                IntPtr displayHandle = gdk_x11_display_get_xdisplay(Display.Handle);
                IntPtr windowHandle = gdk_x11_window_get_xid(Window.Handle);

                return new GLXWindow(new NativeHandle(displayHandle), new NativeHandle(windowHandle));
            }

            throw new NotImplementedException();
        }

        [DllImport("libgdk-3-0.dll")]
        private static extern IntPtr gdk_win32_window_get_handle(IntPtr d);

        [DllImport("libgdk-3.so.0")]
        private static extern IntPtr gdk_x11_display_get_xdisplay(IntPtr gdkDisplay);

        [DllImport("libgdk-3.so.0")]
        private static extern IntPtr gdk_x11_window_get_xid(IntPtr gdkWindow);

        private static FramebufferFormat GetGraphicsMode()
        {
            return Environment.OSVersion.Platform == PlatformID.Unix ? new FramebufferFormat(new ColorFormat(8, 8, 8, 0), 16, 0, ColorFormat.Zero, 0, 2, false) : FramebufferFormat.Default;
        }

        public override void InitializeRenderer()
        {
            // First take exclusivity on the OpenGL context.
            ((Renderer)Renderer).InitializeBackgroundContext(SPBOpenGLContext.CreateBackgroundContext(_gameContext));

            _gameContext.MakeCurrent(_nativeWindow);

            _stagingRenderTarget = GLHelper.GenerateRenderTarget(AllocatedWidth, AllocatedHeight);

            SizeChanged = false;

            GL.ClearColor(0, 0, 0, 1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit);
            SwapBuffers(0);
        }

        public override void SwapBuffers(object framebuffer)
        {
            int boundFrameBuffer = (int)framebuffer;

            if (SizeChanged)
            {
                _stagingRenderTarget.Dispose();

                _stagingRenderTarget = GLHelper.GenerateRenderTarget(AllocatedWidth, AllocatedHeight);

                SizeChanged = false;
            }

            var blitRegion = new BlitRegion(0, 0, AllocatedWidth, AllocatedHeight);

            GLHelper.BlitFramebuffer(boundFrameBuffer, ConfigurationState.Instance.ShowOsd ? _stagingRenderTarget.Framebuffer : 0, blitRegion, blitRegion);

            if (ConfigurationState.Instance.ShowOsd)
            {
                _gameContext.MakeCurrent(null);
                _renderContext.MakeCurrent(_nativeWindow);

                Hud.RenderUi(_stagingRenderTarget.Texture);

                _renderContext.MakeCurrent(null);
                _gameContext.MakeCurrent(_nativeWindow);

                GLHelper.BlitFramebuffer(_stagingRenderTarget.Framebuffer, 0, blitRegion, blitRegion);
            }

            _nativeWindow.SwapBuffers();
        }

        public override string GetGpuVendorName()
        {
            return ((Renderer)Renderer).GpuVendor;
        }

        protected override void Dispose(bool disposing)
        {
            // Try to bind the OpenGL context before calling the shutdown event
            try
            {
                _gameContext?.MakeCurrent(_nativeWindow);
                
                _stagingRenderTarget.Dispose();
            }
            catch (Exception) { }

            Device.DisposeGpu();
            NpadManager.Dispose();

            // Unbind context and destroy everything
            try
            {
                _gameContext?.MakeCurrent(null);
                _renderContext?.MakeCurrent(null);
            }
            catch (Exception) { }

            _gameContext.Dispose();
            _renderContext.Dispose();
        }
    }
}
