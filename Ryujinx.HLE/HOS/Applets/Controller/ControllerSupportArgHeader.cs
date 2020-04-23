namespace Ryujinx.HLE.HOS.Applets
{
    struct ControllerSupportArgHeader
    {
#pragma warning disable CS0649
        public sbyte PlayerCountMin;
        public sbyte PlayerCountMax;
        public byte EnableTakeOverConnection;
        public byte EnableLeftJustify;
        public byte EnablePermitJoyDual;
        public byte EnableSingleMode;
        public byte EnableIdentificationColor;
#pragma warning restore CS0649
    }
}