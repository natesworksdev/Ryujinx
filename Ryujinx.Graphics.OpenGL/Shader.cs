using OpenTK.Graphics.OpenGL;
using Ryujinx.Graphics.GAL;
using Ryujinx.Graphics.Shader;
using System;

namespace Ryujinx.Graphics.OpenGL
{
    class Shader : IShader
    {
        public int Handle { get; private set; }

        public Shader(ShaderStage stage, string code)
        {
            Handle = GL.CreateShader(GetShaderType(stage));

            GL.ShaderSource(Handle, code);
            GL.CompileShader(Handle);
        }

        public Shader(ShaderStage stage, byte[] code)
        {
            int handle = GL.CreateShader(GetShaderType(stage));

            Handle = handle;

            GL.ShaderBinary(1, ref handle, (BinaryFormat)All.ShaderBinaryFormatSpirVArb, code, code.Length);
            GL.SpecializeShader(handle, "main", 0, (int[])null, (int[])null);
        }

        private static ShaderType GetShaderType(ShaderStage stage)
        {
            return stage switch
            {
                ShaderStage.Compute => ShaderType.ComputeShader,
                ShaderStage.Vertex => ShaderType.VertexShader,
                ShaderStage.TessellationControl => ShaderType.TessControlShader,
                ShaderStage.TessellationEvaluation => ShaderType.TessEvaluationShader,
                ShaderStage.Geometry => ShaderType.GeometryShader,
                ShaderStage.Fragment => ShaderType.FragmentShader,
                _ => ShaderType.VertexShader
            };
        }

        public void Dispose()
        {
            if (Handle != 0)
            {
                GL.DeleteShader(Handle);

                Handle = 0;
            }
        }
    }
}
