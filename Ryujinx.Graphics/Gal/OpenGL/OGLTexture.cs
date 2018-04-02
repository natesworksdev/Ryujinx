using OpenTK.Graphics.OpenGL;
using System;

namespace Ryujinx.Graphics.Gal.OpenGL
{
    class OGLTexture
    {
        private OGLShader Shader;

        private int[] Textures;

        private int CurrentTextureIndex;

        public OGLTexture(OGLShader Shader)
        {
            this.Shader = Shader;

            Textures = new int[80];
        }

        public void UpdateTextures(Func<int, GalShaderType, GalTexture> RequestTextureCallback)
        {
            CurrentTextureIndex = 0;

            UpdateTextures(RequestTextureCallback, GalShaderType.Vertex);
            UpdateTextures(RequestTextureCallback, GalShaderType.TessControl);
            UpdateTextures(RequestTextureCallback, GalShaderType.TessEvaluation);
            UpdateTextures(RequestTextureCallback, GalShaderType.Geometry);
            UpdateTextures(RequestTextureCallback, GalShaderType.Fragment);
        }

        private void UpdateTextures(Func<int, GalShaderType, GalTexture> RequestTextureCallback, GalShaderType ShaderType)
        {
            foreach (ShaderDeclInfo DeclInfo in Shader.GetTextureUsage(ShaderType))
            {
                GalTexture Texture = RequestTextureCallback(DeclInfo.Index, ShaderType);

                GL.ActiveTexture(TextureUnit.Texture0 + CurrentTextureIndex);

                UploadTexture(Texture);

                int Location = GL.GetUniformLocation(Shader.CurrentProgramHandle, DeclInfo.Name);

                GL.Uniform1(Location, CurrentTextureIndex++);
            }
        }

        private void UploadTexture(GalTexture Texture)
        {
            int Handle = EnsureTextureInitialized(CurrentTextureIndex);

            GL.BindTexture(TextureTarget.Texture2D, Handle);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

            int W = Texture.Width;
            int H = Texture.Height;

            byte[] Data = Texture.Data;

            int Length = Data.Length;

            if (IsCompressedTextureFormat(Texture.Format))
            {
                PixelInternalFormat Pif = OGLEnumConverter.GetCompressedTextureFormat(Texture.Format);

                GL.CompressedTexImage2D(TextureTarget.Texture2D, 0, Pif, W, H, 0, Length, Data);
            }
            else
            {
                //TODO: Get those from Texture format.
                const PixelInternalFormat Pif = PixelInternalFormat.Rgba;

                const PixelFormat Pf = PixelFormat.Rgba;

                const PixelType Pt = PixelType.UnsignedByte;

                GL.TexImage2D(TextureTarget.Texture2D, 0, Pif, W, H, 0, Pf, Pt, Data);
            }
        }

        private static bool IsCompressedTextureFormat(GalTextureFormat Format)
        {
            return Format == GalTextureFormat.BC1 ||
                   Format == GalTextureFormat.BC2 ||
                   Format == GalTextureFormat.BC3;
        }

        private int EnsureTextureInitialized(int TextureIndex)
        {
            int Handle = Textures[TextureIndex];

            if (Handle == 0)
            {
                Handle = Textures[TextureIndex] = GL.GenTexture();
            }

            return Handle;
        }
    }
}