using System;
using System.Runtime.InteropServices;

namespace ARMeilleure.Memory
{
    public static class MemoryManagement
    {
        private enum Platform
        {
            FreeBSD,
            Linux,
            OSX,
            Windows,
            Unknown
        }

        // Aside from letting us use switch statements instead, both the IL output and the JIT output is _terrible_
        // if you call the RuntimeInformation methods.
        private static readonly Platform CurrentOS = Platform.Unknown;

        static MemoryManagement()
        {
            var platforms = new[]
            {
                // (OSPlatform.FreeBSD, Platform.FreeBSD),
                (OSPlatform.Linux, Platform.Linux),
                (OSPlatform.OSX, Platform.OSX),
                (OSPlatform.Windows, Platform.Windows)
            };

            foreach (var platform in platforms) {
                if (RuntimeInformation.IsOSPlatform(platform.Item1))
                {
                    CurrentOS = platform.Item2;
                    return;
                }
            }

            throw new NotImplementedException("Current OS is not implemented");
        }

        public static IntPtr Allocate(ulong size)
        {
            switch (CurrentOS)
            {
                case Platform.Windows:
                    return MemoryManagementWindows.Allocate((IntPtr)size);
                case Platform.Linux:
                case Platform.OSX:
                    return MemoryManagementUnix.Allocate(size);
                default:
                    throw new PlatformNotSupportedException(CurrentOS.ToString());
            }
        }

        public static IntPtr AllocateWriteTracked(ulong size)
        {
            switch (CurrentOS)
            {
                case Platform.Windows:
                    return MemoryManagementWindows.AllocateWriteTracked((IntPtr)size);
                case Platform.Linux:
                case Platform.OSX:
                    return MemoryManagementUnix.Allocate(size);
                default:
                    throw new PlatformNotSupportedException(CurrentOS.ToString());
            }
        }

        public static bool Commit(IntPtr address, ulong size)
        {
            switch (CurrentOS)
            {
                case Platform.Windows:
                    return MemoryManagementWindows.Commit(address, (IntPtr)size);
                case Platform.Linux:
                case Platform.OSX:
                    return MemoryManagementUnix.Commit(address, size);
                default:
                    throw new PlatformNotSupportedException(CurrentOS.ToString());
            }
        }

        public static void Reprotect(IntPtr address, ulong size, MemoryProtection permission)
        {
            bool result;

            switch (CurrentOS)
            {
                case Platform.Windows:
                    result = MemoryManagementWindows.Reprotect(address, (IntPtr)size, permission);
                    break;
                case Platform.Linux:
                case Platform.OSX:
                    result = MemoryManagementUnix.Reprotect(address, size, permission);
                    break;
                default:
                    throw new PlatformNotSupportedException(CurrentOS.ToString());
            }

            if (!result)
            {
                throw new MemoryProtectionException(permission);
            }
        }

        public static IntPtr Reserve(ulong size)
        {
            switch (CurrentOS)
            {
                case Platform.Windows:
                    return MemoryManagementWindows.Reserve((IntPtr)size);
                case Platform.Linux:
                case Platform.OSX:
                    return MemoryManagementUnix.Reserve(size);
                default:
                    throw new PlatformNotSupportedException(CurrentOS.ToString());
            }
        }

        public static bool Free(IntPtr address)
        {
            switch (CurrentOS)
            {
                case Platform.Windows:
                    return MemoryManagementWindows.Free(address);
                case Platform.Linux:
                case Platform.OSX:
                    return MemoryManagementUnix.Free(address);
                default:
                    throw new PlatformNotSupportedException(CurrentOS.ToString());
            }
        }

        public static bool FlushInstructionCache(IntPtr address, ulong size)
        {
            switch (CurrentOS)
            {
                case Platform.Windows:
                    return MemoryManagementWindows.FlushInstructionCache(address, (IntPtr)size);
                case Platform.Linux:
                case Platform.OSX:
                    return MemoryManagementUnix.FlushInstructionCache(address, size);
                default:
                    throw new PlatformNotSupportedException(CurrentOS.ToString());
            }
        }
    }
}