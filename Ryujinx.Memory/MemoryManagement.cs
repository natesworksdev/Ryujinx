using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ryujinx.Memory
{
    public static class MemoryManagement
    {
        public static IntPtr Allocate(ulong size)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                IntPtr sizeNint = new IntPtr((long)size);

                return MemoryManagementWin.Allocate(sizeNint);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
                     RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return MemoryManagementUnix.Allocate(size);
            }
            else
            {
                throw new PlatformNotSupportedException();
            }
        }

        public static bool Free(IntPtr address)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return MemoryManagementWin.Free(address);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
                     RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return MemoryManagementUnix.Free(address);
            }
            else
            {
                throw new PlatformNotSupportedException();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool QueryModifiedPages(IntPtr address, IntPtr size, Span<IntPtr> addresses, out ulong count)
        {
            // This is only supported on windows, but returning
            // false (failed) is also valid for platforms without
            // write tracking support on the OS.
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return MemoryManagementWin.QueryModifiedPages(address, size, addresses, out count);
            }
            else
            {
                count = 0;

                return false;
            }
        }
    }
}