using System;

namespace Ryujinx.Common.Combo
{
    public enum ComboType : uint
    {
        Start,
        Capture,
        SpecialOne,
        SpecialTwo
    }

    public class ComboPressedEventArgs : EventArgs
    {
        private ComboType Combo;
        private bool consumed;

        public ComboPressedEventArgs(ComboType combo)
        {
            Combo = combo;
            consumed = false;
        }

        public ComboType GetCombo() {
            return Combo;
        }

        public bool GetConsumed() {
            return consumed;
        }

        public void SetConsumed(bool consumed) {
            this.consumed = consumed;
        }
    }
}
