namespace Ryujinx.Input.Assigner
{
    public struct PressedButton
    {
        public PressedButtonType Type { get; }

        private readonly byte _value;

        internal PressedButton(PressedButtonType type)
        {
            Type = type;
        }

        internal PressedButton(Key key) : this(PressedButtonType.Key)
        {
            _value = (byte)key;
        }

        internal PressedButton(GamepadButtonInputId button) : this(PressedButtonType.Button)
        {
            _value = (byte)button;
        }

        internal PressedButton(StickInputId stick) : this(PressedButtonType.Stick)
        {
            _value = (byte)stick;
        }

        public Key AsKey()
        {
            return (Key)_value;
        }

        public GamepadButtonInputId AsGamepadButtonInputId()
        {
            return (GamepadButtonInputId)_value;
        }

        public StickInputId AsStickInputId()
        {
            return (StickInputId)_value;
        }

        public override string ToString()
        {
            return Type switch
            {
                PressedButtonType.Key => AsKey().ToString(),
                PressedButtonType.Button => AsGamepadButtonInputId().ToString(),
                PressedButtonType.Stick => AsStickInputId().ToString(),
                _ => string.Empty
            };
        }
    }
}