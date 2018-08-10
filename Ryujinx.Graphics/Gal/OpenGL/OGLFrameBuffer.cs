using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;

namespace Ryujinx.Graphics.Gal.OpenGL
{
    public class OGLFrameBuffer : IGalFrameBuffer
    {
        private struct Rect
        {
            public int X      { get; private set; }
            public int Y      { get; private set; }
            public int Width  { get; private set; }
            public int Height { get; private set; }

            public Rect(int X, int Y, int Width, int Height)
            {
                this.X = X;
                this.Y = Y;
                this.Width = Width;
                this.Height = Height;
            }
        }

        

        private static readonly DrawBuffersEnum[] DrawBuffers = new DrawBuffersEnum[]
        {
            DrawBuffersEnum.ColorAttachment0,
            DrawBuffersEnum.ColorAttachment1,
            DrawBuffersEnum.ColorAttachment2,
            DrawBuffersEnum.ColorAttachment3,
            DrawBuffersEnum.ColorAttachment4,
            DrawBuffersEnum.ColorAttachment5,
            DrawBuffersEnum.ColorAttachment6,
            DrawBuffersEnum.ColorAttachment7,
        };

        private const int NativeWidth  = 1280;
        private const int NativeHeight = 720;

        private const GalImageFormat RawFormat = GalImageFormat.A8B8G8R8;

        private Dictionary<long, TCE> ColorTextures;
        private Dictionary<long, TCE> ZetaTextures;

        private TCE RawTex;
        private TCE ReadTex;

        private Rect Viewport;
        private Rect Window;

        private bool FlipX;
        private bool FlipY;

        private int CropTop;
        private int CropLeft;
        private int CropRight;
        private int CropBottom;

        private int DummyFrameBuffer;

        private int SrcFb;
        private int DstFb;

        public OGLFrameBuffer()
        {
            ColorTextures = new Dictionary<long, TCE>();

            ZetaTextures = new Dictionary<long, TCE>();
        }

        public void CreateColor(long Key, int Width, int Height, GalFrameBufferFormat Format)
        {
            if (!ColorTextures.TryGetValue(Key, out TCE Tex))
            {
                Tex = new TCE();

                ColorTextures.Add(Key, Tex);
            }

            GalImageFormat ImageFormat = ImageFormatConverter.ConvertFrameBuffer(Format);

            Tex.EnsureSetup(new GalImage(Width, Height, ImageFormat));
        }

        public void BindColor(long Key, int Attachment)
        {
            if (ColorTextures.TryGetValue(Key, out TCE Tex))
            {
                EnsureFrameBuffer();

                GL.FramebufferTexture(
                    FramebufferTarget.Framebuffer,
                    FramebufferAttachment.ColorAttachment0 + Attachment,
                    Tex.Handle,
                    0);
            }
            else
            {
                UnbindColor(Attachment);
            }
        }

        public void UnbindColor(int Attachment)
        {
            EnsureFrameBuffer();

            GL.FramebufferTexture(
                FramebufferTarget.Framebuffer,
                FramebufferAttachment.ColorAttachment0 + Attachment,
                0,
                0);
        }

        public void CreateZeta(long Key, int Width, int Height, GalZetaFormat Format)
        {
            if (!ZetaTextures.TryGetValue(Key, out TCE Tex))
            {
                Tex = new TCE();

                ZetaTextures.Add(Key, Tex);
            }

            GalImageFormat ImageFormat = ImageFormatConverter.ConvertZeta(Format);

            Tex.EnsureSetup(new GalImage(Width, Height, ImageFormat));
        }

        public void BindZeta(long Key)
        {
            if (ZetaTextures.TryGetValue(Key, out TCE Tex))
            {
                EnsureFrameBuffer();

                GL.FramebufferTexture(
                    FramebufferTarget.Framebuffer,
                    FramebufferAttachment.DepthStencilAttachment,
                    Tex.Handle,
                    0);
            }
            else
            {
                UnbindZeta();
            }
        }

        public void UnbindZeta()
        {
            EnsureFrameBuffer();

            GL.FramebufferTexture(
                FramebufferTarget.Framebuffer,
                FramebufferAttachment.DepthStencilAttachment,
                0,
                0);
        }

