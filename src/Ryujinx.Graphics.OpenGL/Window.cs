using Ryujinx.Graphics.GAL;
using Ryujinx.Graphics.OpenGL.Effects;
using Ryujinx.Graphics.OpenGL.Effects.Smaa;
using Ryujinx.Graphics.OpenGL.Image;
using Silk.NET.OpenGL.Legacy;
using System;

namespace Ryujinx.Graphics.OpenGL
{
    class Window : IWindow, IDisposable
    {
        private readonly OpenGLRenderer _gd;

        private bool _initialized;

        private int _width;
        private int _height;
        private bool _updateSize;
        private uint _copyFramebufferHandle;
        private IPostProcessingEffect _antiAliasing;
        private IScalingFilter _scalingFilter;
        private bool _isLinear;
        private AntiAliasing _currentAntiAliasing;
        private bool _updateEffect;
        private ScalingFilter _currentScalingFilter;
        private float _scalingFilterLevel;
        private bool _updateScalingFilter;
        private bool _isBgra;
        private TextureView _upscaledTexture;

        internal BackgroundContextWorker BackgroundContext { get; private set; }

        internal bool ScreenCaptureRequested { get; set; }

        public Window(OpenGLRenderer gd)
        {
            _gd = gd;
        }

        public void Present(ITexture texture, ImageCrop crop, Action swapBuffersCallback)
        {
            _gd.Api.Disable(EnableCap.FramebufferSrgb);

            (uint oldDrawFramebufferHandle, uint oldReadFramebufferHandle) = ((Pipeline)_gd.Pipeline).GetBoundFramebuffers();

            CopyTextureToFrameBufferRGB(0, GetCopyFramebufferHandleLazy(), (TextureView)texture, crop, swapBuffersCallback);

            _gd.Api.BindFramebuffer(FramebufferTarget.ReadFramebuffer, oldReadFramebufferHandle);
            _gd.Api.BindFramebuffer(FramebufferTarget.DrawFramebuffer, oldDrawFramebufferHandle);

            _gd.Api.Enable(EnableCap.FramebufferSrgb);

            // Restore unpack alignment to 4, as performance overlays such as RTSS may change this to load their resources.
            _gd.Api.PixelStore(PixelStoreParameter.UnpackAlignment, 4);
        }

        public void ChangeVSyncMode(bool vsyncEnabled) { }

        public void SetSize(int width, int height)
        {
            _width = width;
            _height = height;

            _updateSize = true;
        }

