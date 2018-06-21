using System.Collections.Generic;

namespace Ryujinx.Graphics.Gal
{
    public interface IGalShader
    {
        void Create(IGalMemory Memory, long Tag, GalShaderType Type);

        IEnumerable<ShaderDeclInfo> GetTextureUsage(long Tag);

        void SetConstBuffer(long Tag, int Cbuf, byte[] Data);

        void SetUniform1(string UniformName, int Value);

        void SetUniform2F(string UniformName, float X, float Y);

        void Bind(long Tag);

        void BindProgram();
    }
}