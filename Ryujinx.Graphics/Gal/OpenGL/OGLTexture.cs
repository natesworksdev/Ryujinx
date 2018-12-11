using OpenTK.Graphics.OpenGL;
using Ryujinx.Graphics.Texture;
using System;

namespace Ryujinx.Graphics.Gal.OpenGL
{
    class OGLTexture : IGalTexture
    {

        private OGLCachedResource<ImageHandler> TextureCache;

        public EventHandler<int> TextureDeleted { get; set; }

        public OGLTexture()
        {
            TextureCache = new OGLCachedResource<ImageHandler>(DeleteTexture, OGLResourceLimits.TextureLimit);
        }

        public void LockCache()
        {
            TextureCache.Lock();
        }

        public void UnlockCache()
        {
            TextureCache.Unlock();
        }

        private void DeleteTexture(ImageHandler CachedImage)
        {
            TextureDeleted?.Invoke(this, CachedImage.Handle);

            GL.DeleteTexture(CachedImage.Handle);
        }

        public void Create(long Key, int Size, GalImage Image)
        {
            int Handle = GL.GenTexture();

            GL.BindTexture(TextureTarget.Texture2D, Handle);

            const int Level  = 0; //TODO: Support mipmap textures.
            const int Border = 0;

            TextureCache.AddOrUpdate(Key, new ImageHandler(Handle, Image), (uint)Size);

            if (ImageUtils.IsCompressed(Image.Format))
            {
                throw new InvalidOperationException("Surfaces with compressed formats are not supported!");
            }

            (PixelInternalFormat InternalFmt,
             PixelFormat         Format,
             PixelType           Type) = OGLEnumConverter.GetImageFormat(Image.Format);

            GL.TexImage2D(
                TextureTarget.Texture2D,
                Level,
                InternalFmt,
                Image.Width,
                Image.Height,
                Border,
                Format,
                Type,
                IntPtr.Zero);
        }

        public void Create(long Key, byte[] Data, GalImage Image)
        {
            int Handle = GL.GenTexture();

            GL.BindTexture(TextureTarget.Texture2D, Handle);

            const int Level  = 0; //TODO: Support mipmap textures.
            const int Border = 0;

            TextureCache.AddOrUpdate(Key, new ImageHandler(Handle, Image), (uint)Data.Length);

            if (ImageUtils.IsCompressed(Image.Format) && !IsAstc(Image.Format))
            {
                InternalFormat InternalFmt = OGLEnumConverter.GetCompressedImageFormat(Image.Format);

                GL.CompressedTexImage2D(
                    TextureTarget.Texture2D,
                    Level,
                    InternalFmt,
                    Image.Width,
                    Image.Height,
                    Border,
                    Data.Length,
                    Data);
            }
            else
            {
                //TODO: Use KHR_texture_compression_astc_hdr when available
                if (IsAstc(Image.Format))
                {
                    int TextureBlockWidth  = ImageUtils.GetBlockWidth(Image.Format);
                    int TextureBlockHeight = ImageUtils.GetBlockHeight(Image.Format);

                    Data = ASTCDecoder.DecodeToRGBA8888(
                        Data,
                        TextureBlockWidth,
                        TextureBlockHeight, 1,
                        Image.Width,
                        Image.Height, 1);

                    Image.Format = GalImageFormat.RGBA8 | (Image.Format & GalImageFormat.TypeMask);
                }

                (PixelInternalFormat InternalFmt,
                 PixelFormat         Format,
                 PixelType           Type) = OGLEnumConverter.GetImageFormat(Image.Format);

                GL.TexImage2D(
                    TextureTarget.Texture2D,
                    Level,
                    InternalFmt,
                    Image.Width,
                    Image.Height,
                    Border,
                    Format,
                    Type,
                    Data);
            }
        }

        private static bool IsAstc(GalImageFormat Format)
        {
            Format &= GalImageFormat.FormatMask;

            return Format > GalImageFormat.Astc2DStart && Format < GalImageFormat.Astc2DEnd;
        }

