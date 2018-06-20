using System;

namespace Ryujinx.Graphics.Gal.Shader
{
    static class UniformBinding
    {
        public const int BuffersPerStage = 12; //ARB_uniform_buffer

        public static int Get(GalShaderType Stage, int Cbuf)
        {
            return GetStageIndex(Stage) * BuffersPerStage + Cbuf;
        }

        private static int GetStageIndex(GalShaderType Stage)
        {
            switch (Stage)
            {
                case GalShaderType.Vertex:         return 0;
                case GalShaderType.Fragment:       return 1;
                case GalShaderType.Geometry:       return 2;
                case GalShaderType.TessControl:    return 3;
                case GalShaderType.TessEvaluation: return 4;
            }

            throw new ArgumentException();
        }
    }
}