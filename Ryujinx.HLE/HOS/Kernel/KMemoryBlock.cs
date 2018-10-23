namespace Ryujinx.HLE.HOS.Kernel
{
    class KMemoryBlock
    {
        public long BasePosition { get; set; }
        public long PagesCount   { get; set; }

        public MemoryState      State      { get; set; }
        public MemoryPermission Permission { get; set; }
        public MemoryAttribute  Attribute  { get; set; }

        public int IpcRefCount    { get; set; }
        public int DeviceRefCount { get; set; }

        public KMemoryBlock(
            long             basePosition,
            long             pagesCount,
            MemoryState      state,
            MemoryPermission permission,
            MemoryAttribute  attribute)
        {
            this.BasePosition = basePosition;
            this.PagesCount   = pagesCount;
            this.State        = state;
            this.Attribute    = attribute;
            this.Permission   = permission;
        }

        public KMemoryInfo GetInfo()
        {
            long size = PagesCount * KMemoryManager.PageSize;

            return new KMemoryInfo(
                BasePosition,
                size,
                State,
                Permission,
                Attribute,
                IpcRefCount,
                DeviceRefCount);
        }
    }
}