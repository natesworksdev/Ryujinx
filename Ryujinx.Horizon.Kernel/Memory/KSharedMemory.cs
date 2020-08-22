using Ryujinx.Common;
using Ryujinx.Horizon.Kernel.Common;
using Ryujinx.Horizon.Kernel.Process;

namespace Ryujinx.Horizon.Kernel.Memory
{
    class KSharedMemory : KAutoObject
    {
        private KPageList _pageList;

        private KMemoryRegion _ownerRegion;
        private KResourceLimit _ownerResourceLimit;

        private long _ownerPid;

        private KMemoryPermission _ownerPermission;
        private KMemoryPermission _userPermission;

        private bool _isInitialized;

        public KSharedMemory(KernelContextInternal context) : base(context)
        {
        }

        public KernelResult Initialize(KProcess owner, ulong size, KMemoryPermission ownerPermission, KMemoryPermission userPermission)
        {
            _ownerPid = owner.Pid;
            _ownerRegion = owner.MemoryRegion;

            _ownerPermission = ownerPermission;
            _userPermission = userPermission;

            ulong pagesCount = size / KMemoryManager.PageSize;

            KResourceLimit resourceLimit = owner.ResourceLimit;

            if (!resourceLimit.Reserve(LimitableResource.Memory, size))
            {
                return KernelResult.ResLimitExceeded;
            }

            ulong address = KernelContext.MemoryRegions[(int)owner.MemoryRegion].AllocatePagesContiguous(pagesCount, !owner.AslrEnabled);

            if (address == 0)
            {
                resourceLimit.Release(LimitableResource.Memory, size);

                return KernelResult.OutOfMemory;
            }

            _ownerResourceLimit = resourceLimit;

            _isInitialized = true;

            KernelContext.Memory.ZeroFill(KMemoryManager.GetDramAddressFromPa(address), size);

            _pageList = new KPageList();
            _pageList.AddRange(address, pagesCount);

            // TODO: This should be using non-contiguous allocation,
            // but looks like it is not working properly right now. To be investigated later.
            /* for (LinkedListNode<KPageNode> node = _pageList.Nodes.First; node != null; node = node.Next)
            {
                KPageNode pageNode = node.Value;

                KernelContext.Memory.ZeroFill(KMemoryManager.GetDramAddressFromPa(pageNode.Address), pageNode.PagesCount * KMemoryManager.PageSize);
            } */

            return KernelResult.Success;
        }

        public KernelResult MapIntoProcess(
            KMemoryManager memoryManager,
            ulong address,
            ulong size,
            KProcess process,
            KMemoryPermission permission)
        {
            ulong pagesCountRounded = BitUtils.DivRoundUp(size, KMemoryManager.PageSize);

            if (_pageList.GetPagesCount() != pagesCountRounded)
            {
                return KernelResult.InvalidSize;
            }

            KMemoryPermission expectedPermission = process.Pid == _ownerPid ? _ownerPermission : _userPermission;

            if (expectedPermission != KMemoryPermission.DontCare && permission != expectedPermission)
            {
                return KernelResult.InvalidPermission;
            }

            return memoryManager.MapPages(address, _pageList, KMemoryState.SharedMemory, permission);
        }

        public KernelResult UnmapFromProcess(KMemoryManager memoryManager, ulong address, ulong size, KProcess process)
        {
            ulong pagesCountRounded = BitUtils.DivRoundUp(size, KMemoryManager.PageSize);

            if (_pageList.GetPagesCount() != pagesCountRounded)
            {
                return KernelResult.InvalidSize;
            }

            return memoryManager.UnmapPages(address, _pageList, KMemoryState.SharedMemory);
        }

        protected override void Destroy()
        {
            if (_isInitialized)
            {
                ulong size = _pageList.GetPagesCount() * KMemoryManager.PageSize;

                _ownerResourceLimit.Release(LimitableResource.Memory, size);

                KernelContext.MemoryRegions[(int)_ownerRegion].FreePages(_pageList);
            }
        }
    }
}