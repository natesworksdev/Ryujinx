using Ryujinx.Common;
using Ryujinx.HLE.HOS.Kernel.Common;
using Ryujinx.HLE.HOS.Kernel.Process;
using System.Collections.Generic;

namespace Ryujinx.HLE.HOS.Kernel.Memory
{
    class KSharedMemory : KAutoObject
    {
        private KPageList _pageList;

        private KResourceLimit _ownerResourceLimit;

        private long _ownerPid;

        private KMemoryPermission _ownerPermission;
        private KMemoryPermission _userPermission;

        private bool _isInitialized;

        public KSharedMemory(KernelContext context) : base(context)
        {
        }

        public KernelResult Initialize(KProcess owner, ulong size, KMemoryPermission ownerPermission, KMemoryPermission userPermission)
        {
            _ownerPid = owner.Pid;

            _ownerPermission = ownerPermission;
            _userPermission = userPermission;

            ulong pagesCount = size / KMemoryManager.PageSize;

            KResourceLimit resourceLimit = owner.ResourceLimit;

            if (!resourceLimit.Reserve(LimitableResource.Memory, size))
            {
                return KernelResult.ResLimitExceeded;
            }

            KernelResult result = KernelContext.MemoryRegions[(int)owner.MemoryRegion].AllocatePages(pagesCount, !owner.AslrEnabled, out _pageList);

            if (result != KernelResult.Success)
            {
                resourceLimit.Release(LimitableResource.Memory, size);

                return result;
            }

            _ownerResourceLimit = resourceLimit;

            _isInitialized = true;

            for (LinkedListNode<KPageNode> node = _pageList.Nodes.First; node != null; node = node.Next)
            {
                KPageNode pageNode = node.Value;

                KernelContext.Memory.ZeroFill(KMemoryManager.GetDramAddressFromPa(pageNode.Address), pageNode.PagesCount * KMemoryManager.PageSize);
            }

            return KernelResult.Success;
        }

        public KernelResult MapIntoProcess(
            KMemoryManager   memoryManager,
            ulong            address,
            ulong            size,
            KProcess         process,
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

            return memoryManager.MapPages(address, _pageList, MemoryState.SharedMemory, permission);
        }

        public KernelResult UnmapFromProcess(KMemoryManager memoryManager, ulong address, ulong size, KProcess process)
        {
            ulong pagesCountRounded = BitUtils.DivRoundUp(size, KMemoryManager.PageSize);

            if (_pageList.GetPagesCount() != pagesCountRounded)
            {
                return KernelResult.InvalidSize;
            }

            return memoryManager.UnmapPages(address, _pageList, MemoryState.SharedMemory);
        }

        protected override void Destroy()
        {
            if (_isInitialized)
            {
                ulong size = _pageList.GetPagesCount() * KMemoryManager.PageSize;

                _ownerResourceLimit.Release(LimitableResource.Memory, size);
            }
        }
    }
}