using System.Diagnostics;

namespace Ryujinx.Input
{
    public enum ButtonValueType { Key, GamepadButtonInputId, StickId }

    public readonly struct ButtonValue
    {
        public readonly ButtonValueType Type;
        private readonly uint _rawValue;

        public ButtonValue(Key key)
        {
            Type = ButtonValueType.Key;
            _rawValue = (uint)key;
        }

        public ButtonValue(GamepadButtonInputId gamepad)
        {
            Type = ButtonValueType.GamepadButtonInputId;
            _rawValue = (uint)gamepad;
        }

        public ButtonValue(StickInputId stick)
        {
            Type = ButtonValueType.StickId;
            _rawValue = (uint)stick;
        }

        public Common.Configuration.Hid.Key AsKey()
        {
            Debug.Assert(Type == ButtonValueType.Key);
            return (Common.Configuration.Hid.Key)_rawValue;
        }

        public Common.Configuration.Hid.Controller.GamepadInputId AsGamepadButtonInputId()
        {
            Debug.Assert(Type == ButtonValueType.GamepadButtonInputId);
            return (Common.Configuration.Hid.Controller.GamepadInputId)_rawValue;
        }

        public Common.Configuration.Hid.Controller.StickInputId AsGamepadStickId()
        {
            Debug.Assert(Type == ButtonValueType.StickId);
            return (Common.Configuration.Hid.Controller.StickInputId)_rawValue;
        }
    }
}
