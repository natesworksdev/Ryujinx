namespace Ryujinx.HLE.HOS.Kernel
{
    internal class KMemoryInfo
    {
        public long Position { get; private set; }
        public long Size     { get; private set; }

        public MemoryState      State      { get; private set; }
        public MemoryPermission Permission { get; private set; }
        public MemoryAttribute  Attribute  { get; private set; }

        public int IpcRefCount    { get; private set; }
        public int DeviceRefCount { get; private set; }

        public KMemoryInfo(
            long             position,
            long             size,
            MemoryState      state,
            MemoryPermission permission,
            MemoryAttribute  attribute,
            int              ipcRefCount,
            int              deviceRefCount)
        {
            this.Position       = position;
            this.Size           = size;
            this.State          = state;
            this.Attribute      = attribute;
            this.Permission     = permission;
            this.IpcRefCount    = ipcRefCount;
            this.DeviceRefCount = deviceRefCount;
        }
    }
}