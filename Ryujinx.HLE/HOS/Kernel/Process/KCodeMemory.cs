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
        private ulong _srcAddress;
        private object _lock;
        private bool _initialized;
        private bool _isOwnerMapped;
        private bool _isMapped;

        public KCodeMemory(KernelContext context) : base(context)
        {
            _lock = new object();
        }

        public KernelResult Initialize(ulong address, ulong size)
        {
            _owner = KernelStatic.GetCurrentProcess();

            _host_pagelist = _owner.MemoryManager.GetPhysicalRegions(address, size);
            _pagecount = BitUtils.DivRoundUp(size, KPageTableBase.PageSize);

            _src_addr = address;
            _initialized = true;
            _is_owner_mapped = false;
            _is_mapped = false;

            return KernelResult.Success;
        }

        public KernelResult Map(ulong address, ulong size, KMemoryPermission perm)
        {
            if (_pagecount != BitUtils.DivRoundUp(size, KPageTableBase.PageSize))
                return KernelResult.InvalidSize;

            lock (_lock)
            {
                if (_is_mapped)
                    return KernelResult.InvalidState;

                KProcess proc = KernelStatic.GetCurrentProcess();
                KernelResult r = proc.MemoryManager.MapPages(address, _host_pagelist, KMemoryPermission.ReadAndWrite);
                // MemoryState.CodeWritable
                if (r != KernelResult.Success)
                {
                    return KernelResult.InvalidState;
                }

                _is_mapped = true;

            }
            return KernelResult.Success;
        }

        public KernelResult MapToOwner(ulong address, ulong size, KMemoryPermission permission)
        {
            if (_pagecount != BitUtils.DivRoundUp(size, KPageTableBase.PageSize))
                return KernelResult.InvalidSize;

            lock (_lock)
            {
                if (_is_owner_mapped)
                    return KernelResult.InvalidState;

                Debug.Assert(perm == KMemoryPermission.Read || perm == KMemoryPermission.ReadAndExecute);

                _owner.MemoryManager.MapPages(address, _host_pagelist,
                    perm == KMemoryPermission.Read ?
                        KMemoryPermission.Read :
                        KMemoryPermission.ReadAndExecute
                );
                // MemoryState.CodeReadOnly

                _is_owner_mapped = true;

            }

            return KernelResult.Success;
        }

        public KernelResult Unmap(ulong address, ulong size)
        {
            if (_pagecount != BitUtils.DivRoundUp(size, KPageTableBase.PageSize))
                return KernelResult.InvalidSize;

            lock (_lock)
            {
                KProcess proc = KernelStatic.GetCurrentProcess();

                proc.MemoryManager.UnmapPages(address, _pagecount, _host_pagelist, MemoryState.CodeWritable);
            }

            return KernelResult.Success;
        }
        public KernelResult UnmapToOwner(ulong address, ulong size)
        {
            if (_pagecount != BitUtils.DivRoundUp(size, KPageTableBase.PageSize))
                return KernelResult.InvalidSize;

            lock (_lock)
            {
                _owner.MemoryManager.UnmapPages(address, _pagecount, _host_pagelist, MemoryState.CodeReadOnly);

                Debug.Assert(_is_owner_mapped);
                _is_owner_mapped = false;
            }

            return KernelResult.Success;
        }

    }
}
