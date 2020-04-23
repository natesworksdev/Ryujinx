namespace Ryujinx.HLE.Loaders.Elf
{
    readonly struct ElfSymbol32
    {
#pragma warning disable CS0649
        public readonly uint   NameOffset;
        public readonly uint   ValueAddress;
        public readonly uint   Size;
        public readonly char   Info;
        public readonly char   Other;
        public readonly ushort SectionIndex;
#pragma warning restore CS0649
    }
}
