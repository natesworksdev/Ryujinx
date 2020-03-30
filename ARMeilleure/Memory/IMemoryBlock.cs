using System;

namespace ARMeilleure.Memory
{
    public interface IMemoryBlock : IDisposable
    {
        IntPtr Pointer { get; }

        T Read<T>(ulong address) where T : unmanaged;
        void Write<T>(ulong address, T value) where T : unmanaged;

        IntPtr GetPointer(ulong address, int size);
        Span<byte> GetSpan(ulong address, int size);
        ref T GetRef<T>(ulong address) where T : unmanaged;
    }
}
