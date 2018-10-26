using OpenTK.Graphics.OpenGL;
using Ryujinx.Graphics.Texture;
using System;

namespace Ryujinx.Graphics.Gal.OpenGL
{
    internal class OGLTexture : IGalTexture
    {
        private OGLCachedResource<ImageHandler> _textureCache;

        public EventHandler<int> TextureDeleted { get; set; }

        public OGLTexture()
        {
            _textureCache = new OGLCachedResource<ImageHandler>(DeleteTexture);
        }

        public void LockCache()
        {
            _textureCache.Lock();
        }

        public void UnlockCache()
        {
            _textureCache.Unlock();
        }

        private void DeleteTexture(ImageHandler cachedImage)
        {
            TextureDeleted?.Invoke(this, cachedImage.Handle);

            GL.DeleteTexture(cachedImage.Handle);
        }

        public void Create(long key, int size, GalImage image)
        {
            int handle = GL.GenTexture();

            GL.BindTexture(TextureTarget.Texture2D, handle);

            const int level  = 0; //TODO: Support mipmap textures.
            const int border = 0;

            _textureCache.AddOrUpdate(key, new ImageHandler(handle, image), (uint)size);

            if (ImageUtils.IsCompressed(image.Format)) throw new InvalidOperationException("Surfaces with compressed formats are not supported!");

            (PixelInternalFormat internalFmt,
             PixelFormat         format,
             PixelType           type) = OGLEnumConverter.GetImageFormat(image.Format);

            GL.TexImage2D(
                TextureTarget.Texture2D,
                level,
                internalFmt,
                image.Width,
                image.Height,
                border,
                format,
                type,
                IntPtr.Zero);
        }

        public void Create(long key, byte[] data, GalImage image)
        {
            int handle = GL.GenTexture();

            GL.BindTexture(TextureTarget.Texture2D, handle);

            const int level  = 0; //TODO: Support mipmap textures.
            const int border = 0;

            _textureCache.AddOrUpdate(key, new ImageHandler(handle, image), (uint)data.Length);

            if (ImageUtils.IsCompressed(image.Format) && !IsAstc(image.Format))
            {
                InternalFormat internalFmt = OGLEnumConverter.GetCompressedImageFormat(image.Format);

                GL.CompressedTexImage2D(
                    TextureTarget.Texture2D,
                    level,
                    internalFmt,
                    image.Width,
                    image.Height,
                    border,
                    data.Length,
                    data);
            }
            else
            {
                //TODO: Use KHR_texture_compression_astc_hdr when available
                if (IsAstc(image.Format))
                {
                    int textureBlockWidth  = ImageUtils.GetBlockWidth(image.Format);
                    int textureBlockHeight = ImageUtils.GetBlockHeight(image.Format);

                    data = ASTCDecoder.DecodeToRgba8888(
                        data,
                        textureBlockWidth,
                        textureBlockHeight, 1,
                        image.Width,
                        image.Height, 1);

                    image.Format = GalImageFormat.Rgba8 | GalImageFormat.Unorm;
                }

                (PixelInternalFormat internalFmt,
                 PixelFormat         format,
                 PixelType           type) = OGLEnumConverter.GetImageFormat(image.Format);

                GL.TexImage2D(
                    TextureTarget.Texture2D,
                    level,
                    internalFmt,
                    image.Width,
                    image.Height,
                    border,
                    format,
                    type,
                    data);
            }
        }

        private static bool IsAstc(GalImageFormat format)
        {
            format &= GalImageFormat.FormatMask;

            return format > GalImageFormat.Astc2DStart && format < GalImageFormat.Astc2DEnd;
        }

        public bool TryGetImage(long key, out GalImage image)
        {
            if (_textureCache.TryGetValue(key, out ImageHandler cachedImage))
            {
                image = cachedImage.Image;

                return true;
            }

            image = default(GalImage);

            return false;
        }

        public bool TryGetImageHandler(long key, out ImageHandler cachedImage)
        {
            if (_textureCache.TryGetValue(key, out cachedImage)) return true;

            cachedImage = null;

            return false;
        }

        public void Bind(long key, int index, GalImage image)
        {
            if (_textureCache.TryGetValue(key, out ImageHandler cachedImage))
            {
                GL.ActiveTexture(TextureUnit.Texture0 + index);

                GL.BindTexture(TextureTarget.Texture2D, cachedImage.Handle);

                int[] swizzleRgba = new int[]
                {
                    (int)OGLEnumConverter.GetTextureSwizzle(image.XSource),
                    (int)OGLEnumConverter.GetTextureSwizzle(image.YSource),
                    (int)OGLEnumConverter.GetTextureSwizzle(image.ZSource),
                    (int)OGLEnumConverter.GetTextureSwizzle(image.WSource)
                };

                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureSwizzleRgba, swizzleRgba);
            }
        }

        public void SetSampler(GalTextureSampler sampler)
        {
            int wrapS = (int)OGLEnumConverter.GetTextureWrapMode(sampler.AddressU);
            int wrapT = (int)OGLEnumConverter.GetTextureWrapMode(sampler.AddressV);

            int minFilter = (int)OGLEnumConverter.GetTextureMinFilter(sampler.MinFilter, sampler.MipFilter);
            int magFilter = (int)OGLEnumConverter.GetTextureMagFilter(sampler.MagFilter);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, wrapS);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, wrapT);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, minFilter);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, magFilter);

            float[] color = new float[]
            {
                sampler.BorderColor.Red,
                sampler.BorderColor.Green,
                sampler.BorderColor.Blue,
                sampler.BorderColor.Alpha
            };

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBorderColor, color);
        }
    }
}
