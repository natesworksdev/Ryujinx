using System;

namespace ARMeilleure.Memory
{
    public interface IMemoryBlock : IDisposable
    {
        IntPtr Pointer { get; }

        bool Commit(ulong offset, ulong size);

        void MapAsRx(ulong offset, ulong size);
        void MapAsRwx(ulong offset, ulong size);

        T Read<T>(ulong offset) where T : unmanaged;
        void Write<T>(ulong offset, T value) where T : unmanaged;

        IntPtr GetPointer(ulong offset, int size);
        Span<byte> GetSpan(ulong offset, int size);
        ref T GetRef<T>(ulong offset) where T : unmanaged;
    }
}
