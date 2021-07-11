using Ryujinx.Common;
using Ryujinx.HLE.HOS.Kernel.Common;
using Ryujinx.HLE.HOS.Kernel.Memory;
using Ryujinx.Memory;
using Ryujinx.Memory.Range;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

namespace Ryujinx.HLE.HOS.Kernel.Process
{
    enum CodeMemoryOperation : uint
    {
        MapOwner,
        MapSlave,
        UnmapOwner,
        UnmapSlave
    };

    class KCodeMemory : KAutoObject
    {
        private IEnumerable<HostMemoryRange> _hostPagelist;
        private ulong _pageCount;
        private KProcess _owner;
        private object _lock;
        private bool _isOwnerMapped;
        private bool _isMapped;

        public KCodeMemory(KernelContext context) : base(context)
        {
            _lock = new object();
        }

        public KernelResult Initialize(ulong address, ulong size)
        {
            _owner = KernelStatic.GetCurrentProcess();

            _hostPagelist = _owner.MemoryManager.GetPhysicalRegions(address, size);
            _pageCount = BitUtils.DivRoundUp(size, KPageTableBase.PageSize);
            
            _isOwnerMapped = false;
            _isMapped = false;

            return KernelResult.Success;
        }

        public KernelResult Map(ulong address, ulong size, KMemoryPermission perm)
        {
            if (_pageCount != BitUtils.DivRoundUp(size, KPageTableBase.PageSize))
            {
                return KernelResult.InvalidSize;
            }

            lock (_lock)
            {
                if (_isMapped)
                {
                    return KernelResult.InvalidState;
                }

                KProcess proc = KernelStatic.GetCurrentProcess();

                // TODO: Mark pages as MemoryState.CodeWritable
                KernelResult resultCode = proc.MemoryManager.MapPages(address, _hostPagelist, KMemoryPermission.ReadAndWrite);
                if (resultCode != KernelResult.Success)
                {
                    return KernelResult.InvalidState;
                }

                _isMapped = true;
            }

            return KernelResult.Success;
        }

        public KernelResult MapToOwner(ulong address, ulong size, KMemoryPermission permission)
        {
            if (_pageCount != BitUtils.DivRoundUp(size, KPageTableBase.PageSize))
            {
                return KernelResult.InvalidSize;
            }

            lock (_lock)
            {
                if (_isOwnerMapped)
                {
                    return KernelResult.InvalidState;
                }

                Debug.Assert(permission == KMemoryPermission.Read || permission == KMemoryPermission.ReadAndExecute);

                // TODO: Mark pages as MemoryState.CodeReadOnly
                _owner.MemoryManager.MapPages(address, _hostPagelist, permission == KMemoryPermission.Read ? KMemoryPermission.Read :  KMemoryPermission.ReadAndExecute);

                _isOwnerMapped = true;
            }

            return KernelResult.Success;
        }

        public KernelResult Unmap(ulong address, ulong size)
        {
            if (_pageCount != BitUtils.DivRoundUp(size, KPageTableBase.PageSize))
            {
                return KernelResult.InvalidSize;
            }

            lock (_lock)
            {
                KProcess proc = KernelStatic.GetCurrentProcess();

                proc.MemoryManager.UnmapPages(address, _pageCount, _hostPagelist, MemoryState.CodeWritable);
            }

            return KernelResult.Success;
        }

        public KernelResult UnmapToOwner(ulong address, ulong size)
        {
            if (_pageCount != BitUtils.DivRoundUp(size, KPageTableBase.PageSize))
            {
                return KernelResult.InvalidSize;
            }

            lock (_lock)
            {
                _owner.MemoryManager.UnmapPages(address, _pageCount, _hostPagelist, MemoryState.CodeReadOnly);

                Debug.Assert(_isOwnerMapped);

                _isOwnerMapped = false;
            }

            return KernelResult.Success;
        }
    }
}