        private void CopyTextureToFrameBufferRGB(uint drawFramebuffer, uint readFramebuffer, TextureView view, ImageCrop crop, Action swapBuffersCallback)
        {
            _gd.Api.BindFramebuffer(FramebufferTarget.DrawFramebuffer, drawFramebuffer);
            _gd.Api.BindFramebuffer(FramebufferTarget.ReadFramebuffer, readFramebuffer);

            TextureView viewConverted = view.Format.IsBgr() ? _gd.TextureCopy.BgraSwap(view) : view;

            UpdateEffect();

            if (_antiAliasing != null)
            {
                var oldView = viewConverted;

                viewConverted = _antiAliasing.Run(viewConverted, _width, _height);

                if (viewConverted.Format.IsBgr())
                {
                    var swappedView = _gd.TextureCopy.BgraSwap(viewConverted);

                    viewConverted.Dispose();

                    viewConverted = swappedView;
                }

                if (viewConverted != oldView && oldView != view)
                {
                    oldView.Dispose();
                }
            }

            _gd.Api.BindFramebuffer(FramebufferTarget.DrawFramebuffer, drawFramebuffer);
            _gd.Api.BindFramebuffer(FramebufferTarget.ReadFramebuffer, readFramebuffer);

            _gd.Api.FramebufferTexture(
                FramebufferTarget.ReadFramebuffer,
                FramebufferAttachment.ColorAttachment0,
                viewConverted.Handle,
                0);

            _gd.Api.ReadBuffer(ReadBufferMode.ColorAttachment0);

            _gd.Api.Disable(EnableCap.RasterizerDiscard);
            _gd.Api.Disable(EnableCap.ScissorTest, 0);

            _gd.Api.Clear(ClearBufferMask.ColorBufferBit);

            int srcX0, srcX1, srcY0, srcY1;

            if (crop.Left == 0 && crop.Right == 0)
            {
                srcX0 = 0;
                srcX1 = viewConverted.Width;
            }
            else
            {
                srcX0 = crop.Left;
                srcX1 = crop.Right;
            }

            if (crop.Top == 0 && crop.Bottom == 0)
            {
                srcY0 = 0;
                srcY1 = viewConverted.Height;
            }
            else
            {
                srcY0 = crop.Top;
                srcY1 = crop.Bottom;
            }

            float ratioX = crop.IsStretched ? 1.0f : MathF.Min(1.0f, _height * crop.AspectRatioX / (_width * crop.AspectRatioY));
            float ratioY = crop.IsStretched ? 1.0f : MathF.Min(1.0f, _width * crop.AspectRatioY / (_height * crop.AspectRatioX));

            int dstWidth = (int)(_width * ratioX);
            int dstHeight = (int)(_height * ratioY);

            int dstPaddingX = (_width - dstWidth) / 2;
            int dstPaddingY = (_height - dstHeight) / 2;

            int dstX0 = crop.FlipX ? _width - dstPaddingX : dstPaddingX;
            int dstX1 = crop.FlipX ? dstPaddingX : _width - dstPaddingX;

            int dstY0 = crop.FlipY ? dstPaddingY : _height - dstPaddingY;
            int dstY1 = crop.FlipY ? _height - dstPaddingY : dstPaddingY;

            if (ScreenCaptureRequested)
            {
                CaptureFrame(srcX0, srcY0, (uint)srcX1, (uint)srcY1, view.Format.IsBgr(), crop.FlipX, crop.FlipY);

                ScreenCaptureRequested = false;
            }

            if (_scalingFilter != null)
            {
                if (viewConverted.Format.IsBgr() && !_isBgra)
                {
                    RecreateUpscalingTexture(true);
                }

                _scalingFilter.Run(
                    viewConverted,
                    _upscaledTexture,
                    _width,
                    _height,
                    new Extents2D(
                        srcX0,
                        srcY0,
                        srcX1,
                        srcY1),
                    new Extents2D(
                        dstX0,
                        dstY0,
                        dstX1,
                        dstY1)
                    );

                srcX0 = dstX0;
                srcY0 = dstY0;
                srcX1 = dstX1;
                srcY1 = dstY1;

                _gd.Api.FramebufferTexture(
                    FramebufferTarget.ReadFramebuffer,
                    FramebufferAttachment.ColorAttachment0,
                    _upscaledTexture.Handle,
                    0);
            }

            _gd.Api.BlitFramebuffer(
                srcX0,
                srcY0,
                srcX1,
                srcY1,
                dstX0,
                dstY0,
                dstX1,
                dstY1,
                ClearBufferMask.ColorBufferBit,
                _isLinear ? BlitFramebufferFilter.Linear : BlitFramebufferFilter.Nearest);

            // Remove Alpha channel
            _gd.Api.ColorMask(false, false, false, true);
            _gd.Api.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);
            _gd.Api.Clear(ClearBufferMask.ColorBufferBit);

            for (int i = 0; i < Constants.MaxRenderTargets; i++)
            {
                ((Pipeline)_gd.Pipeline).RestoreComponentMask(i);
            }

            // Set clip control, viewport and the framebuffer to the output to placate overlays and OBS capture.
            _gd.Api.ClipControl(ClipControlOrigin.LowerLeft, ClipControlDepth.NegativeOneToOne);
            _gd.Api.Viewport(0, 0, (uint)_width, (uint)_height);
            _gd.Api.BindFramebuffer(FramebufferTarget.Framebuffer, drawFramebuffer);

            swapBuffersCallback();

            ((Pipeline)_gd.Pipeline).RestoreClipControl();
            ((Pipeline)_gd.Pipeline).RestoreScissor0Enable();
            ((Pipeline)_gd.Pipeline).RestoreRasterizerDiscard();
            ((Pipeline)_gd.Pipeline).RestoreViewport0();

            if (viewConverted != view)
            {
                viewConverted.Dispose();
            }
        }

        private uint GetCopyFramebufferHandleLazy()
        {
            uint handle = _copyFramebufferHandle;

            if (handle == 0)
            {
                handle = _gd.Api.GenFramebuffer();

                _copyFramebufferHandle = handle;
            }

            return handle;
        }

        public void InitializeBackgroundContext(IOpenGLContext baseContext)
        {
            BackgroundContext = new BackgroundContextWorker(baseContext);
            _initialized = true;
        }

