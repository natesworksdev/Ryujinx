namespace Ryujinx.HLE.HOS.Kernel
{
    class KMemoryBlock
    {
        public long BaseAddress { get; set; }
        public long PagesCount  { get; set; }

        public MemoryState      State      { get; set; }
        public MemoryPermission Permission { get; set; }
        public MemoryAttribute  Attribute  { get; set; }

        public int IpcRefCount    { get; set; }
        public int DeviceRefCount { get; set; }

        public KMemoryBlock(
            long             BaseAddress,
            long             PagesCount,
            MemoryState      State,
            MemoryPermission Permission,
            MemoryAttribute  Attribute)
        {
            this.BaseAddress = BaseAddress;
            this.PagesCount  = PagesCount;
            this.State       = State;
            this.Attribute   = Attribute;
            this.Permission  = Permission;
        }

        public KMemoryInfo GetInfo()
        {
            long Size = PagesCount * KMemoryManager.PageSize;

            return new KMemoryInfo(
                BaseAddress,
                Size,
                State,
                Permission,
                Attribute,
                IpcRefCount,
                DeviceRefCount);
        }
    }
}