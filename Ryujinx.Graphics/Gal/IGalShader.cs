using System.Collections.Generic;

namespace Ryujinx.Graphics.Gal
{
    public interface IGalShader
    {
        void Create(IGalMemory Memory, long Key, GalShaderType Type);

        void Create(IGalMemory Memory, long VpAPos, long Key, GalShaderType Type);

        IEnumerable<ShaderDeclInfo> GetConstBufferUsage(long Key);
        IEnumerable<ShaderDeclInfo> GetTextureUsage(long Key);

        void SetExtraData(float FlipX, float FlipY, int Instance);

        void Bind(long Key);

        void Unbind(GalShaderType Type);

        void BindProgram();
    }
}