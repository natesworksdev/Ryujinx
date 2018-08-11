using OpenTK.Graphics.OpenGL;
using Ryujinx.Graphics.Gal.Texture;
using System;

namespace Ryujinx.Graphics.Gal.OpenGL
{
    class OGLTexture : IGalTexture
    {
        private OGLCachedResource<TCE> TextureCache;

        public OGLTexture()
        {
            TextureCache = new OGLCachedResource<TCE>(DeleteTexture);
        }

        public void LockCache()
        {
            TextureCache.Lock();
        }

        public void UnlockCache()
        {
            TextureCache.Unlock();
        }

        private static void DeleteTexture(TCE CachedTexture)
        {
            GL.DeleteTexture(CachedTexture.Handle);
        }

        public void Create(long Key, byte[] Data, GalImage Image)
        {
            int Handle = GL.GenTexture();

            TextureCache.AddOrUpdate(Key, new TCE(Handle, Image), (uint)Data.Length);

            GL.BindTexture(TextureTarget.Texture2D, Handle);

            const int Level  = 0; //TODO: Support mipmap textures.
            const int Border = 0;

            if (IsCompressedTextureFormat(Image.Format))
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
                if (Image.Format > GalImageFormat.ConvertedBegin && Image.Format < GalImageFormat.ConvertedEnd)
                {
                    int TextureBlockWidth  = GetAstcBlockWidth(Image.Format);
                    int TextureBlockHeight = GetAstcBlockHeight(Image.Format);

                    Data = ASTCDecoder.DecodeToRGBA8888(
                        Data,
                        TextureBlockWidth,
                        TextureBlockHeight, 1,
                        Image.Width,
                        Image.Height, 1);

                    Image.Format = GalImageFormat.A8B8G8R8;
                }

                (PixelInternalFormat InternalFormat, PixelFormat Format, PixelType Type) = OGLEnumConverter.GetImageFormat(Image.Format);

                GL.TexImage2D(
                    TextureTarget.Texture2D,
                    Level,
                    InternalFormat,
                    Image.Width,
                    Image.Height,
                    Border,
                    Format,
                    Type,
                    Data);
            }

            int SwizzleR = (int)OGLEnumConverter.GetTextureSwizzle(Image.XSource);
            int SwizzleG = (int)OGLEnumConverter.GetTextureSwizzle(Image.YSource);
            int SwizzleB = (int)OGLEnumConverter.GetTextureSwizzle(Image.ZSource);
            int SwizzleA = (int)OGLEnumConverter.GetTextureSwizzle(Image.WSource);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureSwizzleR, SwizzleR);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureSwizzleG, SwizzleG);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureSwizzleB, SwizzleB);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureSwizzleA, SwizzleA);
        }

        public void CreateFb(long Key, long Size, GalImage Image)
        {
            if (!TryGetTCE(Key, out TCE Texture))
            {
                Texture = new TCE();

                TextureCache.AddOrUpdate(Key, Texture, Size);
            }

            Texture.EnsureSetup(Image);
        }

        public bool TryGetTCE(long Key, out TCE CachedTexture)
        {
            if (TextureCache.TryGetValue(Key, out CachedTexture))
            {
                return true;
            }

            CachedTexture = null;

            return false;
        }

        private static int GetAstcBlockWidth(GalImageFormat Format)
        {
            switch (Format)
            {
                case GalImageFormat.Astc2D4x4:   return 4;
                case GalImageFormat.Astc2D5x5:   return 5;
                case GalImageFormat.Astc2D6x6:   return 6;
                case GalImageFormat.Astc2D8x8:   return 8;
                case GalImageFormat.Astc2D10x10: return 10;
                case GalImageFormat.Astc2D12x12: return 12;
                case GalImageFormat.Astc2D5x4:   return 5;
                case GalImageFormat.Astc2D6x5:   return 6;
                case GalImageFormat.Astc2D8x6:   return 8;
                case GalImageFormat.Astc2D10x8:  return 10;
                case GalImageFormat.Astc2D12x10: return 12;
                case GalImageFormat.Astc2D8x5:   return 8;
                case GalImageFormat.Astc2D10x5:  return 10;
                case GalImageFormat.Astc2D10x6:  return 10;
            }

            throw new ArgumentException(nameof(Format));
        }

        private static int GetAstcBlockHeight(GalImageFormat Format)
        {
            switch (Format)
            {
                case GalImageFormat.Astc2D4x4:   return 4;
                case GalImageFormat.Astc2D5x5:   return 5;
                case GalImageFormat.Astc2D6x6:   return 6;
                case GalImageFormat.Astc2D8x8:   return 8;
                case GalImageFormat.Astc2D10x10: return 10;
                case GalImageFormat.Astc2D12x12: return 12;
                case GalImageFormat.Astc2D5x4:   return 4;
                case GalImageFormat.Astc2D6x5:   return 5;
                case GalImageFormat.Astc2D8x6:   return 6;
                case GalImageFormat.Astc2D10x8:  return 8;
                case GalImageFormat.Astc2D12x10: return 10;
                case GalImageFormat.Astc2D8x5:   return 5;
                case GalImageFormat.Astc2D10x5:  return 5;
                case GalImageFormat.Astc2D10x6:  return 6;
            }

            throw new ArgumentException(nameof(Format));
        }

        public bool TryGetCachedTexture(long Key, long DataSize, out GalImage Image)
        {
            if (TextureCache.TryGetSize(Key, out long Size) && Size == DataSize)
            {
                if (TextureCache.TryGetValue(Key, out TCE CachedTexture))
                {
                    Image = CachedTexture.Image;

                    return true;
                }
            }

            Image = default(GalImage);

            return false;
        }

        public void Bind(long Key, int Index)
        {
            if (TextureCache.TryGetValue(Key, out TCE CachedTexture))
            {
                GL.ActiveTexture(TextureUnit.Texture0 + Index);

                GL.BindTexture(TextureTarget.Texture2D, CachedTexture.Handle);
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

        private static bool IsCompressedTextureFormat(GalImageFormat Format)
        {
            switch (Format)
            {
                case GalImageFormat.BC6H_UF16:
                case GalImageFormat.BC6H_SF16:
                case GalImageFormat.BC7U:
                case GalImageFormat.BC1:
                case GalImageFormat.BC2:
                case GalImageFormat.BC3:
                case GalImageFormat.BC4:
                case GalImageFormat.BC5:
                    return true;
            }

            return false;
        }
    }
}
