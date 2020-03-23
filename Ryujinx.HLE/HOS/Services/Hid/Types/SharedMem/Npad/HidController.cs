namespace Ryujinx.HLE.HOS.Services.Hid
{
    public unsafe struct HidController
    {
        public HidControllerHeader Header;
        public Array7<HidControllerLayout> Layouts;         // One for each ControllerLayoutType?
        public Array6<HidControllerSixAxisLayout> Sixaxis;  // Unknown layout mapping
        public HidControllerMisc Misc;
        public fixed byte _Unk2[0xDF8];
    }
}