        public void BindTexture(long Key, int Index)
        {
            TCE Tex;

            if (ColorTextures.TryGetValue(Key, out Tex) ||
                 ZetaTextures.TryGetValue(Key, out Tex))
            {
                GL.ActiveTexture(TextureUnit.Texture0 + Index);

                GL.BindTexture(TextureTarget.Texture2D, Tex.Handle);
            }
        }

        public void Set(long Key)
        {
            if (ColorTextures.TryGetValue(Key, out TCE Tex))
            {
                ReadTex = Tex;
            }
        }

        public void Set(byte[] Data, int Width, int Height)
        {
            if (RawTex == null)
            {
                RawTex = new TCE();
            }

            RawTex.EnsureSetup(new GalImage(Width, Height, RawFormat));

            GL.BindTexture(TextureTarget.Texture2D, RawTex.Handle);

            GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, Width, Height, RawTex.PixelFormat, RawTex.PixelType, Data);

            ReadTex = RawTex;
        }

        public void SetTransform(bool FlipX, bool FlipY, int Top, int Left, int Right, int Bottom)
        {
            this.FlipX = FlipX;
            this.FlipY = FlipY;

            CropTop    = Top;
            CropLeft   = Left;
            CropRight  = Right;
            CropBottom = Bottom;
        }

        public void SetWindowSize(int Width, int Height)
        {
            Window = new Rect(0, 0, Width, Height);
        }

        public void SetViewport(int X, int Y, int Width, int Height)
        {
            Viewport = new Rect(X, Y, Width, Height);

            SetViewport(Viewport);
        }

        private void SetViewport(Rect Viewport)
        {
            GL.Viewport(
                Viewport.X,
                Viewport.Y,
                Viewport.Width,
                Viewport.Height);
        }

        public void Render()
        {
            if (ReadTex == null)
            {
                return;
            }

            int SrcX0, SrcX1, SrcY0, SrcY1;

            if (CropLeft == 0 && CropRight == 0)
            {
                SrcX0 = 0;
                SrcX1 = ReadTex.Width;
            }
            else
            {
                SrcX0 = CropLeft;
                SrcX1 = CropRight;
            }

            if (CropTop == 0 && CropBottom == 0)
            {
                SrcY0 = 0;
                SrcY1 = ReadTex.Height;
            }
            else
            {
                SrcY0 = CropTop;
                SrcY1 = CropBottom;
            }

            float RatioX = MathF.Min(1f, (Window.Height * (float)NativeWidth)  / ((float)NativeHeight * Window.Width));
            float RatioY = MathF.Min(1f, (Window.Width  * (float)NativeHeight) / ((float)NativeWidth  * Window.Height));

            int DstWidth  = (int)(Window.Width  * RatioX);
            int DstHeight = (int)(Window.Height * RatioY);

            int DstPaddingX = (Window.Width  - DstWidth)  / 2;
            int DstPaddingY = (Window.Height - DstHeight) / 2;

            int DstX0 = FlipX ? Window.Width - DstPaddingX : DstPaddingX;
            int DstX1 = FlipX ? DstPaddingX : Window.Width - DstPaddingX;

            int DstY0 = FlipY ? DstPaddingY : Window.Height - DstPaddingY;
            int DstY1 = FlipY ? Window.Height - DstPaddingY : DstPaddingY;

            if (SrcFb == 0) SrcFb = GL.GenFramebuffer();

            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);

