using ChocolArm64.State;
using Ryujinx.Common.Logging;

using static Ryujinx.HLE.HOS.ErrorCode;

namespace Ryujinx.HLE.HOS.Kernel
{
    internal partial class SvcHandler
    {
        private void SvcSetHeapSize(AThreadState threadState)
        {
            ulong size = threadState.X1;

            if ((size & 0xFFFFFFFE001FFFFF) != 0)
            {
                Logger.PrintWarning(LogClass.KernelSvc, $"Heap size 0x{size:x16} is not aligned!");

                threadState.X0 = MakeError(ErrorModule.Kernel, KernelErr.InvalidSize);

                return;
            }

            long result = _process.MemoryManager.TrySetHeapSize((long)size, out long position);

            threadState.X0 = (ulong)result;

            if (result == 0)
                threadState.X1 = (ulong)position;
            else
                Logger.PrintWarning(LogClass.KernelSvc, $"Operation failed with error 0x{result:x}!");
        }

        private void SvcSetMemoryAttribute(AThreadState threadState)
        {
            long position = (long)threadState.X0;
            long size     = (long)threadState.X1;

            if (!PageAligned(position))
            {
                Logger.PrintWarning(LogClass.KernelSvc, $"Address 0x{position:x16} is not page aligned!");

                threadState.X0 = MakeError(ErrorModule.Kernel, KernelErr.InvalidAddress);

                return;
            }

            if (!PageAligned(size) || size == 0)
            {
                Logger.PrintWarning(LogClass.KernelSvc, $"Size 0x{size:x16} is not page aligned or is zero!");

                threadState.X0 = MakeError(ErrorModule.Kernel, KernelErr.InvalidSize);

                return;
            }

            MemoryAttribute attributeMask  = (MemoryAttribute)threadState.X2;
            MemoryAttribute attributeValue = (MemoryAttribute)threadState.X3;

            MemoryAttribute attributes = attributeMask | attributeValue;

            if (attributes != attributeMask ||
               (attributes | MemoryAttribute.Uncached) != MemoryAttribute.Uncached)
            {
                Logger.PrintWarning(LogClass.KernelSvc, "Invalid memory attributes!");

                threadState.X0 = MakeError(ErrorModule.Kernel, KernelErr.InvalidMaskValue);

                return;
            }

            long result = _process.MemoryManager.SetMemoryAttribute(
                position,
                size,
                attributeMask,
                attributeValue);

            if (result != 0)
                Logger.PrintWarning(LogClass.KernelSvc, $"Operation failed with error 0x{result:x}!");
            else
                _memory.StopObservingRegion(position, size);

            threadState.X0 = (ulong)result;
        }

        private void SvcMapMemory(AThreadState threadState)
        {
            long dst  = (long)threadState.X0;
            long src  = (long)threadState.X1;
            long size = (long)threadState.X2;

            if (!PageAligned(src | dst))
            {
                Logger.PrintWarning(LogClass.KernelSvc, "Addresses are not page aligned!");

                threadState.X0 = MakeError(ErrorModule.Kernel, KernelErr.InvalidAddress);

                return;
            }

            if (!PageAligned(size) || size == 0)
            {
                Logger.PrintWarning(LogClass.KernelSvc, $"Size 0x{size:x16} is not page aligned or is zero!");

                threadState.X0 = MakeError(ErrorModule.Kernel, KernelErr.InvalidSize);

                return;
            }

            if ((ulong)(src + size) <= (ulong)src || (ulong)(dst + size) <= (ulong)dst)
            {
                Logger.PrintWarning(LogClass.KernelSvc, "Addresses outside of range!");

                threadState.X0 = MakeError(ErrorModule.Kernel, KernelErr.NoAccessPerm);

                return;
            }

            if (!InsideAddrSpace(src, size))
            {
                Logger.PrintWarning(LogClass.KernelSvc, $"Src address 0x{src:x16} out of range!");

                threadState.X0 = MakeError(ErrorModule.Kernel, KernelErr.NoAccessPerm);

                return;
            }

            if (!InsideNewMapRegion(dst, size))
            {
                Logger.PrintWarning(LogClass.KernelSvc, $"Dst address 0x{dst:x16} out of range!");

                threadState.X0 = MakeError(ErrorModule.Kernel, KernelErr.InvalidMemRange);

                return;
            }

            long result = _process.MemoryManager.Map(src, dst, size);

            if (result != 0) Logger.PrintWarning(LogClass.KernelSvc, $"Operation failed with error 0x{result:x}!");

            threadState.X0 = (ulong)result;
        }

