using System;

namespace Ryujinx.Memory
{
    public interface IAddressSpaceManager
    {
        void Map(ulong va, ulong pa, ulong size);
        void Unmap(ulong va, ulong size);

        T Read<T>(ulong va) where T : unmanaged;
        void Read(ulong va, Span<byte> data);

        void Write<T>(ulong va, T value) where T : unmanaged;
        void Write(ulong va, ReadOnlySpan<byte> data);

        ReadOnlySpan<byte> GetSpan(ulong va, int size, bool tracked = false);
        WritableRegion GetWritableRegion(ulong va, int size);
        ref T GetRef<T>(ulong va) where T : unmanaged;

        bool IsMapped(ulong va);
        ulong GetPhysicalAddress(ulong va);
    }
}