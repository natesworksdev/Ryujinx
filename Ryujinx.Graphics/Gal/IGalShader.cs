using System.Collections.Generic;

namespace Ryujinx.Graphics.Gal
{
    public interface IGalShader
    {
        void Create(long KeyA, long KeyB, byte[] BinaryA, byte[] BinaryB, GalShaderType Type);

        bool TryGetSize(long Key, out long Size);

        IEnumerable<ShaderDeclInfo> GetConstBufferUsage(long Key);
        IEnumerable<ShaderDeclInfo> GetTextureUsage(long Key);

        void Bind(long Key);

        void Unbind(GalShaderType Type);

        void BindProgram();
    }
}