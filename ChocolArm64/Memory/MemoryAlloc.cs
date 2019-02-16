using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ChocolArm64.Memory
{
    public static class MemoryAlloc
    {
        public static bool HasWriteWatchSupport => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        public static IntPtr Allocate(ulong size)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                IntPtr sizeNint = new IntPtr((long)size);

                return MemoryAllocWindows.Allocate(sizeNint);
            }
            else
            {
                throw new PlatformNotSupportedException();
            }
        }

        public static IntPtr AllocateWriteTracked(ulong size)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                IntPtr sizeNint = new IntPtr((long)size);

                return MemoryAllocWindows.AllocateWriteTracked(sizeNint);
            }
            else
            {
                throw new PlatformNotSupportedException();
            }
        }

        public static bool Reprotect(IntPtr address, ulong size, MemoryProtection permission)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                IntPtr sizeNint = new IntPtr((long)size);

                return MemoryAllocWindows.Reprotect(address, sizeNint, permission);
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
                return MemoryAllocWindows.Free(address);
            }
            else
            {
                throw new PlatformNotSupportedException();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool GetModifiedPages(
            IntPtr    address,
            IntPtr    size,
            IntPtr[]  addresses,
            out ulong count)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return MemoryAllocWindows.GetModifiedPages(address, size, addresses, out count);
            }
            else
            {
                count = 0;

                return false;
            }
        }
    }
}