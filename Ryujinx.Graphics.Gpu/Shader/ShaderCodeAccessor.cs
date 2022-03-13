using Ryujinx.Common.Cache;
using Ryujinx.Graphics.Gpu.Memory;
using System;

namespace Ryujinx.Graphics.Gpu.Shader
{
    struct ShaderCodeAccessor : IDataAccessor
    {
        private readonly MemoryManager _memoryManager;
        private readonly ulong _baseAddress;

        public ShaderCodeAccessor(MemoryManager memoryManager, ulong baseAddress)
        {
            _memoryManager = memoryManager;
            _baseAddress = baseAddress;
        }

        public ReadOnlySpan<byte> GetSpan(int offset, int size)
        {
            return _memoryManager.GetSpanMapped(_baseAddress + (ulong)offset, size);
        }
    }
}