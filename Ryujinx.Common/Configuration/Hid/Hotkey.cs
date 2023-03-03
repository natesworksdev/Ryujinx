namespace Ryujinx.Common.Configuration.Hid
{
    public struct Hotkey
    {
        public Key Key { get; set; }
        public KeyModifier Modifier { get; set; }
        public ulong GamepadInputMask { get; set; }

        public Hotkey(Key key, KeyModifier modifier, ulong gamepadInputMask)
        {
            Key = key;
            Modifier = modifier;
            GamepadInputMask = gamepadInputMask;
        }

        public Hotkey(Key key) : this(key, KeyModifier.None, 0UL)
        {
        }

        public bool HasKeyboard()
        {
            return Key != Key.Unknown && Key != Key.Unbound;
        }

        public bool HasGamepad()
        {
            return GamepadInputMask != 0UL;
        }
    }
}