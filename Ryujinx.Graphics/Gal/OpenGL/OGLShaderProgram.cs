using System;

namespace Ryujinx.Graphics.Gal.OpenGL
{
    struct OGLShaderProgram
    {
        public OGLShaderStage Vertex;
        public OGLShaderStage TessControl;
        public OGLShaderStage TessEvaluation;
        public OGLShaderStage Geometry;
        public OGLShaderStage Fragment;

        public override bool Equals(object obj)
        {
            if (!(obj is OGLShaderProgram program))
            {
                return false;
            }

            return Vertex         == program.Vertex         &&
                   TessControl    == program.TessControl    &&
                   TessEvaluation == program.TessEvaluation &&
                   Geometry       == program.Geometry       &&
                   Fragment       == program.Fragment;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                Vertex,
                TessControl,
                TessEvaluation,
                Geometry,
                Fragment);
        }
    }
}