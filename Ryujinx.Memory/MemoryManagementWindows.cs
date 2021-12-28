using Ryujinx.Memory.WindowsShared;
using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Ryujinx.Memory
{
    [SupportedOSPlatform("windows")]
    static class MemoryManagementWindows
    {
        private const int PageSize = 0x1000;

        private static readonly IntPtr InvalidHandleValue = new IntPtr(-1);
        private static readonly IntPtr CurrentProcessHandle = new IntPtr(-1);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr VirtualAlloc(
            IntPtr lpAddress,
            IntPtr dwSize,
            AllocationType flAllocationType,
            MemoryProtection flProtect);

        [DllImport("KernelBase.dll", SetLastError = true)]
        private static extern IntPtr VirtualAlloc2(
            IntPtr process,
            IntPtr lpAddress,
            IntPtr dwSize,
            AllocationType flAllocationType,
            MemoryProtection flProtect,
            IntPtr extendedParameters,
            ulong parameterCount);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool VirtualProtect(
            IntPtr lpAddress,
            IntPtr dwSize,
            MemoryProtection flNewProtect,
            out MemoryProtection lpflOldProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool VirtualFree(IntPtr lpAddress, IntPtr dwSize, AllocationType dwFreeType);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr CreateFileMapping(
            IntPtr hFile,
            IntPtr lpFileMappingAttributes,
            FileMapProtection flProtect,
            uint dwMaximumSizeHigh,
            uint dwMaximumSizeLow,
            [MarshalAs(UnmanagedType.LPWStr)] string lpName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr MapViewOfFile(
            IntPtr hFileMappingObject,
            uint dwDesiredAccess,
            uint dwFileOffsetHigh,
            uint dwFileOffsetLow,
            IntPtr dwNumberOfBytesToMap);

        [DllImport("KernelBase.dll", SetLastError = true)]
        private static extern IntPtr MapViewOfFile3(
            IntPtr hFileMappingObject,
            IntPtr process,
            IntPtr baseAddress,
            ulong offset,
            IntPtr dwNumberOfBytesToMap,
            ulong allocationType,
            MemoryProtection dwDesiredAccess,
            IntPtr extendedParameters,
            ulong parameterCount);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool UnmapViewOfFile(IntPtr lpBaseAddress);

        [DllImport("KernelBase.dll", SetLastError = true)]
        private static extern bool UnmapViewOfFile2(IntPtr process, IntPtr lpBaseAddress, ulong unmapFlags);

        [DllImport("kernel32.dll")]
        private static extern uint GetLastError();

        public static IntPtr Allocate(IntPtr size)
        {
            return AllocateInternal(size, AllocationType.Reserve | AllocationType.Commit);
        }

        public static IntPtr Reserve(IntPtr size, bool viewCompatible)
        {
            if (viewCompatible)
            {
                return AllocateInternal2(size, AllocationType.Reserve | AllocationType.ReservePlaceholder);
            }

            return AllocateInternal(size, AllocationType.Reserve);
        }

        private static IntPtr AllocateInternal(IntPtr size, AllocationType flags = 0)
        {
            IntPtr ptr = VirtualAlloc(IntPtr.Zero, size, flags, MemoryProtection.ReadWrite);

            if (ptr == IntPtr.Zero)
            {
                throw new OutOfMemoryException();
            }

            return ptr;
        }

        private static IntPtr AllocateInternal2(IntPtr size, AllocationType flags = 0)
        {
            IntPtr ptr = VirtualAlloc2(CurrentProcessHandle, IntPtr.Zero, size, flags, MemoryProtection.NoAccess, IntPtr.Zero, 0);

            if (ptr == IntPtr.Zero)
            {
                throw new OutOfMemoryException();
            }

            return ptr;
        }

        public static bool Commit(IntPtr location, IntPtr size)
        {
            return VirtualAlloc(location, size, AllocationType.Commit, MemoryProtection.ReadWrite) != IntPtr.Zero;
        }

        public static bool Decommit(IntPtr location, IntPtr size)
        {
            return VirtualFree(location, size, AllocationType.Decommit);
        }

        public static void MapView(IntPtr sharedMemory, ulong srcOffset, IntPtr location, IntPtr size)
        {
            IntPtr endLocation = (nint)location + (nint)size;

            while (location != endLocation)
            {
                VirtualFree(location, (IntPtr)PageSize, AllocationType.Release | AllocationType.PreservePlaceholder);

                var ptr = MapViewOfFile3(
                    sharedMemory,
                    CurrentProcessHandle,
                    location,
                    srcOffset,
                    (IntPtr)PageSize,
                    0x4000,
                    MemoryProtection.ReadWrite,
                    IntPtr.Zero,
                    0);

                if (ptr == IntPtr.Zero)
                {
                    throw new Exception($"MapViewOfFile3 failed with error code 0x{GetLastError():X}.");
                }

                location += PageSize;
                srcOffset += PageSize;
            }
        }

        public static void UnmapView(IntPtr location, IntPtr size)
        {
            IntPtr endLocation = (nint)location + (int)size;

            while (location != endLocation)
            {
                bool result = UnmapViewOfFile2(CurrentProcessHandle, location, 2);
                if (!result)
                {
                    throw new Exception($"UnmapViewOfFile2 failed with error code 0x{GetLastError():X}.");
                }

                location += PageSize;
            }
        }

        public static bool Reprotect(IntPtr address, IntPtr size, MemoryPermission permission, bool forView)
        {
            if (forView)
            {
                ulong uaddress = (ulong)address;
                ulong usize = (ulong)size;
                while (usize > 0)
                {
                    if (!VirtualProtect((IntPtr)uaddress, (IntPtr)PageSize, GetProtection(permission), out _))
                    {
                        return false;
                    }

                    uaddress += PageSize;
                    usize -= PageSize;
                }

                return true;
            }
            else
            {
                return VirtualProtect(address, size, GetProtection(permission), out _);
            }
        }

        private static MemoryProtection GetProtection(MemoryPermission permission)
        {
            return permission switch
            {
                MemoryPermission.None => MemoryProtection.NoAccess,
                MemoryPermission.Read => MemoryProtection.ReadOnly,
                MemoryPermission.ReadAndWrite => MemoryProtection.ReadWrite,
                MemoryPermission.ReadAndExecute => MemoryProtection.ExecuteRead,
                MemoryPermission.ReadWriteExecute => MemoryProtection.ExecuteReadWrite,
                MemoryPermission.Execute => MemoryProtection.Execute,
                _ => throw new MemoryProtectionException(permission)
            };
        }

        public static bool Free(IntPtr address)
        {
            return VirtualFree(address, IntPtr.Zero, AllocationType.Release);
        }

        public static IntPtr CreateSharedMemory(IntPtr size, bool reserve)
        {
            var prot = reserve ? FileMapProtection.SectionReserve : FileMapProtection.SectionCommit;

            IntPtr handle = CreateFileMapping(
                InvalidHandleValue,
                IntPtr.Zero,
                FileMapProtection.PageReadWrite | prot,
                (uint)(size.ToInt64() >> 32),
                (uint)size.ToInt64(),
                null);

            if (handle == IntPtr.Zero)
            {
                throw new OutOfMemoryException();
            }

            return handle;
        }

        public static void DestroySharedMemory(IntPtr handle)
        {
            if (!CloseHandle(handle))
            {
                throw new ArgumentException("Invalid handle.", nameof(handle));
            }
        }

        public static IntPtr MapSharedMemory(IntPtr handle)
        {
            IntPtr ptr = MapViewOfFile(handle, 4 | 2, 0, 0, IntPtr.Zero);

            if (ptr == IntPtr.Zero)
            {
                throw new OutOfMemoryException();
            }

            return ptr;
        }

        public static void UnmapSharedMemory(IntPtr address)
        {
            if (!UnmapViewOfFile(address))
            {
                throw new ArgumentException("Invalid address.", nameof(address));
            }
        }
    }
}