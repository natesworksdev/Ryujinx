namespace Ryujinx.HLE.Loaders
{
    struct ElfRel
    {
        public long Offset { get; private set; }
        public long Addend { get; private set; }

        public ElfSym     Symbol { get; private set; }
        public ElfRelType Type   { get; private set; }

        public ElfRel(long offset, long addend, ElfSym symbol, ElfRelType type)
        {
            this.Offset = offset;
            this.Addend = addend;
            this.Symbol = symbol;
            this.Type   = type;
        }
    }
}