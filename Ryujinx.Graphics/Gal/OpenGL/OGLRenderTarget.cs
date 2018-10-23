using OpenTK.Graphics.OpenGL;
using Ryujinx.Graphics.Texture;
using System;

namespace Ryujinx.Graphics.Gal.OpenGL
{
    class OglRenderTarget : IGalRenderTarget
    {
        private struct Rect
        {
            public int X      { get; private set; }
            public int Y      { get; private set; }
            public int Width  { get; private set; }
            public int Height { get; private set; }

            public Rect(int x, int y, int width, int height)
            {
                this.X      = x;
                this.Y      = y;
                this.Width  = width;
                this.Height = height;
            }
        }

        private class FrameBufferAttachments
        {
            public long[] Colors;
            public long Zeta;

            public int MapCount;
            public DrawBuffersEnum[] Map;

            public FrameBufferAttachments()
            {
                Colors = new long[RenderTargetsCount];

                Map = new DrawBuffersEnum[RenderTargetsCount];
            }

            public void SetAndClear(FrameBufferAttachments source)
            {
                Zeta     = source.Zeta;
                MapCount = source.MapCount;

                source.Zeta     = 0;
                source.MapCount = 0;

                for (int i = 0; i < RenderTargetsCount; i++)
                {
                    Colors[i] = source.Colors[i];
                    Map[i]    = source.Map[i];

                    source.Colors[i] = 0;
                    source.Map[i]    = 0;
                }
            }
        }

        private const int NativeWidth  = 1280;
        private const int NativeHeight = 720;

        private const int RenderTargetsCount = GalPipelineState.RenderTargetsCount;

        private OglTexture _texture;

        private ImageHandler _readTex;

        private float[] _viewports;
        private Rect _window;

        private bool _flipX;
        private bool _flipY;

        private int _cropTop;
        private int _cropLeft;
        private int _cropRight;
        private int _cropBottom;

        //This framebuffer is used to attach guest rendertargets,
        //think of it as a dummy OpenGL VAO
        private int _dummyFrameBuffer;

        //These framebuffers are used to blit images
        private int _srcFb;
        private int _dstFb;

        private FrameBufferAttachments _attachments;
        private FrameBufferAttachments _oldAttachments;

        private int _copyPbo;

        public OglRenderTarget(OglTexture texture)
        {
            _attachments = new FrameBufferAttachments();

            _oldAttachments = new FrameBufferAttachments();

            _viewports = new float[RenderTargetsCount * 4];

            this._texture = texture;
        }

        public void Bind()
        {
            if (_dummyFrameBuffer == 0)
            {
                _dummyFrameBuffer = GL.GenFramebuffer();
            }

            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, _dummyFrameBuffer);

            ImageHandler cachedImage;

            for (int attachment = 0; attachment < RenderTargetsCount; attachment++)
            {
                long key = _attachments.Colors[attachment];

                if (key == _oldAttachments.Colors[attachment])
                {
                    continue;
                }

                int handle = 0;

                if (key != 0 && _texture.TryGetImageHandler(key, out cachedImage))
                {
                    handle = cachedImage.Handle;
                }

                GL.FramebufferTexture(
                    FramebufferTarget.DrawFramebuffer,
                    FramebufferAttachment.ColorAttachment0 + attachment,
                    handle,
                    0);
            }

            if (_attachments.Zeta != _oldAttachments.Zeta)
            {
                if (_attachments.Zeta != 0 && _texture.TryGetImageHandler(_attachments.Zeta, out cachedImage))
                {
                    if (cachedImage.HasDepth && cachedImage.HasStencil)
                    {
                        GL.FramebufferTexture(
                            FramebufferTarget.DrawFramebuffer,
                            FramebufferAttachment.DepthStencilAttachment,
                            cachedImage.Handle,
                            0);
                    }
                    else if (cachedImage.HasDepth)
                    {
                        GL.FramebufferTexture(
                            FramebufferTarget.DrawFramebuffer,
                            FramebufferAttachment.DepthAttachment,
                            cachedImage.Handle,
                            0);

                        GL.FramebufferTexture(
                            FramebufferTarget.DrawFramebuffer,
                            FramebufferAttachment.StencilAttachment,
                            0,
                            0);
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }
                else
                {
                    GL.FramebufferTexture(
                        FramebufferTarget.DrawFramebuffer,
                        FramebufferAttachment.DepthStencilAttachment,
                        0,
                        0);
                }
            }

            if (OglExtension.ViewportArray)
            {
                GL.ViewportArray(0, RenderTargetsCount, _viewports);
            }
            else
            {
                GL.Viewport(
                    (int)_viewports[0],
                    (int)_viewports[1],
                    (int)_viewports[2],
                    (int)_viewports[3]);
            }

            if (_attachments.MapCount > 1)
            {
                GL.DrawBuffers(_attachments.MapCount, _attachments.Map);
            }
            else if (_attachments.MapCount == 1)
            {
                GL.DrawBuffer((DrawBufferMode)_attachments.Map[0]);
            }
            else
            {
                GL.DrawBuffer(DrawBufferMode.None);
            }

            _oldAttachments.SetAndClear(_attachments);
        }