        private void SvcUnmapMemory(AThreadState threadState)
        {
            long dst  = (long)threadState.X0;
            long src  = (long)threadState.X1;
            long size = (long)threadState.X2;

            if (!PageAligned(src | dst))
            {
                Logger.PrintWarning(LogClass.KernelSvc, "Addresses are not page aligned!");

                threadState.X0 = MakeError(ErrorModule.Kernel, KernelErr.InvalidAddress);

                return;
            }

            if (!PageAligned(size) || size == 0)
            {
                Logger.PrintWarning(LogClass.KernelSvc, $"Size 0x{size:x16} is not page aligned or is zero!");

                threadState.X0 = MakeError(ErrorModule.Kernel, KernelErr.InvalidSize);

                return;
            }

            if ((ulong)(src + size) <= (ulong)src || (ulong)(dst + size) <= (ulong)dst)
            {
                Logger.PrintWarning(LogClass.KernelSvc, "Addresses outside of range!");

                threadState.X0 = MakeError(ErrorModule.Kernel, KernelErr.NoAccessPerm);

                return;
            }

            if (!InsideAddrSpace(src, size))
            {
                Logger.PrintWarning(LogClass.KernelSvc, $"Src address 0x{src:x16} out of range!");

                threadState.X0 = MakeError(ErrorModule.Kernel, KernelErr.NoAccessPerm);

                return;
            }

            if (!InsideNewMapRegion(dst, size))
            {
                Logger.PrintWarning(LogClass.KernelSvc, $"Dst address 0x{dst:x16} out of range!");

                threadState.X0 = MakeError(ErrorModule.Kernel, KernelErr.InvalidMemRange);

                return;
            }

            long result = _process.MemoryManager.Unmap(src, dst, size);

            if (result != 0) Logger.PrintWarning(LogClass.KernelSvc, $"Operation failed with error 0x{result:x}!");

            threadState.X0 = (ulong)result;
        }

        private void SvcQueryMemory(AThreadState threadState)
        {
            long infoPtr  = (long)threadState.X0;
            long position = (long)threadState.X2;

            KMemoryInfo blkInfo = _process.MemoryManager.QueryMemory(position);

            _memory.WriteInt64(infoPtr + 0x00, blkInfo.Position);
            _memory.WriteInt64(infoPtr + 0x08, blkInfo.Size);
            _memory.WriteInt32(infoPtr + 0x10, (int)blkInfo.State & 0xff);
            _memory.WriteInt32(infoPtr + 0x14, (int)blkInfo.Attribute);
            _memory.WriteInt32(infoPtr + 0x18, (int)blkInfo.Permission);
            _memory.WriteInt32(infoPtr + 0x1c, blkInfo.IpcRefCount);
            _memory.WriteInt32(infoPtr + 0x20, blkInfo.DeviceRefCount);
            _memory.WriteInt32(infoPtr + 0x24, 0);

            threadState.X0 = 0;
            threadState.X1 = 0;
        }

        private void SvcMapSharedMemory(AThreadState threadState)
        {
            int  handle   =  (int)threadState.X0;
            long position = (long)threadState.X1;
            long size     = (long)threadState.X2;

            if (!PageAligned(position))
            {
                Logger.PrintWarning(LogClass.KernelSvc, $"Address 0x{position:x16} is not page aligned!");

                threadState.X0 = MakeError(ErrorModule.Kernel, KernelErr.InvalidAddress);

                return;
            }

            if (!PageAligned(size) || size == 0)
            {
                Logger.PrintWarning(LogClass.KernelSvc, $"Size 0x{size:x16} is not page aligned or is zero!");

                threadState.X0 = MakeError(ErrorModule.Kernel, KernelErr.InvalidSize);

                return;
            }

            if ((ulong)(position + size) <= (ulong)position)
            {
                Logger.PrintWarning(LogClass.KernelSvc, $"Invalid region address 0x{position:x16} / size 0x{size:x16}!");

                threadState.X0 = MakeError(ErrorModule.Kernel, KernelErr.NoAccessPerm);

                return;
            }

            MemoryPermission permission = (MemoryPermission)threadState.X3;

            if ((permission | MemoryPermission.Write) != MemoryPermission.ReadAndWrite)
            {
                Logger.PrintWarning(LogClass.KernelSvc, $"Invalid permission {permission}!");

                threadState.X0 = MakeError(ErrorModule.Kernel, KernelErr.InvalidPermission);

                return;
            }

            KSharedMemory sharedMemory = _process.HandleTable.GetObject<KSharedMemory>(handle);

            if (sharedMemory == null)
            {
                Logger.PrintWarning(LogClass.KernelSvc, $"Invalid shared memory handle 0x{handle:x8}!");

                threadState.X0 = MakeError(ErrorModule.Kernel, KernelErr.InvalidHandle);

                return;
            }

            if (!InsideAddrSpace(position, size) || InsideMapRegion(position, size) || InsideHeapRegion(position, size))
            {
                Logger.PrintWarning(LogClass.KernelSvc, $"Address 0x{position:x16} out of range!");

                threadState.X0 = MakeError(ErrorModule.Kernel, KernelErr.NoAccessPerm);

                return;
            }

            if (sharedMemory.Size != size)
            {
                Logger.PrintWarning(LogClass.KernelSvc, $"Size 0x{size:x16} does not match shared memory size 0x{sharedMemory.Size:16}!");

                threadState.X0 = MakeError(ErrorModule.Kernel, KernelErr.InvalidSize);

                return;
            }

            long result = _process.MemoryManager.MapSharedMemory(sharedMemory, permission, position);

            if (result != 0) Logger.PrintWarning(LogClass.KernelSvc, $"Operation failed with error 0x{result:x}!");

            threadState.X0 = (ulong)result;
        }

