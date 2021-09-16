using OpenTK.Graphics.OpenGL;
using Ryujinx.Graphics.GAL;
using Ryujinx.Graphics.OpenGL.Image;
using System;

namespace Ryujinx.Graphics.OpenGL
{
    class Window : IWindow, IDisposable
    {
        private readonly Renderer _renderer;

        private int _width;
        private int _height;

        private int _copyFramebufferHandle;

        internal BackgroundContextWorker BackgroundContext { get; private set; }

        internal bool ScreenCaptureRequested { get; set; }

        public Window(Renderer renderer)
        {
            _renderer = renderer;
        }

        public void Present(ITexture texture, ImageCrop crop, Action swapBuffersCallback)
        {
            GL.Disable(EnableCap.FramebufferSrgb);

            CopyTextureToFrameBufferRGB(0, GetCopyFramebufferHandleLazy(), (TextureView)texture, crop);

            GL.Enable(EnableCap.FramebufferSrgb);

            swapBuffersCallback();
        }

        public void SetSize(int width, int height)
        {
            _width  = width;
            _height = height;
        }

        private void CopyTextureToFrameBufferRGB(int drawFramebuffer, int readFramebuffer, TextureView view, ImageCrop crop)
        {
            int oldDrawFramebufferHandle = ((Pipeline)_renderer.Pipeline).GetBoundDrawFramebuffer();

            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, drawFramebuffer);

            TextureView viewConverted = view.Format.IsBgr() ? _renderer.TextureCopy.BgraSwap(view) : view;

            GL.NamedFramebufferTexture(
                readFramebuffer,
                FramebufferAttachment.ColorAttachment0,
                viewConverted.Handle,
                0);

            GL.NamedFramebufferReadBuffer(readFramebuffer, ReadBufferMode.ColorAttachment0);

            GL.Disable(EnableCap.RasterizerDiscard);
            GL.Disable(IndexedEnableCap.ScissorTest, 0);

            GL.Clear(ClearBufferMask.ColorBufferBit);

            int srcX0, srcX1, srcY0, srcY1;
            float scale = view.ScaleFactor;

            if (crop.Left == 0 && crop.Right == 0)
            {
                srcX0 = 0;
                srcX1 = (int)(view.Width / scale);
            }
            else
            {
                srcX0 = crop.Left;
                srcX1 = crop.Right;
            }

            if (crop.Top == 0 && crop.Bottom == 0)
            {
                srcY0 = 0;
                srcY1 = (int)(view.Height / scale);
            }
            else
            {
                srcY0 = crop.Top;
                srcY1 = crop.Bottom;
            }

            if (scale != 1f)
            {
                srcX0 = (int)(srcX0 * scale);
                srcY0 = (int)(srcY0 * scale);
                srcX1 = (int)Math.Ceiling(srcX1 * scale);
                srcY1 = (int)Math.Ceiling(srcY1 * scale);
            }

            float ratioX = crop.IsStretched ? 1.0f : MathF.Min(1.0f, _height * crop.AspectRatioX / (_width  * crop.AspectRatioY));
            float ratioY = crop.IsStretched ? 1.0f : MathF.Min(1.0f, _width  * crop.AspectRatioY / (_height * crop.AspectRatioX));

            int dstWidth  = (int)(_width  * ratioX);
            int dstHeight = (int)(_height * ratioY);

            int dstPaddingX = (_width  - dstWidth)  / 2;
            int dstPaddingY = (_height - dstHeight) / 2;

            int dstX0 = crop.FlipX ? _width - dstPaddingX : dstPaddingX;
            int dstX1 = crop.FlipX ? dstPaddingX : _width - dstPaddingX;

            int dstY0 = crop.FlipY ? dstPaddingY : _height - dstPaddingY;
            int dstY1 = crop.FlipY ? _height - dstPaddingY : dstPaddingY;

            if (ScreenCaptureRequested)
            {
                GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, readFramebuffer);

                CaptureFrame(srcX0, srcY0, srcX1, srcY1, view.Format.IsBgr(), crop.FlipX, crop.FlipY);

                GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, 0);

                ScreenCaptureRequested = false;
            }

            GL.BlitNamedFramebuffer(
                readFramebuffer,
                drawFramebuffer,
                srcX0,
                srcY0,
                srcX1,
                srcY1,
                dstX0,
                dstY0,
                dstX1,
                dstY1,
                ClearBufferMask.ColorBufferBit,
                BlitFramebufferFilter.Linear);

            // Remove Alpha channel
            GL.ColorMask(false, false, false, true);
            GL.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit);

            for (int i = 0; i < Constants.MaxRenderTargets; i++)
            {
                ((Pipeline)_renderer.Pipeline).RestoreComponentMask(i);
            }

            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, oldDrawFramebufferHandle);

            ((Pipeline)_renderer.Pipeline).RestoreScissor0Enable();
            ((Pipeline)_renderer.Pipeline).RestoreRasterizerDiscard();

            if (viewConverted != view)
            {
                viewConverted.Dispose();
            }
        }

        private int GetCopyFramebufferHandleLazy()
        {
            int handle = _copyFramebufferHandle;

            if (handle == 0)
            {
                handle = Framebuffer.Create();

                _copyFramebufferHandle = handle;
            }

            return handle;
        }

        public void InitializeBackgroundContext(IOpenGLContext baseContext)
        {
            BackgroundContext = new BackgroundContextWorker(baseContext);
        }

        public void CaptureFrame(int x, int y, int width, int height, bool isBgra, bool flipX, bool flipY)
        {
            long size = Math.Abs(4 * width * height);
            byte[] bitmap = new byte[size];

            GL.ReadPixels(x, y, width, height, isBgra ? PixelFormat.Bgra : PixelFormat.Rgba, PixelType.UnsignedByte, bitmap);

            _renderer.OnScreenCaptured(new ScreenCaptureImageInfo(width, height, isBgra, bitmap, flipX, flipY));
        }

        public void Dispose()
        {
            BackgroundContext.Dispose();

            if (_copyFramebufferHandle != 0)
            {
                GL.DeleteFramebuffer(_copyFramebufferHandle);

                _copyFramebufferHandle = 0;
            }
        }
    }
}
