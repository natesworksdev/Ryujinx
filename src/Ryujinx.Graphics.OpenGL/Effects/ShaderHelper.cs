using Silk.NET.OpenGL.Legacy;

namespace Ryujinx.Graphics.OpenGL.Effects
{
    internal static class ShaderHelper
    {
        public static uint CompileProgram(GL api, string shaderCode, ShaderType shaderType)
        {
            var shader = api.CreateShader(shaderType);
            api.ShaderSource(shader, shaderCode);
            api.CompileShader(shader);

            var program = api.CreateProgram();
            api.AttachShader(program, shader);
            api.LinkProgram(program);

            api.DetachShader(program, shader);
            api.DeleteShader(shader);

            return program;
        }

        public static uint CompileProgram(GL api, string[] shaders, ShaderType shaderType)
        {
            var shader = api.CreateShader(shaderType);
            api.ShaderSource(shader, (uint)shaders.Length, shaders, 0);
            api.CompileShader(shader);

            var program = api.CreateProgram();
            api.AttachShader(program, shader);
            api.LinkProgram(program);

            api.DetachShader(program, shader);
            api.DeleteShader(shader);

            return program;
        }
    }
}