            GL.Viewport(0, 0, Window.Width, Window.Height);

            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, SrcFb);

            GL.FramebufferTexture(FramebufferTarget.ReadFramebuffer, FramebufferAttachment.ColorAttachment0, ReadTex.Handle, 0);

            GL.ReadBuffer(ReadBufferMode.ColorAttachment0);
            GL.DrawBuffer(DrawBufferMode.ColorAttachment0);

            GL.Clear(ClearBufferMask.ColorBufferBit);

            GL.BlitFramebuffer(
                SrcX0, SrcY0, SrcX1, SrcY1,
                DstX0, DstY0, DstX1, DstY1,
                ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Linear);

            EnsureFrameBuffer();
        }

        public void Copy(
            long SrcKey,
            long DstKey,
            int  SrcX0,
            int  SrcY0,
            int  SrcX1,
            int  SrcY1,
            int  DstX0,
            int  DstY0,
            int  DstX1,
            int  DstY1)
        {
            bool Found = false;

            if (ColorTextures.TryGetValue(SrcKey, out TCE SrcTex) &&
                ColorTextures.TryGetValue(DstKey, out TCE DstTex))
            {
                CopyTextures(
                    SrcX0, SrcY0, SrcX1, SrcY1,
                    DstX0, DstY0, DstX1, DstY1,
                    SrcTex.Handle,
                    DstTex.Handle,
                    FramebufferAttachment.ColorAttachment0,
                    ClearBufferMask.ColorBufferBit,
                    true);

                Found = true;
            }

            if (ZetaTextures.TryGetValue(SrcKey, out TCE ZetaSrcTex) &&
                ZetaTextures.TryGetValue(DstKey, out TCE ZetaDstTex))
            {
                CopyTextures(
                    SrcX0, SrcY0, SrcX1, SrcY1,
                    DstX0, DstY0, DstX1, DstY1,
                    ZetaSrcTex.Handle,
                    ZetaDstTex.Handle,
                    FramebufferAttachment.DepthStencilAttachment,
                    ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit,
                    false);

                Found = true;
            }

            if (Found)
            {
                EnsureFrameBuffer();
            }
        }

        public void GetBufferData(long Key, Action<byte[]> Callback)
        {
            TCE Tex;

            if (ColorTextures.TryGetValue(Key, out Tex) ||
                 ZetaTextures.TryGetValue(Key, out Tex))
            {
                byte[] Data = new byte[Tex.Width * Tex.Height * TCE.MaxBpp];

                GL.BindTexture(TextureTarget.Texture2D, Tex.Handle);

                GL.GetTexImage(
                    TextureTarget.Texture2D,
                    0,
                    Tex.PixelFormat,
                    Tex.PixelType,
                    Data);

                Callback(Data);
            }
        }

        public void SetBufferData(
            long             Key,
            int              Width,
            int              Height,
            byte[]           Buffer)
        {
            TCE Tex;

            if (ColorTextures.TryGetValue(Key, out Tex) ||
                 ZetaTextures.TryGetValue(Key, out Tex))
            {
                GL.BindTexture(TextureTarget.Texture2D, Tex.Handle);

                const int Level  = 0;
                const int Border = 0;

                GL.TexImage2D(
                    TextureTarget.Texture2D,
                    Level,
                    Tex.InternalFormat,
                    Width,
                    Height,
                    Border,
                    Tex.PixelFormat,
                    Tex.PixelType,
                    Buffer);
            }
        }

        private void EnsureFrameBuffer()
        {
            if (DummyFrameBuffer == 0)
            {
                DummyFrameBuffer = GL.GenFramebuffer();
            }

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, DummyFrameBuffer);

            GL.DrawBuffers(8, DrawBuffers);
        }

        private void CopyTextures(
            int SrcX0,
            int SrcY0,
            int SrcX1,
            int SrcY1,
            int DstX0,
            int DstY0,
            int DstX1,
            int DstY1,
            int SrcTexture,
            int DstTexture,
            FramebufferAttachment Attachment,
            ClearBufferMask Mask,
            bool Color)
        {
            if (SrcFb == 0) SrcFb = GL.GenFramebuffer();
            if (DstFb == 0) DstFb = GL.GenFramebuffer();

            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, SrcFb);
            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, DstFb);

            GL.FramebufferTexture(
                FramebufferTarget.ReadFramebuffer,
                Attachment,
                SrcTexture,
                0);

            GL.FramebufferTexture(
                FramebufferTarget.DrawFramebuffer,
                Attachment,
                DstTexture,
                0);

            if (Color)
            {
                GL.DrawBuffer(DrawBufferMode.ColorAttachment0);
            }

            GL.Clear(Mask);

            GL.BlitFramebuffer(
                SrcX0, SrcY0, SrcX1, SrcY1,
                DstX0, DstY0, DstX1, DstY1,
                Mask,
                Color ? BlitFramebufferFilter.Linear : BlitFramebufferFilter.Nearest);
        }
    }
}