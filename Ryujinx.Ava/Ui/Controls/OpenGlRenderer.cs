using Avalonia;
using Avalonia.Controls;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using Avalonia.Platform;
using Avalonia.Win32;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using Ryujinx.Common.Configuration;
using SPB.Graphics;
using SPB.Graphics.OpenGL;
using SPB.Platform;
using SPB.Platform.Win32;
using SPB.Windowing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Ryujinx.Ava.Ui.Controls
{
    internal class OpenGlRenderer : RendererControl
    {
        private IntPtr _handle;
        private int _framebuffer;

        public int Major { get; }
        public int Minor { get; }
        public GraphicsDebugLevel DebugLevel { get; }
        public OpenGLContextBase GameContext { get; set; }
        private SwappableNativeWindowBase _gameBackgroundWindow;

        public OpenGLContextBase PrimaryContext =>
                AvaloniaLocator.Current.GetService<IPlatformOpenGlInterface>().PrimaryContext.AsOpenGLContextBase();

        public OpenGlRenderer(int major, int minor, GraphicsDebugLevel graphicsDebugLevel)
        {
            Major = major;
            Minor = minor;
            DebugLevel = graphicsDebugLevel;
        }

        public IGlContext GetControlContext()
        {
            var field = GetType().BaseType.BaseType.GetField("_context", BindingFlags.NonPublic | BindingFlags.Instance);

            return field.GetValue(this) as IGlContext;
        }

        protected override void OnOpenGlInit(GlInterface gl, int fb)
        {
            base.OnOpenGlInit(gl, fb);

            OpenGLContextBase mainContext = PrimaryContext;

            CreateWindow(mainContext);

            Window.SwapInterval = 0;

            OnInitialized(gl);

            _framebuffer = GL.GenFramebuffer();
        }

        protected override void OnRender(GlInterface gl, int fb)
        {
            if (GameContext == null || !IsStarted)
            {
                return;
            }

            int current_texture = GL.GetInteger(GetPName.Texture2D);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

            if (!IsThreaded)
            {
                GetControlContext().AsOpenGLContextBase().MakeCurrent(null);
                MakeCurrent();
            }

            CallRenderEvent();

            if (!IsThreaded)
            {
                MakeCurrent(null);
            }

            if (Image == 0)
            {
                return;
            }

            var disposableContext = !IsThreaded ? GetControlContext().EnsureCurrent() : null;

            GL.WaitSync(Fence, WaitSyncFlags.None, ulong.MaxValue);

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _framebuffer);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, Image, 0);

            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, _framebuffer);
            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, fb);
            GL.ReadBuffer(ReadBufferMode.ColorAttachment0);
            GL.BlitFramebuffer(0,
                               0,
                               (int)RenderSize.Width,
                               (int)RenderSize.Height,
                               0,
                               (int)RenderSize.Height,
                               (int)RenderSize.Width,
                               0,
                               ClearBufferMask.ColorBufferBit,
                               BlitFramebufferFilter.Linear);

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, fb);

            disposableContext?.Dispose();
        }

        protected override void OnOpenGlDeinit(GlInterface gl, int fb)
        {
            base.OnOpenGlDeinit(gl, fb);

            Window.SwapInterval = 1;
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

        private void CreateWindow(OpenGLContextBase mainContext)
        {
            var flags = OpenGLContextFlags.Compat;
            if(DebugLevel != GraphicsDebugLevel.None)
            {
                flags |= OpenGLContextFlags.Debug;
            }
            _gameBackgroundWindow = PlatformHelper.CreateOpenGLWindow(FramebufferFormat.Default, 0, 0, (int)Bounds.Width, (int)Bounds.Height);
            _gameBackgroundWindow.Hide();

            GameContext = PlatformHelper.CreateOpenGLContext(FramebufferFormat.Default, Major, Minor, flags, shareContext: mainContext);
            GameContext.Initialize(_gameBackgroundWindow);
        }
    }
}
