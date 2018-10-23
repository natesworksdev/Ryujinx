using OpenTK.Graphics.OpenGL;
using Ryujinx.Graphics.Texture;
using System;

namespace Ryujinx.Graphics.Gal.OpenGL
{
    class OglTexture : IGalTexture
    {
        private OglCachedResource<ImageHandler> _textureCache;
        public EventHandler<int> TextureDeleted { get; set; }
        
        public OglTexture()
        {
            _textureCache = new OglCachedResource<ImageHandler>(DeleteTexture);
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

            if (ImageUtils.IsCompressed(image.Format))
            {
                throw new InvalidOperationException("Surfaces with compressed formats are not supported!");
            }

            (PixelInternalFormat internalFmt,
             PixelFormat         format,
             PixelType           type) = OglEnumConverter.GetImageFormat(image.Format);

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
                InternalFormat internalFmt = OglEnumConverter.GetCompressedImageFormat(image.Format);

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

                    data = AstcDecoder.DecodeToRgba8888(
                        data,
                        textureBlockWidth,
                        textureBlockHeight, 1,
                        image.Width,
                        image.Height, 1);

                    image.Format = GalImageFormat.Rgba8 | GalImageFormat.Unorm;
                }

                (PixelInternalFormat internalFmt,
                 PixelFormat         format,
                 PixelType           type) = OglEnumConverter.GetImageFormat(image.Format);

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
            if (_textureCache.TryGetValue(key, out cachedImage))
            {
                return true;
            }

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
                    (int)OglEnumConverter.GetTextureSwizzle(image.XSource),
                    (int)OglEnumConverter.GetTextureSwizzle(image.YSource),
                    (int)OglEnumConverter.GetTextureSwizzle(image.ZSource),
                    (int)OglEnumConverter.GetTextureSwizzle(image.WSource)
                };

                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureSwizzleRgba, swizzleRgba);
            }
        }

        public void SetSampler(GalTextureSampler sampler)
        {
            int wrapS = (int)OglEnumConverter.GetTextureWrapMode(sampler.AddressU);
            int wrapT = (int)OglEnumConverter.GetTextureWrapMode(sampler.AddressV);

            int minFilter = (int)OglEnumConverter.GetTextureMinFilter(sampler.MinFilter, sampler.MipFilter);
            int magFilter = (int)OglEnumConverter.GetTextureMagFilter(sampler.MagFilter);

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
