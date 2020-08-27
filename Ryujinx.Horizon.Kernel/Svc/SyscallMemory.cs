using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Kernel.Common;
using Ryujinx.Horizon.Kernel.Memory;
using Ryujinx.Horizon.Kernel.Process;

namespace Ryujinx.Horizon.Kernel.Svc
{
    public partial class Syscall
    {
        public Result SetHeapSize(ulong size, out ulong position)
        {
            if ((size & 0xfffffffe001fffff) != 0)
            {
                position = 0;

                return CheckResult(KernelResult.InvalidSize);
            }

            KProcess process = _context.Scheduler.GetCurrentProcess();

            return CheckResult(process.MemoryManager.SetHeapSize(size, out position));
        }

        public Result SetMemoryAttribute(
            ulong position,
            ulong size,
            KMemoryAttribute attributeMask,
            KMemoryAttribute attributeValue)
        {
            if (!PageAligned(position))
            {
                return CheckResult(KernelResult.InvalidAddress);
            }

            if (!PageAligned(size) || size == 0)
            {
                return CheckResult(KernelResult.InvalidSize);
            }

            KMemoryAttribute attributes = attributeMask | attributeValue;

            if (attributes != attributeMask ||
               (attributes | KMemoryAttribute.Uncached) != KMemoryAttribute.Uncached)
            {
                return CheckResult(KernelResult.InvalidCombination);
            }

            KProcess process = _context.Scheduler.GetCurrentProcess();

            Result result = process.MemoryManager.SetMemoryAttribute(
                position,
                size,
                attributeMask,
                attributeValue);

            return CheckResult(result);
        }

        public Result MapMemory(ulong dst, ulong src, ulong size)
        {
            if (!PageAligned(src | dst))
            {
                return CheckResult(KernelResult.InvalidAddress);
            }

            if (!PageAligned(size) || size == 0)
            {
                return CheckResult(KernelResult.InvalidSize);
            }

            if (src + size <= src || dst + size <= dst)
            {
                return CheckResult(KernelResult.InvalidMemState);
            }

            KProcess currentProcess = _context.Scheduler.GetCurrentProcess();

            if (!currentProcess.MemoryManager.InsideAddrSpace(src, size))
            {
                return CheckResult(KernelResult.InvalidMemState);
            }

            if (currentProcess.MemoryManager.OutsideStackRegion(dst, size) ||
                currentProcess.MemoryManager.InsideHeapRegion(dst, size) ||
                currentProcess.MemoryManager.InsideAliasRegion(dst, size))
            {
                return CheckResult(KernelResult.InvalidMemRange);
            }

            KProcess process = _context.Scheduler.GetCurrentProcess();

            return CheckResult(process.MemoryManager.Map(dst, src, size));
        }

        public Result UnmapMemory(ulong dst, ulong src, ulong size)
        {
            if (!PageAligned(src | dst))
            {
                return CheckResult(KernelResult.InvalidAddress);
            }

            if (!PageAligned(size) || size == 0)
            {
                return CheckResult(KernelResult.InvalidSize);
            }

            if (src + size <= src || dst + size <= dst)
            {
                return CheckResult(KernelResult.InvalidMemState);
            }

            KProcess currentProcess = _context.Scheduler.GetCurrentProcess();

            if (!currentProcess.MemoryManager.InsideAddrSpace(src, size))
            {
                return CheckResult(KernelResult.InvalidMemState);
            }

            if (currentProcess.MemoryManager.OutsideStackRegion(dst, size) ||
                currentProcess.MemoryManager.InsideHeapRegion(dst, size) ||
                currentProcess.MemoryManager.InsideAliasRegion(dst, size))
            {
                return CheckResult(KernelResult.InvalidMemRange);
            }

            KProcess process = _context.Scheduler.GetCurrentProcess();

            return CheckResult(process.MemoryManager.Unmap(dst, src, size));
        }

        public Result QueryMemory(ulong infoPtr, ulong pageInfoPtr, ulong address)
        {
            return CheckResult(QueryProcessMemory(infoPtr, pageInfoPtr, KHandleTable.SelfProcessHandle, address));
        }

        public Result QueryMemory(out MemoryInfo info, ulong address)
        {
            return CheckResult(QueryProcessMemory(out info, KHandleTable.SelfProcessHandle, address));
        }

