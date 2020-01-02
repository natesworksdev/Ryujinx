using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ryujinx.Memory
{
    static class MemoryManagementWin
    {
        [Flags]
        private enum AllocationType : uint
        {
            Commit = 0x1000,
            Reserve = 0x2000,
            Decommit = 0x4000,
            Release = 0x8000,
            Reset = 0x80000,
            Physical = 0x400000,
            TopDown = 0x100000,
            WriteWatch = 0x200000,
            LargePages = 0x20000000
        }

        [Flags]
        private enum MemoryProtection : uint
        {
            NoAccess = 0x01,
            ReadOnly = 0x02,
            ReadWrite = 0x04,
            WriteCopy = 0x08,
            Execute = 0x10,
            ExecuteRead = 0x20,
            ExecuteReadWrite = 0x40,
            ExecuteWriteCopy = 0x80,
            GuardModifierflag = 0x100,
            NoCacheModifierflag = 0x200,
            WriteCombineModifierflag = 0x400
        }

        private enum WriteWatchFlags : uint
        {
            None = 0,
            Reset = 1
        }

        [DllImport("kernel32.dll")]
        private static extern IntPtr VirtualAlloc(
            IntPtr lpAddress,
            IntPtr dwSize,
            AllocationType flAllocationType,
            MemoryProtection flProtect);

        [DllImport("kernel32.dll")]
        private static extern bool VirtualFree(
            IntPtr lpAddress,
            IntPtr dwSize,
            AllocationType dwFreeType);

        [DllImport("kernel32.dll")]
        private unsafe static extern int GetWriteWatch(
            WriteWatchFlags dwFlags,
            IntPtr lpBaseAddress,
            IntPtr dwRegionSize,
            IntPtr* lpAddresses,
            ref ulong lpdwCount,
            out uint lpdwGranularity);

        public static IntPtr Allocate(IntPtr size)
        {
            const AllocationType flags =
                AllocationType.Reserve |
                AllocationType.Commit |
                AllocationType.WriteWatch;

            IntPtr ptr = VirtualAlloc(IntPtr.Zero, size, flags, MemoryProtection.ReadWrite);

            if (ptr == IntPtr.Zero)
            {
                throw new OutOfMemoryException();
            }

            return ptr;
        }

        public static bool Free(IntPtr address)
        {
            return VirtualFree(address, IntPtr.Zero, AllocationType.Release);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe static bool QueryModifiedPages(IntPtr address, IntPtr size, Span<IntPtr> addresses, out ulong count)
        {
            ulong pagesCount = (ulong)addresses.Length;

            int result;

            fixed (IntPtr* addressesPtr = addresses)
            {
                result = GetWriteWatch(
                    WriteWatchFlags.Reset,
                    address,
                    size,
                    addressesPtr,
                    ref pagesCount,
                    out _);
            }

            count = pagesCount;

            return result == 0;
        }
    }
}