namespace Ryujinx.HLE.Loaders
{
    struct ElfDyn
    {
        public ElfDynTag Tag { get; private set; }

        public long Value { get; private set; }

        public ElfDyn(ElfDynTag tag, long value)
        {
            this.Tag   = tag;
            this.Value = value;
        }
    }
}