        public unsafe void CaptureFrame(int x, int y, uint width, uint height, bool isBgra, bool flipX, bool flipY)
        {
            long size = 4 * width * height;

            _gd.Api.ReadPixels(x, y, width, height, isBgra ? PixelFormat.Bgra : PixelFormat.Rgba, PixelType.UnsignedByte, out int data);

            var bitmap = new Span<byte>((void*)data, (int)size).ToArray();

            _gd.OnScreenCaptured(new ScreenCaptureImageInfo((int)width, (int)height, isBgra, bitmap, flipX, flipY));
        }

        public void Dispose()
        {
            if (!_initialized)
            {
                return;
            }

            BackgroundContext.Dispose();

            if (_copyFramebufferHandle != 0)
            {
                _gd.Api.DeleteFramebuffer(_copyFramebufferHandle);

                _copyFramebufferHandle = 0;
            }

            _antiAliasing?.Dispose();
            _scalingFilter?.Dispose();
            _upscaledTexture?.Dispose();
        }

        public void SetAntiAliasing(AntiAliasing effect)
        {
            if (_currentAntiAliasing == effect && _antiAliasing != null)
            {
                return;
            }

            _currentAntiAliasing = effect;

            _updateEffect = true;
        }

        public void SetScalingFilter(ScalingFilter type)
        {
            if (_currentScalingFilter == type && _antiAliasing != null)
            {
                return;
            }

            _currentScalingFilter = type;

            _updateScalingFilter = true;
        }

        public void SetColorSpacePassthrough(bool colorSpacePassthroughEnabled) { }

        private void UpdateEffect()
        {
            if (_updateEffect)
            {
                _updateEffect = false;

                switch (_currentAntiAliasing)
                {
                    case AntiAliasing.Fxaa:
                        _antiAliasing?.Dispose();
                        _antiAliasing = new FxaaPostProcessingEffect(_gd);
                        break;
                    case AntiAliasing.None:
                        _antiAliasing?.Dispose();
                        _antiAliasing = null;
                        break;
                    case AntiAliasing.SmaaLow:
                    case AntiAliasing.SmaaMedium:
                    case AntiAliasing.SmaaHigh:
                    case AntiAliasing.SmaaUltra:
                        var quality = _currentAntiAliasing - AntiAliasing.SmaaLow;
                        if (_antiAliasing is SmaaPostProcessingEffect smaa)
                        {
                            smaa.Quality = quality;
                        }
                        else
                        {
                            _antiAliasing?.Dispose();
                            _antiAliasing = new SmaaPostProcessingEffect(_gd, quality);
                        }
                        break;
                }
            }

            if (_updateSize && !_updateScalingFilter)
            {
                RecreateUpscalingTexture();
            }

            _updateSize = false;

            if (_updateScalingFilter)
            {
                _updateScalingFilter = false;

                switch (_currentScalingFilter)
                {
                    case ScalingFilter.Bilinear:
                    case ScalingFilter.Nearest:
                        _scalingFilter?.Dispose();
                        _scalingFilter = null;
                        _isLinear = _currentScalingFilter == ScalingFilter.Bilinear;
                        _upscaledTexture?.Dispose();
                        _upscaledTexture = null;
                        break;
                    case ScalingFilter.Fsr:
                        if (_scalingFilter is not FsrScalingFilter)
                        {
                            _scalingFilter?.Dispose();
                            _scalingFilter = new FsrScalingFilter(_gd);
                        }
                        _isLinear = false;
                        _scalingFilter.Level = _scalingFilterLevel;

                        RecreateUpscalingTexture();
                        break;
                }
            }
        }

        private void RecreateUpscalingTexture(bool forceBgra = false)
        {
            _upscaledTexture?.Dispose();

            var info = new TextureCreateInfo(
                _width,
                _height,
                1,
                1,
                1,
                1,
                1,
                1,
                Format.R8G8B8A8Unorm,
                DepthStencilMode.Depth,
                Target.Texture2D,
                forceBgra ? SwizzleComponent.Blue : SwizzleComponent.Red,
                SwizzleComponent.Green,
                forceBgra ? SwizzleComponent.Red : SwizzleComponent.Blue,
                SwizzleComponent.Alpha);

            _isBgra = forceBgra;
            _upscaledTexture = _gd.CreateTexture(info) as TextureView;
        }

        public void SetScalingFilterLevel(float level)
        {
            _scalingFilterLevel = level;
            _updateScalingFilter = true;
        }
    }
}
