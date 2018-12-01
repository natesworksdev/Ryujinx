namespace Ryujinx.HLE.HOS.Kernel
{
    struct ProcessCreationInfo
    {
        public string Name { get; private set; }

        public int  Category { get; private set; }
        public long TitleId  { get; private set; }

        public ulong CodeAddress    { get; private set; }
        public int   CodePagesCount { get; private set; }

        public int MmuFlags                 { get; private set; }
        public int ResourceLimitHandle      { get; private set; }
        public int PersonalMmHeapPagesCount { get; private set; }

        public ProcessCreationInfo(
            string name,
            int    category,
            long   titleId,
            ulong  codeAddress,
            int    codePagesCount,
            int    mmuFlags,
            int    resourceLimitHandle,
            int    personalMmHeapPagesCount)
        {
            this.Name                     = name;
            this.Category                 = category;
            this.TitleId                  = titleId;
            this.CodeAddress              = codeAddress;
            this.CodePagesCount           = codePagesCount;
            this.MmuFlags                 = mmuFlags;
            this.ResourceLimitHandle      = resourceLimitHandle;
            this.PersonalMmHeapPagesCount = personalMmHeapPagesCount;
        }
    }
}