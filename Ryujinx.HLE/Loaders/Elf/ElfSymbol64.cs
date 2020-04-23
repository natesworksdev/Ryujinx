namespace Ryujinx.HLE.Loaders.Elf
{
    readonly struct ElfSymbol64
    {
#pragma warning disable CS0649
        public readonly uint   NameOffset;
        public readonly char   Info;
        public readonly char   Other;
        public readonly ushort SectionIndex;
        public readonly ulong  ValueAddress;
        public readonly ulong  Size;
#pragma warning restore CS0649
    }
}
