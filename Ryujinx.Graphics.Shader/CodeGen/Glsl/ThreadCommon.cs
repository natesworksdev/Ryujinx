using Ryujinx.Graphics.Shader.Translation;

namespace Ryujinx.Graphics.Shader.CodeGen.Glsl
{
    static class ThreadCommon
    {
        public static string GenerateMaskInvocation(ShaderConfig config, string invocation)
        {
            return config.GpuAccessor.QueryShaderMaxThreads32() ? invocation : $"({invocation} & 31)";
        }

        public static string GenerateUnpackMask(ShaderConfig config, string u64mask)
        {
            return config.GpuAccessor.QueryShaderMaxThreads32()
                ? $"unpackUint2x32({u64mask}).x"
                : $"(gl_SubGroupInvocationARB < 32 ? unpackUint2x32({u64mask}).x : unpackUint2x32({u64mask}).y)";
        }
    }
}
