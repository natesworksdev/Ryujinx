using Ryujinx.HLE.HOS.Kernel.Memory;

namespace Ryujinx.HLE.HOS.Kernel.SupervisorCall
{
    struct MemoryInfo
    {
        public ulong Address { get; }
        public ulong Size { get; }
        public MemoryState State { get; }
        public MemoryAttribute Attribute { get; }
        public KMemoryPermission Permission { get; }
        public int IpcRefCount { get; }
        public int DeviceRefCount { get; }
#pragma warning disable CS0414
        private int _padding;
#pragma warning restore CS0414

        public MemoryInfo(
            ulong address,
            ulong size,
            MemoryState state,
            MemoryAttribute attribute,
            KMemoryPermission permission,
            int ipcRefCount,
            int deviceRefCount)
        {
            Address = address;
            Size = size;
            State = state;
            Attribute = attribute;
            Permission = permission;
            IpcRefCount = ipcRefCount;
            DeviceRefCount = deviceRefCount;
            _padding = 0;
        }
    }
}