        private void SvcUnmapSharedMemory(AThreadState threadState)
        {
            int  handle   =  (int)threadState.X0;
            long position = (long)threadState.X1;
            long size     = (long)threadState.X2;

            if (!PageAligned(position))
            {
                Logger.PrintWarning(LogClass.KernelSvc, $"Address 0x{position:x16} is not page aligned!");

                threadState.X0 = MakeError(ErrorModule.Kernel, KernelErr.InvalidAddress);

                return;
            }

            if (!PageAligned(size) || size == 0)
            {
                Logger.PrintWarning(LogClass.KernelSvc, $"Size 0x{size:x16} is not page aligned or is zero!");

                threadState.X0 = MakeError(ErrorModule.Kernel, KernelErr.InvalidSize);

                return;
            }

            if ((ulong)(position + size) <= (ulong)position)
            {
                Logger.PrintWarning(LogClass.KernelSvc, $"Invalid region address 0x{position:x16} / size 0x{size:x16}!");

                threadState.X0 = MakeError(ErrorModule.Kernel, KernelErr.NoAccessPerm);

                return;
            }

            KSharedMemory sharedMemory = _process.HandleTable.GetObject<KSharedMemory>(handle);

            if (sharedMemory == null)
            {
                Logger.PrintWarning(LogClass.KernelSvc, $"Invalid shared memory handle 0x{handle:x8}!");

                threadState.X0 = MakeError(ErrorModule.Kernel, KernelErr.InvalidHandle);

                return;
            }

            if (!InsideAddrSpace(position, size) || InsideMapRegion(position, size) || InsideHeapRegion(position, size))
            {
                Logger.PrintWarning(LogClass.KernelSvc, $"Address 0x{position:x16} out of range!");

                threadState.X0 = MakeError(ErrorModule.Kernel, KernelErr.NoAccessPerm);

                return;
            }

            long result = _process.MemoryManager.UnmapSharedMemory(position, size);

            if (result != 0) Logger.PrintWarning(LogClass.KernelSvc, $"Operation failed with error 0x{result:x}!");

            threadState.X0 = (ulong)result;
        }

        private void SvcCreateTransferMemory(AThreadState threadState)
        {
            long position = (long)threadState.X1;
            long size     = (long)threadState.X2;

            if (!PageAligned(position))
            {
                Logger.PrintWarning(LogClass.KernelSvc, $"Address 0x{position:x16} is not page aligned!");

                threadState.X0 = MakeError(ErrorModule.Kernel, KernelErr.InvalidAddress);

                return;
            }

            if (!PageAligned(size) || size == 0)
            {
                Logger.PrintWarning(LogClass.KernelSvc, $"Size 0x{size:x16} is not page aligned or is zero!");

                threadState.X0 = MakeError(ErrorModule.Kernel, KernelErr.InvalidAddress);

                return;
            }

            if ((ulong)(position + size) <= (ulong)position)
            {
                Logger.PrintWarning(LogClass.KernelSvc, $"Invalid region address 0x{position:x16} / size 0x{size:x16}!");

                threadState.X0 = MakeError(ErrorModule.Kernel, KernelErr.NoAccessPerm);

                return;
            }

            MemoryPermission permission = (MemoryPermission)threadState.X3;

            if (permission > MemoryPermission.ReadAndWrite || permission == MemoryPermission.Write)
            {
                Logger.PrintWarning(LogClass.KernelSvc, $"Invalid permission {permission}!");

                threadState.X0 = MakeError(ErrorModule.Kernel, KernelErr.InvalidPermission);

                return;
            }

            _process.MemoryManager.ReserveTransferMemory(position, size, permission);

            KTransferMemory transferMemory = new KTransferMemory(position, size);

            KernelResult result = _process.HandleTable.GenerateHandle(transferMemory, out int handle);

            threadState.X0 = (uint)result;
            threadState.X1 = (ulong)handle;
        }

