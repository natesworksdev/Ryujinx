using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe ref T GetRef<T>(int offset) where T : unmanaged
        {
            if ((uint)offset >= Memory.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            return ref MemoryMarshal.Cast<byte, T>(Memory.Span.Slice(offset))[0];
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
