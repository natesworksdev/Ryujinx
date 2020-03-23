namespace Ryujinx.HLE.HOS.Applets
{
    // squashed into single struct
    unsafe struct ControllerSupportResultInfo
    {
        public sbyte PlayerCount;
        public fixed byte _Pad[3];
        public uint SelectedId;
        public uint Result;
    }
}