using System;

namespace Ryujinx.Common.Combo
{
    public enum ComboType : uint
    {
        SpecialOne,
        SpecialTwo
    }

    public class ComboPressedEventArgs : EventArgs
    {
        public ComboType Combo;

        public ComboPressedEventArgs(ComboType combo)
        {
            Combo = combo;
        }
    }
}
