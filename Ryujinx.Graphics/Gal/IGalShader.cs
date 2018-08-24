using System.Collections.Generic;

namespace Ryujinx.Graphics.Gal
{
    public interface IGalShader
    {
        void Create(long Key, byte[] BinaryA, byte[] BinaryB, GalShaderType Type);

        IEnumerable<ShaderDeclInfo> GetConstBufferUsage(long Key);
        IEnumerable<ShaderDeclInfo> GetTextureUsage(long Key);

        void Bind(long Key);

        void Unbind(GalShaderType Type);

        void BindProgram();
    }
}