        private void SvcMapPhysicalMemory(AThreadState threadState)
        {
            long position = (long)threadState.X0;
            long size     = (long)threadState.X1;

            if (!PageAligned(position))
            {
                Logger.PrintWarning(LogClass.KernelSvc, $"Address 0x{position:x16} is not page aligned!");

                threadState.X0 = MakeError(ErrorModule.Kernel, KernelErr.InvalidAddress);

                return;
            }

            if (!PageAligned(size) || size == 0)
            {
                Logger.PrintWarning(LogClass.KernelSvc, $"Size 0x{size:x16} is not page aligned or is zero!");

                threadState.X0 = MakeError(ErrorModule.Kernel, KernelErr.InvalidSize);

                return;
            }

            if ((ulong)(position + size) <= (ulong)position)
            {
                Logger.PrintWarning(LogClass.KernelSvc, $"Invalid region address 0x{position:x16} / size 0x{size:x16}!");

                threadState.X0 = MakeError(ErrorModule.Kernel, KernelErr.NoAccessPerm);

                return;
            }

            if (!InsideAddrSpace(position, size))
            {
                Logger.PrintWarning(LogClass.KernelSvc, $"Invalid address {position:x16}!");

                threadState.X0 = MakeError(ErrorModule.Kernel, KernelErr.NoAccessPerm);

                return;
            }

            long result = _process.MemoryManager.MapPhysicalMemory(position, size);

            if (result != 0) Logger.PrintWarning(LogClass.KernelSvc, $"Operation failed with error 0x{result:x}!");

            threadState.X0 = (ulong)result;
        }

        private void SvcUnmapPhysicalMemory(AThreadState threadState)
        {
            long position = (long)threadState.X0;
            long size     = (long)threadState.X1;

            if (!PageAligned(position))
            {
                Logger.PrintWarning(LogClass.KernelSvc, $"Address 0x{position:x16} is not page aligned!");

                threadState.X0 = MakeError(ErrorModule.Kernel, KernelErr.InvalidAddress);

                return;
            }

            if (!PageAligned(size) || size == 0)
            {
                Logger.PrintWarning(LogClass.KernelSvc, $"Size 0x{size:x16} is not page aligned or is zero!");

                threadState.X0 = MakeError(ErrorModule.Kernel, KernelErr.InvalidSize);

                return;
            }

            if ((ulong)(position + size) <= (ulong)position)
            {
                Logger.PrintWarning(LogClass.KernelSvc, $"Invalid region address 0x{position:x16} / size 0x{size:x16}!");

                threadState.X0 = MakeError(ErrorModule.Kernel, KernelErr.NoAccessPerm);

                return;
            }

            if (!InsideAddrSpace(position, size))
            {
                Logger.PrintWarning(LogClass.KernelSvc, $"Invalid address {position:x16}!");

                threadState.X0 = MakeError(ErrorModule.Kernel, KernelErr.NoAccessPerm);

                return;
            }

            long result = _process.MemoryManager.UnmapPhysicalMemory(position, size);

            if (result != 0) Logger.PrintWarning(LogClass.KernelSvc, $"Operation failed with error 0x{result:x}!");

            threadState.X0 = (ulong)result;
        }

        private static bool PageAligned(long position)
        {
            return (position & (KMemoryManager.PageSize - 1)) == 0;
        }

        private bool InsideAddrSpace(long position, long size)
        {
            ulong start = (ulong)position;
            ulong end   = (ulong)size + start;

            return start >= (ulong)_process.MemoryManager.AddrSpaceStart &&
                   end   <  (ulong)_process.MemoryManager.AddrSpaceEnd;
        }

        private bool InsideMapRegion(long position, long size)
        {
            ulong start = (ulong)position;
            ulong end   = (ulong)size + start;

            return start >= (ulong)_process.MemoryManager.MapRegionStart &&
                   end   <  (ulong)_process.MemoryManager.MapRegionEnd;
        }

        private bool InsideHeapRegion(long position, long size)
        {
            ulong start = (ulong)position;
            ulong end   = (ulong)size + start;

            return start >= (ulong)_process.MemoryManager.HeapRegionStart &&
                   end   <  (ulong)_process.MemoryManager.HeapRegionEnd;
        }

        private bool InsideNewMapRegion(long position, long size)
        {
            ulong start = (ulong)position;
            ulong end   = (ulong)size + start;

            return start >= (ulong)_process.MemoryManager.NewMapRegionStart &&
                   end   <  (ulong)_process.MemoryManager.NewMapRegionEnd;
        }
    }
}