        public void BindColor(long key, int attachment)
        {
            _attachments.Colors[attachment] = key;
        }

        public void UnbindColor(int attachment)
        {
            _attachments.Colors[attachment] = 0;
        }

        public void BindZeta(long key)
        {
            _attachments.Zeta = key;
        }

        public void UnbindZeta()
        {
            _attachments.Zeta = 0;
        }

        public void Present(long key)
        {
            _texture.TryGetImageHandler(key, out _readTex);
        }

        public void SetMap(int[] map)
        {
            if (map != null)
            {
                _attachments.MapCount = map.Length;

                for (int attachment = 0; attachment < _attachments.MapCount; attachment++)
                {
                    _attachments.Map[attachment] = DrawBuffersEnum.ColorAttachment0 + map[attachment];
                }
            }
            else
            {
                _attachments.MapCount = 0;
            }
        }

        public void SetTransform(bool flipX, bool flipY, int top, int left, int right, int bottom)
        {
            this._flipX = flipX;
            this._flipY = flipY;

            _cropTop    = top;
            _cropLeft   = left;
            _cropRight  = right;
            _cropBottom = bottom;
        }

        public void SetWindowSize(int width, int height)
        {
            _window = new Rect(0, 0, width, height);
        }

        public void SetViewport(int attachment, int x, int y, int width, int height)
        {
            int offset = attachment * 4;

            _viewports[offset + 0] = x;
            _viewports[offset + 1] = y;
            _viewports[offset + 2] = width;
            _viewports[offset + 3] = height;
        }

        public void Render()
        {
            if (_readTex == null)
            {
                return;
            }

            int srcX0, srcX1, srcY0, srcY1;

            if (_cropLeft == 0 && _cropRight == 0)
            {
                srcX0 = 0;
                srcX1 = _readTex.Width;
            }
            else
            {
                srcX0 = _cropLeft;
                srcX1 = _cropRight;
            }

            if (_cropTop == 0 && _cropBottom == 0)
            {
                srcY0 = 0;
                srcY1 = _readTex.Height;
            }
            else
            {
                srcY0 = _cropTop;
                srcY1 = _cropBottom;
            }

            float ratioX = MathF.Min(1f, (_window.Height * (float)NativeWidth)  / ((float)NativeHeight * _window.Width));
            float ratioY = MathF.Min(1f, (_window.Width  * (float)NativeHeight) / ((float)NativeWidth  * _window.Height));

            int dstWidth  = (int)(_window.Width  * ratioX);
            int dstHeight = (int)(_window.Height * ratioY);

            int dstPaddingX = (_window.Width  - dstWidth)  / 2;
            int dstPaddingY = (_window.Height - dstHeight) / 2;

            int dstX0 = _flipX ? _window.Width - dstPaddingX : dstPaddingX;
            int dstX1 = _flipX ? dstPaddingX : _window.Width - dstPaddingX;

            int dstY0 = _flipY ? dstPaddingY : _window.Height - dstPaddingY;
            int dstY1 = _flipY ? _window.Height - dstPaddingY : dstPaddingY;

            GL.Viewport(0, 0, _window.Width, _window.Height);

            if (_srcFb == 0)
            {
                _srcFb = GL.GenFramebuffer();
            }

            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, _srcFb);
            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);

            GL.FramebufferTexture(FramebufferTarget.ReadFramebuffer, FramebufferAttachment.ColorAttachment0, _readTex.Handle, 0);

            GL.ReadBuffer(ReadBufferMode.ColorAttachment0);

            GL.Clear(ClearBufferMask.ColorBufferBit);