        public Result MapSharedMemory(int handle, ulong address, ulong size, KMemoryPermission permission)
        {
            if (!PageAligned(address))
            {
                return CheckResult(KernelResult.InvalidAddress);
            }

            if (!PageAligned(size) || size == 0)
            {
                return CheckResult(KernelResult.InvalidSize);
            }

            if (address + size <= address)
            {
                return CheckResult(KernelResult.InvalidMemState);
            }

            if ((permission | KMemoryPermission.Write) != KMemoryPermission.ReadAndWrite)
            {
                return CheckResult(KernelResult.InvalidPermission);
            }

            KProcess currentProcess = _context.Scheduler.GetCurrentProcess();

            KSharedMemory sharedMemory = currentProcess.HandleTable.GetObject<KSharedMemory>(handle);

            if (sharedMemory == null)
            {
                return CheckResult(KernelResult.InvalidHandle);
            }

            if (currentProcess.MemoryManager.IsInvalidRegion(address, size) ||
                currentProcess.MemoryManager.InsideHeapRegion(address, size) ||
                currentProcess.MemoryManager.InsideAliasRegion(address, size))
            {
                return CheckResult(KernelResult.InvalidMemRange);
            }

            return sharedMemory.MapIntoProcess(
                currentProcess.MemoryManager,
                address,
                size,
                currentProcess,
                permission);
        }

        public Result UnmapSharedMemory(int handle, ulong address, ulong size)
        {
            if (!PageAligned(address))
            {
                return CheckResult(KernelResult.InvalidAddress);
            }

            if (!PageAligned(size) || size == 0)
            {
                return CheckResult(KernelResult.InvalidSize);
            }

            if (address + size <= address)
            {
                return CheckResult(KernelResult.InvalidMemState);
            }

            KProcess currentProcess = _context.Scheduler.GetCurrentProcess();

            KSharedMemory sharedMemory = currentProcess.HandleTable.GetObject<KSharedMemory>(handle);

            if (sharedMemory == null)
            {
                return CheckResult(KernelResult.InvalidHandle);
            }

            if (currentProcess.MemoryManager.IsInvalidRegion(address, size) ||
                currentProcess.MemoryManager.InsideHeapRegion(address, size) ||
                currentProcess.MemoryManager.InsideAliasRegion(address, size))
            {
                return CheckResult(KernelResult.InvalidMemRange);
            }

            return sharedMemory.UnmapFromProcess(
                currentProcess.MemoryManager,
                address,
                size,
                currentProcess);
        }

        public Result CreateTransferMemory(ulong address, ulong size, KMemoryPermission permission, out int handle)
        {
            handle = 0;

            if (!PageAligned(address))
            {
                return CheckResult(KernelResult.InvalidAddress);
            }

            if (!PageAligned(size) || size == 0)
            {
                return CheckResult(KernelResult.InvalidSize);
            }

            if (address + size <= address)
            {
                return CheckResult(KernelResult.InvalidMemState);
            }

            if (permission > KMemoryPermission.ReadAndWrite || permission == KMemoryPermission.Write)
            {
                return CheckResult(KernelResult.InvalidPermission);
            }

            KProcess process = _context.Scheduler.GetCurrentProcess();

            KResourceLimit resourceLimit = process.ResourceLimit;

            if (resourceLimit != null && !resourceLimit.Reserve(LimitableResource.TransferMemory, 1))
            {
                return CheckResult(KernelResult.ResLimitExceeded);
            }

            void CleanUpForError()
            {
                resourceLimit?.Release(LimitableResource.TransferMemory, 1);
            }

            if (!process.MemoryManager.InsideAddrSpace(address, size))
            {
                CleanUpForError();

                return CheckResult(KernelResult.InvalidMemState);
            }

            KTransferMemory transferMemory = new KTransferMemory(_context);

            Result result = transferMemory.Initialize(address, size, permission);

            if (result != Result.Success)
            {
                CleanUpForError();

                return CheckResult(result);
            }

            result = process.HandleTable.GenerateHandle(transferMemory, out handle);

            transferMemory.DecrementReferenceCount();

            return CheckResult(result);
        }

