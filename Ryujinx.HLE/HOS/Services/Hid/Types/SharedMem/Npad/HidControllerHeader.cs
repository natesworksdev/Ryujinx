namespace Ryujinx.HLE.HOS.Services.Hid
{
    public struct HidControllerHeader
    {
        public ControllerType Type;
        public HidBool IsHalf;
        public HidControllerColorDescription SingleColorsDescriptor;
        public NpadColor SingleColorBody;
        public NpadColor SingleColorButtons;
        public HidControllerColorDescription SplitColorsDescriptor;
        public NpadColor LeftColorBody;
        public NpadColor LeftColorButtons;
        public NpadColor RightColorBody;
        public NpadColor RightColorButtons;
    }
}