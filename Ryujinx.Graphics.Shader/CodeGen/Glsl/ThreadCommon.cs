namespace Ryujinx.Graphics.Shader.CodeGen.Glsl
{
    static class ThreadCommon
    {
        public static string GenerateUnpackMask(string u64mask)
        {
            return $"(gl_SubGroupInvocationARB < 32 ? unpackUint2x32({u64mask}).x : unpackUint2x32({u64mask}).y)";
        }
    }
}
