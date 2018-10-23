using System;

namespace Ryujinx.HLE.Input
{
    [Flags]
    public enum HidControllerButtons
    {
        KeyA            = (1 << 0),
        KeyB            = (1 << 1),
        KeyX            = (1 << 2),
        KeyY            = (1 << 3),
        KeyLstick       = (1 << 4),
        KeyRstick       = (1 << 5),
        KeyL            = (1 << 6),
        KeyR            = (1 << 7),
        KeyZl           = (1 << 8),
        KeyZr           = (1 << 9),
        KeyPlus         = (1 << 10),
        KeyMinus        = (1 << 11),
        KeyDleft        = (1 << 12),
        KeyDup          = (1 << 13),
        KeyDright       = (1 << 14),
        KeyDdown        = (1 << 15),
        KeyLstickLeft  = (1 << 16),
        KeyLstickUp    = (1 << 17),
        KeyLstickRight = (1 << 18),
        KeyLstickDown  = (1 << 19),
        KeyRstickLeft  = (1 << 20),
        KeyRstickUp    = (1 << 21),
        KeyRstickRight = (1 << 22),
        KeyRstickDown  = (1 << 23),
        KeySl           = (1 << 24),
        KeySr           = (1 << 25)
    }
}