        public Result MapPhysicalMemory(ulong address, ulong size)
        {
            if (!PageAligned(address))
            {
                return CheckResult(KernelResult.InvalidAddress);
            }

            if (!PageAligned(size) || size == 0)
            {
                return CheckResult(KernelResult.InvalidSize);
            }

            if (address + size <= address)
            {
                return CheckResult(KernelResult.InvalidMemRange);
            }

            KProcess currentProcess = _context.Scheduler.GetCurrentProcess();

            if ((currentProcess.PersonalMmHeapPagesCount & 0xfffffffffffff) == 0)
            {
                return CheckResult(KernelResult.InvalidState);
            }

            if (!currentProcess.MemoryManager.InsideAddrSpace(address, size) ||
                 currentProcess.MemoryManager.OutsideAliasRegion(address, size))
            {
                return CheckResult(KernelResult.InvalidMemRange);
            }

            KProcess process = _context.Scheduler.GetCurrentProcess();

            return CheckResult(process.MemoryManager.MapPhysicalMemory(address, size));
        }

        public Result UnmapPhysicalMemory(ulong address, ulong size)
        {
            if (!PageAligned(address))
            {
                return CheckResult(KernelResult.InvalidAddress);
            }

            if (!PageAligned(size) || size == 0)
            {
                return CheckResult(KernelResult.InvalidSize);
            }

            if (address + size <= address)
            {
                return CheckResult(KernelResult.InvalidMemRange);
            }

            KProcess currentProcess = _context.Scheduler.GetCurrentProcess();

            if ((currentProcess.PersonalMmHeapPagesCount & 0xfffffffffffff) == 0)
            {
                return CheckResult(KernelResult.InvalidState);
            }

            if (!currentProcess.MemoryManager.InsideAddrSpace(address, size) ||
                 currentProcess.MemoryManager.OutsideAliasRegion(address, size))
            {
                return CheckResult(KernelResult.InvalidMemRange);
            }

            KProcess process = _context.Scheduler.GetCurrentProcess();

            return CheckResult(process.MemoryManager.UnmapPhysicalMemory(address, size));
        }

        public Result CreateSharedMemory(out int handle, ulong size, KMemoryPermission ownerPermission, KMemoryPermission userPermission)
        {
            handle = 0;

            if (!PageAligned(size) || size == 0 || size >= 0x100000000UL)
            {
                return CheckResult(KernelResult.InvalidSize);
            }

            if (ownerPermission != KMemoryPermission.Read &&
                ownerPermission != KMemoryPermission.ReadAndWrite)
            {
                return CheckResult(KernelResult.InvalidPermission);
            }

            if (userPermission != KMemoryPermission.DontCare &&
                userPermission != KMemoryPermission.Read &&
                userPermission != KMemoryPermission.ReadAndWrite)
            {
                return CheckResult(KernelResult.InvalidPermission);
            }

            KSharedMemory sharedMemory = new KSharedMemory(_context);

            using var _ = new OnScopeExit(sharedMemory.DecrementReferenceCount);

            KProcess currentProcess = _context.Scheduler.GetCurrentProcess();

            Result result = sharedMemory.Initialize(currentProcess, size, ownerPermission, userPermission);

            if (result != Result.Success)
            {
                return CheckResult(result);
            }

            return CheckResult(currentProcess.HandleTable.GenerateHandle(sharedMemory, out handle));
        }

        public Result MapTransferMemory(int handle, ulong address, ulong size, KMemoryPermission permission)
        {
            if (!PageAligned(address))
            {
                return CheckResult(KernelResult.InvalidAddress);
            }

            if (!PageAligned(size) || size == 0)
            {
                return CheckResult(KernelResult.InvalidSize);
            }

            if (size + address <= address)
            {
                return CheckResult(KernelResult.InvalidMemState);
            }

            if (permission != KMemoryPermission.None &&
                permission != KMemoryPermission.Read &&
                permission != KMemoryPermission.ReadAndWrite)
            {
                return CheckResult(KernelResult.InvalidState);
            }

            KProcess currentProcess = _context.Scheduler.GetCurrentProcess();

            KTransferMemory transferMemory = currentProcess.HandleTable.GetObject<KTransferMemory>(handle);

            if (transferMemory == null)
            {
                return CheckResult(KernelResult.InvalidHandle);
            }

            if (!currentProcess.MemoryManager.CanContain(address, size, KMemoryState.TransferMemoryIsolated))
            {
                return CheckResult(KernelResult.InvalidMemRange);
            }

            return CheckResult(transferMemory.Map(address, size, permission));
        }

