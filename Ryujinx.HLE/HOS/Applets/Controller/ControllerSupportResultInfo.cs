namespace Ryujinx.HLE.HOS.Applets
{
    unsafe struct ControllerSupportResultInfo
    {
        public sbyte PlayerCount;
        fixed byte padding[3];
        public uint SelectedId;
        public uint Result;
    }
}