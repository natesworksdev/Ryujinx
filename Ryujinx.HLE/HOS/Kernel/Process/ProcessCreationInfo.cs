namespace Ryujinx.HLE.HOS.Kernel.Process
{
    readonly struct ProcessCreationInfo
    {
        public readonly string Name;

        public readonly int Category;
        public readonly ulong TitleId;

        public readonly ulong CodeAddress;
        public readonly int CodePagesCount;

        public readonly int MmuFlags;
        public readonly int ResourceLimitHandle;
        public readonly int PersonalMmHeapPagesCount;

        public ProcessCreationInfo(
            string name,
            int    category,
            ulong  titleId,
            ulong  codeAddress,
            int    codePagesCount,
            int    mmuFlags,
            int    resourceLimitHandle,
            int    personalMmHeapPagesCount)
        {
            Name                     = name;
            Category                 = category;
            TitleId                  = titleId;
            CodeAddress              = codeAddress;
            CodePagesCount           = codePagesCount;
            MmuFlags                 = mmuFlags;
            ResourceLimitHandle      = resourceLimitHandle;
            PersonalMmHeapPagesCount = personalMmHeapPagesCount;
        }
    }
}