            GL.BlitFramebuffer(
                srcX0, srcY0, srcX1, srcY1,
                dstX0, dstY0, dstX1, dstY1,
                ClearBufferMask.ColorBufferBit,
                BlitFramebufferFilter.Linear);
        }

        public void Copy(
            long srcKey,
            long dstKey,
            int  srcX0,
            int  srcY0,
            int  srcX1,
            int  srcY1,
            int  dstX0,
            int  dstY0,
            int  dstX1,
            int  dstY1)
        {
            if (_texture.TryGetImageHandler(srcKey, out ImageHandler srcTex) &&
                _texture.TryGetImageHandler(dstKey, out ImageHandler dstTex))
            {
                if (srcTex.HasColor   != dstTex.HasColor ||
                    srcTex.HasDepth   != dstTex.HasDepth ||
                    srcTex.HasStencil != dstTex.HasStencil)
                {
                    throw new NotImplementedException();
                }

                if (_srcFb == 0)
                {
                    _srcFb = GL.GenFramebuffer();
                }

                if (_dstFb == 0)
                {
                    _dstFb = GL.GenFramebuffer();
                }

                GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, _srcFb);
                GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, _dstFb);

                FramebufferAttachment attachment = GetAttachment(srcTex);

                GL.FramebufferTexture(FramebufferTarget.ReadFramebuffer, attachment, srcTex.Handle, 0);
                GL.FramebufferTexture(FramebufferTarget.DrawFramebuffer, attachment, dstTex.Handle, 0);

                BlitFramebufferFilter filter = BlitFramebufferFilter.Nearest;

                if (srcTex.HasColor)
                {
                    GL.DrawBuffer(DrawBufferMode.ColorAttachment0);

                    filter = BlitFramebufferFilter.Linear;
                }

                ClearBufferMask mask = GetClearMask(srcTex);

                GL.Clear(mask);

                GL.BlitFramebuffer(srcX0, srcY0, srcX1, srcY1, dstX0, dstY0, dstX1, dstY1, mask, filter);
            }
        }

        public void Reinterpret(long key, GalImage newImage)
        {
            if (!_texture.TryGetImage(key, out GalImage oldImage))
            {
                return;
            }

            if (newImage.Format == oldImage.Format)
            {
                return;
            }

            if (_copyPbo == 0)
            {
                _copyPbo = GL.GenBuffer();
            }

            GL.BindBuffer(BufferTarget.PixelPackBuffer, _copyPbo);

            GL.BufferData(BufferTarget.PixelPackBuffer, Math.Max(ImageUtils.GetSize(oldImage), ImageUtils.GetSize(newImage)), IntPtr.Zero, BufferUsageHint.StreamCopy);

            if (!_texture.TryGetImageHandler(key, out ImageHandler cachedImage))
            {
                throw new InvalidOperationException();
            }

            (_, PixelFormat format, PixelType type) = OglEnumConverter.GetImageFormat(cachedImage.Format);

            GL.BindTexture(TextureTarget.Texture2D, cachedImage.Handle);

            GL.GetTexImage(TextureTarget.Texture2D, 0, format, type, IntPtr.Zero);

            GL.BindBuffer(BufferTarget.PixelPackBuffer, 0);

            GL.BindBuffer(BufferTarget.PixelUnpackBuffer, _copyPbo);

            _texture.Create(key, ImageUtils.GetSize(newImage), newImage);

            GL.BindBuffer(BufferTarget.PixelUnpackBuffer, 0);
        }

        private static FramebufferAttachment GetAttachment(ImageHandler cachedImage)
        {
            if (cachedImage.HasColor)
            {
                return FramebufferAttachment.ColorAttachment0;
            }
            else if (cachedImage.HasDepth && cachedImage.HasStencil)
            {
                return FramebufferAttachment.DepthStencilAttachment;
            }
            else if (cachedImage.HasDepth)
            {
                return FramebufferAttachment.DepthAttachment;
            }
            else if (cachedImage.HasStencil)
            {
                return FramebufferAttachment.StencilAttachment;
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        private static ClearBufferMask GetClearMask(ImageHandler cachedImage)
        {
            return (cachedImage.HasColor   ? ClearBufferMask.ColorBufferBit   : 0) |
                   (cachedImage.HasDepth   ? ClearBufferMask.DepthBufferBit   : 0) |
                   (cachedImage.HasStencil ? ClearBufferMask.StencilBufferBit : 0);
        }
    }
}