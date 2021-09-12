namespace Ryujinx.HLE.Loaders.Elf
{
    public struct ElfSymbol64
    {
        public uint   NameOffset;
        public byte   Info;
        public byte   Other;
        public ushort SectionIndex;
        public ulong  ValueAddress;
        public ulong  Size;
    }
}