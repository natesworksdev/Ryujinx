namespace Ryujinx.HLE.Loaders.Elf
{
    readonly struct ElfSymbol
    {
        public readonly string Name;

        public readonly ElfSymbolType Type;
        public readonly ElfSymbolBinding Binding;
        public readonly ElfSymbolVisibility Visibility;

        public readonly bool IsFuncOrObject =>
            Type == ElfSymbolType.SttFunc ||
            Type == ElfSymbolType.SttObject;

        public readonly bool IsGlobalOrWeak =>
            Binding == ElfSymbolBinding.StbGlobal ||
            Binding == ElfSymbolBinding.StbWeak;

        public readonly int ShIdx;
        public readonly ulong Value;
        public readonly ulong Size;

        public ElfSymbol(
            string name,
            int    info,
            int    other,
            int    shIdx,
            ulong  value,
            ulong  size)
        {
            Name       = name;
            Type       = (ElfSymbolType)(info & 0xf);
            Binding    = (ElfSymbolBinding)(info >> 4);
            Visibility = (ElfSymbolVisibility)other;
            ShIdx      = shIdx;
            Value      = value;
            Size       = size;
        }
    }
}