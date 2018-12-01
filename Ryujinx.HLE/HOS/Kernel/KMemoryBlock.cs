namespace Ryujinx.HLE.HOS.Kernel
{
    class KMemoryBlock
    {
        public ulong BaseAddress { get; set; }
        public ulong PagesCount  { get; set; }

        public MemoryState      State      { get; set; }
        public MemoryPermission Permission { get; set; }
        public MemoryAttribute  Attribute  { get; set; }

        public int IpcRefCount    { get; set; }
        public int DeviceRefCount { get; set; }

        public KMemoryBlock(
            ulong            baseAddress,
            ulong            pagesCount,
            MemoryState      state,
            MemoryPermission permission,
            MemoryAttribute  attribute)
        {
            this.BaseAddress = baseAddress;
            this.PagesCount  = pagesCount;
            this.State       = state;
            this.Attribute   = attribute;
            this.Permission  = permission;
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
                IpcRefCount,
                DeviceRefCount);
        }
    }
}