        public Result UnmapTransferMemory(int handle, ulong address, ulong size)
        {
            if (!PageAligned(address))
            {
                return CheckResult(KernelResult.InvalidAddress);
            }

            if (!PageAligned(size) || size == 0)
            {
                return CheckResult(KernelResult.InvalidSize);
            }

            if (size + address <= address)
            {
                return CheckResult(KernelResult.InvalidMemState);
            }

            KProcess currentProcess = _context.Scheduler.GetCurrentProcess();

            KTransferMemory transferMemory = currentProcess.HandleTable.GetObject<KTransferMemory>(handle);

            if (transferMemory == null)
            {
                return CheckResult(KernelResult.InvalidHandle);
            }

            if (!currentProcess.MemoryManager.CanContain(address, size, KMemoryState.TransferMemoryIsolated))
            {
                return CheckResult(KernelResult.InvalidMemRange);
            }

            return CheckResult(transferMemory.Unmap(address, size));
        }

        public Result MapProcessMemory(ulong dst, int processHandle, ulong src, ulong size)
        {
            return CheckResult(MapOrUnmapProcessMemory(dst, processHandle, src, size, map: true));
        }

        public Result UnmapProcessMemory(ulong dst, int processHandle, ulong src, ulong size)
        {
            return CheckResult(MapOrUnmapProcessMemory(dst, processHandle, src, size, map: false));
        }

        private Result MapOrUnmapProcessMemory(ulong dst, int processHandle, ulong src, ulong size, bool map)
        {
            if (!PageAligned(dst) || !PageAligned(src))
            {
                return CheckResult(KernelResult.InvalidAddress);
            }

            if (!PageAligned(size) || size == 0)
            {
                return CheckResult(KernelResult.InvalidSize);
            }

            if (size + dst <= dst || size + src <= src)
            {
                return CheckResult(KernelResult.InvalidMemState);
            }

            KProcess currentProcess = _context.Scheduler.GetCurrentProcess();

            KProcess sourceProcess = currentProcess.HandleTable.GetObject<KProcess>(processHandle);

            if (sourceProcess == null)
            {
                return CheckResult(KernelResult.InvalidHandle);
            }

            if (!sourceProcess.MemoryManager.InsideAddrSpace(src, size))
            {
                return CheckResult(KernelResult.InvalidMemState);
            }

            if (!currentProcess.MemoryManager.CanContain(dst, size, KMemoryState.ProcessMemory))
            {
                return CheckResult(KernelResult.InvalidMemRange);
            }

            KPageList pageList = new KPageList();

            Result result = sourceProcess.MemoryManager.GetPages(
                src,
                size / KMemoryManager.PageSize,
                KMemoryState.MapProcessAllowed,
                KMemoryState.MapProcessAllowed,
                KMemoryPermission.None,
                KMemoryPermission.None,
                KMemoryAttribute.Mask,
                KMemoryAttribute.None,
                pageList);

            if (result != Result.Success)
            {
                return CheckResult(result);
            }

            if (map)
            {
                return CheckResult(currentProcess.MemoryManager.MapPages(dst, pageList, KMemoryState.ProcessMemory, KMemoryPermission.ReadAndWrite));
            }
            else
            {
                return CheckResult(currentProcess.MemoryManager.UnmapPages(dst, pageList, KMemoryState.ProcessMemory));
            }
        }

        public Result QueryProcessMemory(ulong infoPtr, ulong pageInfoPtr, int processHandle, ulong address)
        {
            Result result = QueryProcessMemory(out MemoryInfo info, processHandle, address);

            if (result != Result.Success)
            {
                return CheckResult(result);
            }

            return KernelTransfer.KernelToUser(_context, infoPtr, info)
                ? Result.Success
                : KernelResult.InvalidMemState;
        }

