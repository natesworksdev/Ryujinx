using ARMeilleure.Common;
using System;

namespace ARMeilleure.Memory
{
    public static class MemoryManagement
    {
        public static IntPtr Allocate(ulong size)
        {
            switch (Platform.CurrentSystem)
            {
                case Platform.System.Windows:
                    return MemoryManagementWindows.Allocate((IntPtr)size);
                case Platform.System.Linux:
                case Platform.System.OSX:
                    return MemoryManagementUnix.Allocate(size);
                default:
                    throw new PlatformNotSupportedException(Platform.CurrentSystem.ToString());
            }
        }

        public static IntPtr AllocateWriteTracked(ulong size)
        {
            switch (Platform.CurrentSystem)
            {
                case Platform.System.Windows:
                    return MemoryManagementWindows.AllocateWriteTracked((IntPtr)size);
                case Platform.System.Linux:
                case Platform.System.OSX:
                    return MemoryManagementUnix.Allocate(size);
                default:
                    throw new PlatformNotSupportedException(Platform.CurrentSystem.ToString());
            }
        }

        public static bool Commit(IntPtr address, ulong size)
        {
            switch (Platform.CurrentSystem)
            {
                case Platform.System.Windows:
                    return MemoryManagementWindows.Commit(address, (IntPtr)size);
                case Platform.System.Linux:
                case Platform.System.OSX:
                    return MemoryManagementUnix.Commit(address, size);
                default:
                    throw new PlatformNotSupportedException(Platform.CurrentSystem.ToString());
            }
        }

        public static void Reprotect(IntPtr address, ulong size, MemoryProtection permission)
        {
            bool result;

            switch (Platform.CurrentSystem)
            {
                case Platform.System.Windows:
                    result = MemoryManagementWindows.Reprotect(address, (IntPtr)size, permission);
                    break;
                case Platform.System.Linux:
                case Platform.System.OSX:
                    result = MemoryManagementUnix.Reprotect(address, size, permission);
                    break;
                default:
                    throw new PlatformNotSupportedException(Platform.CurrentSystem.ToString());
            }

            if (!result)
            {
                throw new MemoryProtectionException(permission);
            }
        }

        public static IntPtr Reserve(ulong size)
        {
            switch (Platform.CurrentSystem)
            {
                case Platform.System.Windows:
                    return MemoryManagementWindows.Reserve((IntPtr)size);
                case Platform.System.Linux:
                case Platform.System.OSX:
                    return MemoryManagementUnix.Reserve(size);
                default:
                    throw new PlatformNotSupportedException(Platform.CurrentSystem.ToString());
            }
        }

        public static bool Free(IntPtr address)
        {
            switch (Platform.CurrentSystem)
            {
                case Platform.System.Windows:
                    return MemoryManagementWindows.Free(address);
                case Platform.System.Linux:
                case Platform.System.OSX:
                    return MemoryManagementUnix.Free(address);
                default:
                    throw new PlatformNotSupportedException(Platform.CurrentSystem.ToString());
            }
        }

        public static bool FlushInstructionCache(IntPtr address, ulong size)
        {
            switch (Platform.CurrentSystem)
            {
                case Platform.System.Windows:
                    return MemoryManagementWindows.FlushInstructionCache(address, (IntPtr)size);
                case Platform.System.Linux:
                case Platform.System.OSX:
                    return MemoryManagementUnix.FlushInstructionCache(address, size);
                default:
                    throw new PlatformNotSupportedException(Platform.CurrentSystem.ToString());
            }
        }
    }
}