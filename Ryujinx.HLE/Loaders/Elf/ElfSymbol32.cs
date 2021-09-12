namespace Ryujinx.HLE.Loaders.Elf
{
    public struct ElfSymbol32
    {
        public uint   NameOffset;
        public uint   ValueAddress;
        public uint   Size;
        public byte   Info;
        public byte   Other;
        public ushort SectionIndex;
    }
}