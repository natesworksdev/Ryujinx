namespace Ryujinx.HLE.Loaders.Elf
{
    readonly struct ElfDynamic
    {
        public readonly ElfDynamicTag Tag;

        public readonly long Value;

        public ElfDynamic(ElfDynamicTag tag, long value)
        {
            Tag   = tag;
            Value = value;
        }
    }
}