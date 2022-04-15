using Avalonia;
using Avalonia.Platform;
using Avalonia.Skia;
using Avalonia.X11;
using OpenTK.Graphics.OpenGL;
using Ryujinx.Ava.Ui.Controls;
using SkiaSharp;
using SPB.Graphics;
using SPB.Graphics.OpenGL;
using SPB.Platform;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Ryujinx.Ava.Ui.Backend.OpenGl
{
    public class OpenGlSkiaGpu : ISkiaGpu, IDisposable
    {
        private readonly long? _maxResourceBytes;
        private GRContext _grContext;
        private bool _initialized;
        private GRGlInterface _interface;

        public GRContext GrContext { get => _grContext; set => _grContext = value; }
        internal OpenGlContext PrimaryContext { get; }

        public OpenGlSkiaGpu(long? maxResourceBytes)
        {
            _maxResourceBytes = maxResourceBytes;

            // Load GL functions
            var window = PlatformHelper.CreateOpenGLWindow(FramebufferFormat.Default, 0, 0, 100, 100);
            window.Hide();
            var context = PlatformHelper.CreateOpenGLContext(FramebufferFormat.Default, 3, 2, OpenGLContextFlags.Compat);
            context.Initialize(window);

            context.MakeCurrent(window);
            GL.LoadBindings(new OpenToolkitBindingsContext(context.GetProcAddress));
            context.MakeCurrent(null);
            context.Dispose();
            window.Dispose();

            // Make Primary Context
            PrimaryContext = new OpenGlContext();

            AvaloniaLocator.CurrentMutable.Bind<OpenGLContextBase>().ToConstant(PrimaryContext.BaseContext);
            AvaloniaLocator.CurrentMutable.Bind<ISkiaGpu>().ToConstant(this);
        }

        private void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;

            _interface = GRGlInterface.Create();
            _grContext = GRContext.CreateGl(_interface, new GRContextOptions { AvoidStencilBuffers = true });
            if (_maxResourceBytes.HasValue)
            {
                _grContext.SetResourceCacheLimit(_maxResourceBytes.Value);
            }
        }

        public ISkiaGpuRenderTarget TryCreateRenderTarget(IEnumerable<object> surfaces)
        {
            foreach (var surface in surfaces)
            {
                OpenGlSurface window = null;

                if (surface is IPlatformHandle handle)
                {
                    window = new OpenGlSurface(handle.Handle);
                }
                else if (surface is X11FramebufferSurface x11FramebufferSurface)
                {
                    var xId = (IntPtr)x11FramebufferSurface.GetType().GetField("_xid", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(x11FramebufferSurface);

                    window = new OpenGlSurface(xId);
                }

                if (window == null)
                {
                    return null;
                }

                var openGlRenderTarget = new OpenGlRenderTarget(window);

                window.MakeCurrent();
                Initialize();
                window.ReleaseCurrent();

                openGlRenderTarget.GrContext = _grContext;

                return openGlRenderTarget;
            }

            return null;
        }

        public ISkiaSurface TryCreateSurface(PixelSize size, ISkiaGpuRenderSession session)
        {
            return null;
        }

        public void Dispose()
        {
            PrimaryContext?.Dispose();
        }
    }
}
