using System.Runtime.InteropServices;

namespace ARMeilleure.Translation.PTC
{
    [StructLayout(LayoutKind.Sequential, Pack = 1/*, Size = 13*/)]
    struct RelocEntry
    {
        public int Position;
        public Symbol Symbol;

        public RelocEntry(int position, Symbol symbol)
        {
            Position = position;
            Symbol = symbol;
        }

        public override string ToString()
        {
            return $"({nameof(Position)} = {Position}, {nameof(Symbol)} = {Symbol})";
        }
    }
}