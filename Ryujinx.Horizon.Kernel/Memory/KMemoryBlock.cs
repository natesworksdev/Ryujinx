using System;

namespace Ryujinx.Horizon.Kernel.Memory
{
    class KMemoryBlock
    {
        public ulong BaseAddress { get; private set; }
        public ulong PagesCount { get; private set; }

        public KMemoryState State { get; private set; }
        public KMemoryPermission Permission { get; private set; }
        public KMemoryAttribute Attribute { get; private set; }
        public KMemoryPermission SourcePermission { get; private set; }

        public int IpcRefCount { get; private set; }
        public int DeviceRefCount { get; private set; }

        public KMemoryBlock(
            ulong baseAddress,
            ulong pagesCount,
            KMemoryState state,
            KMemoryPermission permission,
            KMemoryAttribute attribute,
            int ipcRefCount = 0,
            int deviceRefCount = 0)
        {
            BaseAddress = baseAddress;
            PagesCount = pagesCount;
            State = state;
            Attribute = attribute;
            Permission = permission;
            IpcRefCount = ipcRefCount;
            DeviceRefCount = deviceRefCount;
        }

        public void SetState(KMemoryPermission permission, KMemoryState state, KMemoryAttribute attribute)
        {
            Permission = permission;
            State = state;
            Attribute &= KMemoryAttribute.IpcAndDeviceMapped;
            Attribute |= attribute;
        }

        public void SetIpcMappingPermission(KMemoryPermission newPermission)
        {
            int oldIpcRefCount = IpcRefCount++;

            if ((ushort)IpcRefCount == 0)
            {
                throw new InvalidOperationException("IPC reference count increment overflowed.");
            }

            if (oldIpcRefCount == 0)
            {
                SourcePermission = Permission;

                Permission &= ~KMemoryPermission.ReadAndWrite;
                Permission |= KMemoryPermission.ReadAndWrite & newPermission;
            }

            Attribute |= KMemoryAttribute.IpcMapped;
        }

        public void RestoreIpcMappingPermission()
        {
            int oldIpcRefCount = IpcRefCount--;

            if (oldIpcRefCount == 0)
            {
                throw new InvalidOperationException("IPC reference count decrement underflowed.");
            }

            if (oldIpcRefCount == 1)
            {
                Permission = SourcePermission;

                SourcePermission = KMemoryPermission.None;

                Attribute &= ~KMemoryAttribute.IpcMapped;
            }
        }

        public KMemoryBlock SplitRightAtAddress(ulong address)
        {
            ulong leftAddress = BaseAddress;

            ulong leftPagesCount = (address - leftAddress) / KMemoryManager.PageSize;

            BaseAddress = address;

            PagesCount -= leftPagesCount;

            return new KMemoryBlock(
                leftAddress,
                leftPagesCount,
                State,
                Permission,
                Attribute,
                IpcRefCount,
                DeviceRefCount);
        }

        public void AddPages(ulong pagesCount)
        {
            PagesCount += pagesCount;
        }

        public KMemoryInfo GetInfo()
        {
            ulong size = PagesCount * KMemoryManager.PageSize;

            return new KMemoryInfo(
                BaseAddress,
                size,
                State,
                Permission,
                Attribute,
                SourcePermission,
                IpcRefCount,
                DeviceRefCount);
        }
    }
}