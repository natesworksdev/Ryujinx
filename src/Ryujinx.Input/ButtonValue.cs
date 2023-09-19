using System.Diagnostics;

namespace Ryujinx.Input
{
    public enum ButtonValueType { Key, GamepadButtonInputId, StickId }

    public struct ButtonValue
    {
        public ButtonValueType Type;
        public uint RawValue;

        public ButtonValue(Key key)
        {
            Type = ButtonValueType.Key;
            RawValue = (uint)key;
        }

        public ButtonValue(GamepadButtonInputId gamepad)
        {
            Type = ButtonValueType.GamepadButtonInputId;
            RawValue = (uint)gamepad;
        }

        public ButtonValue(StickInputId stick)
        {
            Type = ButtonValueType.StickId;
            RawValue = (uint)stick;
        }

        public Common.Configuration.Hid.Key AsKey()
        {
            Debug.Assert(Type == ButtonValueType.Key);
            return (Common.Configuration.Hid.Key)RawValue;
        }

        public Common.Configuration.Hid.Controller.GamepadInputId AsGamepadButtonInputId()
        {
            Debug.Assert(Type == ButtonValueType.GamepadButtonInputId);
            return (Common.Configuration.Hid.Controller.GamepadInputId)RawValue;
        }

        public Common.Configuration.Hid.Controller.StickInputId AsGamepadStickId()
        {
            Debug.Assert(Type == ButtonValueType.StickId);
            return (Common.Configuration.Hid.Controller.StickInputId)RawValue;
        }
    }
}
