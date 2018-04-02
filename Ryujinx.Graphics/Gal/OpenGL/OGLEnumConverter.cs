using OpenTK.Graphics.OpenGL;
using System;

namespace Ryujinx.Graphics.Gal.OpenGL
{
    static class OGLEnumConverter
    {
        public static ShaderType GetShaderType(GalShaderType Type)
        {
            switch (Type)
            {
                case GalShaderType.Vertex:         return ShaderType.VertexShader;
                case GalShaderType.TessControl:    return ShaderType.TessControlShader;
                case GalShaderType.TessEvaluation: return ShaderType.TessEvaluationShader;
                case GalShaderType.Geometry:       return ShaderType.GeometryShader;
                case GalShaderType.Fragment:       return ShaderType.FragmentShader;
            }

            throw new ArgumentException(nameof(Type));
        }

        public static PixelInternalFormat GetCompressedTextureFormat(GalTextureFormat Format)
        {
            switch (Format)
            {
                case GalTextureFormat.BC1: return PixelInternalFormat.CompressedRgbaS3tcDxt1Ext;
                case GalTextureFormat.BC2: return PixelInternalFormat.CompressedRgbaS3tcDxt3Ext;
                case GalTextureFormat.BC3: return PixelInternalFormat.CompressedRgbaS3tcDxt5Ext;
            }

            throw new NotImplementedException(Format.ToString());
        }
    }
}