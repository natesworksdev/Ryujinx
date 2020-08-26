using Ryujinx.Common;
using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Kernel.Common;
using Ryujinx.Horizon.Kernel.Process;
using System;

namespace Ryujinx.Horizon.Kernel.Memory
{
    class KTransferMemory : KAutoObject
    {
        private readonly KPageList _pageList;

        private KProcess _creator;

        private ulong _address;

        private readonly object _lock;

        private KMemoryPermission _ownerPermission;

        private bool _hasBeenInitialized;
        private bool _isMapped;

        public KTransferMemory(KernelContextInternal context) : base(context)
        {
            _pageList = new KPageList();
            _lock = new object();
        }

        public Result Initialize(ulong address, ulong size, KMemoryPermission permission)
        {
            KProcess creator = KernelContext.Scheduler.GetCurrentProcess();

            _creator = creator;

            Result result = creator.MemoryManager.BorrowTransferMemory(_pageList, address, size, permission);

            if (result != Result.Success)
            {
                return result;
            }

            creator.IncrementReferenceCount();

            _ownerPermission = permission;
            _address = address;
            _hasBeenInitialized = true;
            _isMapped = false;

            return result;
        }

        public Result Map(ulong address, ulong size, KMemoryPermission permission)
        {
            ulong pagesCountRounded = BitUtils.DivRoundUp(size, KMemoryManager.PageSize);

            if (_pageList.GetPagesCount() != pagesCountRounded)
            {
                return KernelResult.InvalidSize;
            }

            if (_ownerPermission != permission)
            {
                return KernelResult.InvalidState;
            }

            lock (_lock)
            {
                if (_isMapped)
                {
                    return KernelResult.InvalidState;
                }

                KMemoryState state = _ownerPermission == KMemoryPermission.None
                    ? KMemoryState.TransferMemoryIsolated
                    : KMemoryState.TransferMemory;

                KProcess currentProcess = KernelContext.Scheduler.GetCurrentProcess();

                Result result = currentProcess.MemoryManager.MapPages(address, _pageList, state, KMemoryPermission.ReadAndWrite);

                if (result != Result.Success)
                {
                    return result;
                }

                _isMapped = true;
            }

            return Result.Success;
        }

        public Result Unmap(ulong address, ulong size)
        {
            ulong pagesCountRounded = BitUtils.DivRoundUp(size, KMemoryManager.PageSize);

            if (_pageList.GetPagesCount() != pagesCountRounded)
            {
                return KernelResult.InvalidSize;
            }

            lock (_lock)
            {
                KMemoryState state = _ownerPermission == KMemoryPermission.None
                    ? KMemoryState.TransferMemoryIsolated
                    : KMemoryState.TransferMemory;

                KProcess currentProcess = KernelContext.Scheduler.GetCurrentProcess();

                Result result = currentProcess.MemoryManager.UnmapPages(address, _pageList, state);

                if (result != Result.Success)
                {
                    return result;
                }

                _isMapped = false;
            }

            return Result.Success;
        }

        protected override void Destroy()
        {
            if (_hasBeenInitialized)
            {
                ulong size = _pageList.GetPagesCount() * KMemoryManager.PageSize;

                if (!_isMapped && _creator.MemoryManager.UnborrowTransferMemory(_address, size, _pageList) != Result.Success)
                {
                    throw new InvalidOperationException("Unexpected failure restoring transfer memory attributes.");
                }

                _creator.ResourceLimit?.Release(LimitableResource.TransferMemory, 1);
                _creator.DecrementReferenceCount();
            }
        }
    }
}