        public void Reinterpret(long Key, GalImage NewImage)
        {
            if (!TryGetImage(Key, out GalImage OldImage))
            {
                return;
            }

            if (NewImage.Width  == OldImage.Width  &&
                NewImage.Height == OldImage.Height &&
                NewImage.Format == OldImage.Format)
            {
                return;
            }

            //The buffer should be large enough to hold the largest texture.
            int BufferSize = Math.Max(ImageUtils.GetSize(OldImage),
                                      ImageUtils.GetSize(NewImage));

            if (!PboCache.TryReuseValue(0, BufferSize, out int Handle))
            {
                Handle = GL.GenBuffer();

                GL.BindBuffer(BufferTarget.PixelPackBuffer, Handle);

                GL.BufferData(BufferTarget.PixelPackBuffer, BufferSize, IntPtr.Zero, BufferUsageHint.StreamCopy);

                PboCache.AddOrUpdate(0, BufferSize, Handle, BufferSize);
            }
            else
            {
                GL.BindBuffer(BufferTarget.PixelPackBuffer, Handle);
            }

            if (!TryGetImageHandler(Key, out ImageHandler CachedImage))
            {
                throw new ArgumentException(nameof(Key));
            }

            (_, PixelFormat Format, PixelType Type) = OGLEnumConverter.GetImageFormat(CachedImage.Format);

            GL.BindTexture(TextureTarget.Texture2D, CachedImage.Handle);

            GL.GetTexImage(TextureTarget.Texture2D, 0, Format, Type, IntPtr.Zero);

            GL.BindBuffer(BufferTarget.PixelPackBuffer, 0);
            GL.BindBuffer(BufferTarget.PixelUnpackBuffer, Handle);

            GL.PixelStore(PixelStoreParameter.UnpackRowLength, OldImage.Width);

            CreateFromPboOrEmpty(Key, ImageUtils.GetSize(NewImage), NewImage);

            GL.PixelStore(PixelStoreParameter.UnpackRowLength, 0);

            GL.BindBuffer(BufferTarget.PixelUnpackBuffer, 0);
        }

        public bool TryGetImage(long Key, out GalImage Image)
        {
            if (TextureCache.TryGetValue(Key, out ImageHandler CachedImage))
            {
                Image = CachedImage.Image;

                return true;
            }

            Image = default(GalImage);

            return false;
        }

        public bool TryGetImageHandler(long Key, out ImageHandler CachedImage)
        {
            if (TextureCache.TryGetValue(Key, out CachedImage))
            {
                return true;
            }

            CachedImage = null;

            return false;
        }

        public void Bind(long Key, int Index, GalImage Image)
        {
            if (TextureCache.TryGetValue(Key, out ImageHandler CachedImage))
            {
                GL.ActiveTexture(TextureUnit.Texture0 + Index);

                GL.BindTexture(TextureTarget.Texture2D, CachedImage.Handle);

                int[] SwizzleRgba = new int[]
                {
                    (int)OGLEnumConverter.GetTextureSwizzle(Image.XSource),
                    (int)OGLEnumConverter.GetTextureSwizzle(Image.YSource),
                    (int)OGLEnumConverter.GetTextureSwizzle(Image.ZSource),
                    (int)OGLEnumConverter.GetTextureSwizzle(Image.WSource)
                };

                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureSwizzleRgba, SwizzleRgba);
            }
        }

        public void SetSampler(GalTextureSampler Sampler)
        {
            int WrapS = (int)OGLEnumConverter.GetTextureWrapMode(Sampler.AddressU);
            int WrapT = (int)OGLEnumConverter.GetTextureWrapMode(Sampler.AddressV);

            int MinFilter = (int)OGLEnumConverter.GetTextureMinFilter(Sampler.MinFilter, Sampler.MipFilter);
            int MagFilter = (int)OGLEnumConverter.GetTextureMagFilter(Sampler.MagFilter);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, WrapS);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, WrapT);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, MinFilter);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, MagFilter);

            float[] Color = new float[]
            {
                Sampler.BorderColor.Red,
                Sampler.BorderColor.Green,
                Sampler.BorderColor.Blue,
                Sampler.BorderColor.Alpha
            };

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBorderColor, Color);
        }
    }
}
