namespace Ryujinx.Horizon.Kernel.Diagnostics.Elf
{
    struct ElfDynamic
    {
        public ElfDynamicTag Tag { get; private set; }

        public long Value { get; private set; }

        public ElfDynamic(ElfDynamicTag tag, long value)
        {
            Tag = tag;
            Value = value;
        }
    }
}