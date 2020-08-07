using System;

namespace Ryujinx.Memory
{
    public sealed class WritableRegion : IDisposable
    {
        private readonly IAddressSpaceManager _mm;
        private readonly ulong _va;

        private bool NeedsWriteback => _mm != null;

        public Memory<byte> Memory { get; }

        public WritableRegion(IAddressSpaceManager mm, ulong va, Memory<byte> memory)
        {
            _mm = mm;
            _va = va;
            Memory = memory;
        }

        public void Dispose()
        {
            if (NeedsWriteback)
            {
                _mm.Write(_va, Memory.Span);
            }
        }
    }
}
