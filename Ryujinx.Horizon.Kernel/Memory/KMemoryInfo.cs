namespace Ryujinx.Horizon.Kernel.Memory
{
    class KMemoryInfo
    {
        public ulong Address { get; }
        public ulong Size { get; }

        public KMemoryState State { get; }
        public KMemoryPermission Permission { get; }
        public KMemoryAttribute Attribute { get; }
        public KMemoryPermission SourcePermission { get; }

        public int IpcRefCount { get; }
        public int DeviceRefCount { get; }

        public KMemoryInfo(
            ulong address,
            ulong size,
            KMemoryState state,
            KMemoryPermission permission,
            KMemoryAttribute attribute,
            KMemoryPermission sourcePermission,
            int ipcRefCount,
            int deviceRefCount)
        {
            Address = address;
            Size = size;
            State = state;
            Permission = permission;
            Attribute = attribute;
            SourcePermission = sourcePermission;
            IpcRefCount = ipcRefCount;
            DeviceRefCount = deviceRefCount;
        }
    }
}