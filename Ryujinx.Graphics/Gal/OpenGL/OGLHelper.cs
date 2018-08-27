using OpenTK.Graphics.OpenGL;
using System;

namespace Ryujinx.Graphics.Gal.OpenGL
{
    public static class OGLHelper
    {
        public static unsafe void TexImage(
            TextureTarget Target,
            int Level,
            PixelInternalFormat InternalFormat,
            int Width,
            int Height,
            int Depth,
            int Border,
            PixelFormat PixelFormat,
            PixelType PixelType,
            byte[] Data)
        {
            switch (Target)
            {
                case TextureTarget.Texture1D:
                    GL.TexImage1D(
                        Target,
                        Level,
                        InternalFormat,
                        Width,
                        Border,
                        PixelFormat,
                        PixelType,
                        Data);
                    break;

                case TextureTarget.Texture2D:
                    GL.TexImage2D(
                        Target,
                        Level,
                        InternalFormat,
                        Width,
                        Height,
                        Border,
                        PixelFormat,
                        PixelType,
                        Data);
                    break;

                case TextureTarget.Texture2DArray:
                case TextureTarget.Texture3D:
                    //FIXME: Unstub depth when swizzle is fixed
                    Depth = 1;
                    GL.TexImage3D(
                        Target,
                        Level,
                        InternalFormat,
                        Width,
                        Height,
                        Depth,
                        Border,
                        PixelFormat,
                        PixelType,
                        Data);
                    break;

                case TextureTarget.TextureCubeMap:
                {
                    long FaceSize = Data.LongLength / 6;

                    for (int Face = 0; Face < 6; Face++)
                    {
                        fixed (byte* DataPtr = Data)
                        {
                            IntPtr Addr;

                            if (Data != null)
                            {
                                Addr = new IntPtr(DataPtr + FaceSize * Face);
                            }
                            else
                            {
                                Addr = new IntPtr(0);
                            }

                            GL.TexImage2D(
                                TextureTarget.TextureCubeMapPositiveX + Face,
                                Level,
                                InternalFormat,
                                Width,
                                Height,
                                Border,
                                PixelFormat,
                                PixelType,
                                Addr);
                        }
                    }
                    break;
                }

                default:
                    throw new NotImplementedException(Target.ToString());
            }
        }

        public static unsafe void CompressedTexImage(
            TextureTarget Target,
            int Level,
            InternalFormat InternalFormat,
            int Width,
            int Height,
            int Depth,
            int Border,
            byte[] Data)
        {
            switch (Target)
            {
                case TextureTarget.Texture1D:
                    GL.CompressedTexImage1D(
                        Target,
                        Level,
                        InternalFormat,
                        Width,
                        Border,
                        Data.Length,
                        Data);
                    break;

                case TextureTarget.Texture2D:
                    GL.CompressedTexImage2D(
                        Target,
                        Level,
                        InternalFormat,
                        Width,
                        Height,
                        Border,
                        Data.Length,
                        Data);
                    break;

                case TextureTarget.Texture2DArray:
                case TextureTarget.Texture3D:
                    //FIXME: Unstub depth when swizzle is fixed
                    Depth = 1;
                    GL.CompressedTexImage3D(
                        Target,
                        Level,
                        InternalFormat,
                        Width,
                        Height,
                        Depth,
                        Border,
                        Data.Length,
                        Data);
                    break;

                case TextureTarget.TextureCubeMap:
                {
                    //FIXME: This implies that all 6 faces are equal
                    int FaceSize = Data.Length / 6;

                    for (int Face = 0; Face < 6; Face++)
                    {
                        fixed (byte* DataPtr = Data)
                        {
                            IntPtr Addr;

                            if (Data != null)
                            {
                                Addr = new IntPtr(DataPtr + FaceSize * Face);
                            }
                            else
                            {
                                Addr = new IntPtr(0);
                            }

                            GL.CompressedTexImage2D(
                                TextureTarget.TextureCubeMapPositiveX + Face,
                                Level,
                                InternalFormat,
                                Width,
                                Height,
                                Border,
                                FaceSize,
                                Addr);
                        }
                    }
                    break;
                }

                default:
                    throw new NotImplementedException(Target.ToString());
            }
        }
    }
}