using OpenTK.Graphics.OpenGL;
using Ryujinx.Graphics.Texture;
using System;

namespace Ryujinx.Graphics.Gal.OpenGL
{
    class OGLTexture : IGalTexture
    {
        private OGLCachedResource<GalImage, ImageHandler> TextureCache;

        public OGLTexture()
        {
            TextureCache = new OGLCachedResource<GalImage, ImageHandler>(CreateTexture, DeleteTexture);
        }

        public void LockCache()
        {
            TextureCache.Lock();
        }

        public void UnlockCache()
        {
            TextureCache.Unlock();
        }

        private static ImageHandler CreateTexture(GalImage Image)
        {
            const int Level  = 0; //TODO: Support mipmap textures.
            const int Border = 0;

            GalImage Native = ConvertToNativeImage(Image);

            int Handle = GL.GenTexture();

            GL.BindTexture(TextureTarget.Texture2D, Handle);

            //Compressed images are stored when they are set

            if (!ImageUtils.IsCompressed(Native.Format))
            {
                (PixelInternalFormat InternalFmt,
                 PixelFormat Format,
                 PixelType Type) = OGLEnumConverter.GetImageFormat(Native.Format);

                //TODO: Use ARB_texture_storage when available
                GL.TexImage2D(
                    TextureTarget.Texture2D,
                    Level,
                    InternalFmt,
                    Native.Width,
                    Native.Height,
                    Border,
                    Format,
                    Type,
                    IntPtr.Zero);
            }

            return new ImageHandler(Handle, Image);
        }

        private static void DeleteTexture(ImageHandler CachedImage)
        {
            GL.DeleteTexture(CachedImage.Handle);
        }

        public void CreateEmpty(long Key, int Size, GalImage Image)
        {
            TextureCache.CreateOrRecycle(Key, Image, Size);
        }

        public void CreatePBO(long Key, int Size, GalImage Image, int PBO)
        {
            const int Level = 0; //TODO: Support mipmap textures.

            GalImage Native = ConvertToNativeImage(Image);

            if (ImageUtils.IsCompressed(Native.Format))
            {
                throw new NotImplementedException("Compressed PBO creation is not implemented");
            }

            ImageHandler CachedImage = TextureCache.CreateOrRecycle(Key, Image, Size);

            GL.BindBuffer(BufferTarget.PixelUnpackBuffer, PBO);

            (_, PixelFormat Format, PixelType Type) = OGLEnumConverter.GetImageFormat(Native.Format);

            GL.BindTexture(TextureTarget.Texture2D, CachedImage.Handle);

            GL.TexSubImage2D(
                TextureTarget.Texture2D,
                Level,
                0,
                0,
                Native.Width,
                Native.Height,
                Format,
                Type,
                IntPtr.Zero);

            GL.BindBuffer(BufferTarget.PixelUnpackBuffer, 0);
        }

        public void CreateData(long Key, byte[] Data, GalImage Image)
        {
            const int Level  = 0; //TODO: Support mipmap textures.
            const int Border = 0;

            GalImage Native = ConvertToNativeImage(Image);

            byte[] NativeData = ConvertToNativeData(Image, Data);

            ImageHandler CachedImage = TextureCache.CreateOrRecycle(Key, Image, (uint)Data.Length);

            GL.BindTexture(TextureTarget.Texture2D, CachedImage.Handle);

            if (ImageUtils.IsCompressed(Native.Format))
            {
                InternalFormat InternalFmt = OGLEnumConverter.GetCompressedImageFormat(Native.Format);

                GL.CompressedTexImage2D(
                    TextureTarget.Texture2D,
                    Level,
                    InternalFmt,
                    Native.Width,
                    Native.Height,
                    Border,
                    Data.Length,
                    NativeData);
            }
            else
            {
                (_, PixelFormat Format, PixelType Type) = OGLEnumConverter.GetImageFormat(Native.Format);

                GL.TexSubImage2D(
                    TextureTarget.Texture2D,
                    Level,
                    0,
                    0,
                    Native.Width,
                    Native.Height,
                    Format,
                    Type,
                    NativeData);
            }
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
            if (!TextureCache.TryGetValue(Key, out ImageHandler CachedImage))
            {
                throw new InvalidOperationException();
            }

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

        private static GalImage ConvertToNativeImage(GalImage Image)
        {
            GalImageFormat Format = Image.Format & GalImageFormat.FormatMask;

            if (IsASTC(Image))
            {
                Image.Format = GalImageFormat.A8B8G8R8 | GalImageFormat.Unorm;
            }
            else if (Format == GalImageFormat.G8R8)
            {
                Image.Format = GalImageFormat.R8G8 | (Image.Format & GalImageFormat.TypeMask);
            }

            return Image;
        }

        private static byte[] ConvertToNativeData(GalImage Image, byte[] Data)
        {
            //TODO: Use KHR_texture_compression_astc_hdr when available

            GalImageFormat Format = Image.Format & GalImageFormat.FormatMask;

            if (IsASTC(Image))
            {
                int TextureBlockWidth = ImageUtils.GetBlockWidth(Image.Format);
                int TextureBlockHeight = ImageUtils.GetBlockHeight(Image.Format);

                return ASTCDecoder.DecodeToRGBA8888(
                    Data,
                    TextureBlockWidth,
                    TextureBlockHeight, 1,
                    Image.Width,
                    Image.Height, 1);
            }
            else if (Format == GalImageFormat.G8R8)
            {
                return ImageConverter.G8R8ToR8G8(
                    Data,
                    Image.Width,
                    Image.Height,
                    1);
            }
            else
            {
                return Data;
            }
        }

        private static bool IsASTC(GalImage Image)
        {
            GalImageFormat Format = Image.Format & GalImageFormat.FormatMask;

            return Format >= GalImageFormat.ASTC_BEGIN &&
                   Format <= GalImageFormat.ASTC_END;
        }
    }
}
