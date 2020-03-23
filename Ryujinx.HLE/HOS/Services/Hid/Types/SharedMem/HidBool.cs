namespace Ryujinx.HLE.HOS.Services.Hid
{
    public struct HidBool
    {
        private uint _value;
        public static implicit operator bool(HidBool value) => (value._value & 1) != 0;
        public static implicit operator HidBool(bool value) => new HidBool() { _value = value ? 1u : 0u };
    }
}