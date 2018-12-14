using System;
using System.Collections.Generic;

namespace Ryujinx.Graphics.Gal
{
    public interface IGalShader
    {
        void Create(IGalMemory Memory, long Key, GalShaderType Type);

        void Create(IGalMemory Memory, long VpAPos, long Key, GalShaderType Type);

        ShaderDeclInfo GetGlobalMemoryUsage(long Key);
        IEnumerable<ShaderDeclInfo> GetConstBufferUsage(long Key);
        IEnumerable<ShaderDeclInfo> GetTextureUsage(long Key);

        void SetGlobalMemory(IntPtr Data, int Size);

        int GetGlobalMemorySize();

        void Bind(long Key);

        void Unbind(GalShaderType Type);

        void BindProgram();
    }
}