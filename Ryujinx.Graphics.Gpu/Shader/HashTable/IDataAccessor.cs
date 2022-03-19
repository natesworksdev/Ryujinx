using System;

namespace Ryujinx.Graphics.Gpu.Shader.HashTable
{
    public interface IDataAccessor
    {
        ReadOnlySpan<byte> GetSpan(int offset, int length);
    }
}
