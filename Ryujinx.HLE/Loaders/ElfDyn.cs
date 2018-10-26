namespace Ryujinx.HLE.Loaders
{
    internal struct ElfDyn
    {
        public ElfDynTag Tag { get; private set; }

        public long Value { get; private set; }

        public ElfDyn(ElfDynTag tag, long value)
        {
            Tag   = tag;
            Value = value;
        }
    }
}