        public Result QueryProcessMemory(out MemoryInfo info, int processHandle, ulong address)
        {
            KProcess process = _context.Scheduler.GetCurrentProcess().HandleTable.GetKProcess(processHandle);

            if (process == null)
            {
                info = default;

                return CheckResult(KernelResult.InvalidHandle);
            }

            KMemoryInfo blockInfo = process.MemoryManager.QueryMemory(address);

            info = new MemoryInfo(
                blockInfo.Address,
                blockInfo.Size,
                (int)blockInfo.State & 0xff,
                (int)blockInfo.Attribute,
                (int)blockInfo.Permission,
                blockInfo.IpcRefCount,
                blockInfo.DeviceRefCount);

            return CheckResult(Result.Success);
        }

        public Result MapProcessCodeMemory(int processHandle, ulong dst, ulong src, ulong size)
        {
            if (!PageAligned(dst) || !PageAligned(src))
            {
                return CheckResult(KernelResult.InvalidAddress);
            }

            if (!PageAligned(size) || size == 0)
            {
                return CheckResult(KernelResult.InvalidSize);
            }

            KProcess currentProcess = _context.Scheduler.GetCurrentProcess();

            KProcess targetProcess = currentProcess.HandleTable.GetObject<KProcess>(processHandle);

            if (targetProcess == null)
            {
                return CheckResult(KernelResult.InvalidHandle);
            }

            if (targetProcess.MemoryManager.OutsideAddrSpace(dst, size) ||
                targetProcess.MemoryManager.OutsideAddrSpace(src, size) ||
                targetProcess.MemoryManager.InsideAliasRegion(dst, size) ||
                targetProcess.MemoryManager.InsideHeapRegion(dst, size))
            {
                return CheckResult(KernelResult.InvalidMemRange);
            }

            if (size + dst <= dst || size + src <= src)
            {
                return CheckResult(KernelResult.InvalidMemState);
            }

            return CheckResult(targetProcess.MemoryManager.MapProcessCodeMemory(dst, src, size));
        }

        public Result UnmapProcessCodeMemory(int handle, ulong dst, ulong src, ulong size)
        {
            if (!PageAligned(dst) || !PageAligned(src))
            {
                return CheckResult(KernelResult.InvalidAddress);
            }

            if (!PageAligned(size) || size == 0)
            {
                return CheckResult(KernelResult.InvalidSize);
            }

            KProcess currentProcess = _context.Scheduler.GetCurrentProcess();

            KProcess targetProcess = currentProcess.HandleTable.GetObject<KProcess>(handle);

            if (targetProcess == null)
            {
                return CheckResult(KernelResult.InvalidHandle);
            }

            if (targetProcess.MemoryManager.OutsideAddrSpace(dst, size) ||
                targetProcess.MemoryManager.OutsideAddrSpace(src, size) ||
                targetProcess.MemoryManager.InsideAliasRegion(dst, size) ||
                targetProcess.MemoryManager.InsideHeapRegion(dst, size))
            {
                return CheckResult(KernelResult.InvalidMemRange);
            }

            if (size + dst <= dst || size + src <= src)
            {
                return CheckResult(KernelResult.InvalidMemState);
            }

            return CheckResult(targetProcess.MemoryManager.UnmapProcessCodeMemory(dst, src, size));
        }

        public Result SetProcessMemoryPermission(int handle, ulong src, ulong size, KMemoryPermission permission)
        {
            if (!PageAligned(src))
            {
                return CheckResult(KernelResult.InvalidAddress);
            }

            if (!PageAligned(size) || size == 0)
            {
                return CheckResult(KernelResult.InvalidSize);
            }

            if (permission != KMemoryPermission.None &&
                permission != KMemoryPermission.Read &&
                permission != KMemoryPermission.ReadAndWrite &&
                permission != KMemoryPermission.ReadAndExecute)
            {
                return CheckResult(KernelResult.InvalidPermission);
            }

            KProcess currentProcess = _context.Scheduler.GetCurrentProcess();

            KProcess targetProcess = currentProcess.HandleTable.GetObject<KProcess>(handle);

            if (targetProcess == null)
            {
                return CheckResult(KernelResult.InvalidHandle);
            }

            if (targetProcess.MemoryManager.OutsideAddrSpace(src, size))
            {
                return CheckResult(KernelResult.InvalidMemState);
            }

            return CheckResult(targetProcess.MemoryManager.SetProcessMemoryPermission(src, size, permission));
        }

        private static bool PageAligned(ulong address)
        {
            return (address & (KMemoryManager.PageSize - 1)) == 0;
        }
    }
}
