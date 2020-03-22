namespace Ryujinx.HLE.HOS.Applets
{
    /*
     * Reference:
     * https://github.com/switchbrew/libnx/blob/master/nx/source/applets/hid_la.c
     * https://github.com/switchbrew/libnx/blob/master/nx/include/switch/applets/hid_la.h
     * 
    */

    enum HidLaControllerSupportMode : byte
    {
        ShowControllerSupport = 0,
        ShowControllerStrapGuide = 1,
        ShowControllerFirmwareUpdate = 2
    }

    struct HidLaControllerSupportArgPrivate
    {
        public uint PrivateSize;
        public uint ArgSize;
        public byte Flag0;
        public byte Flag1;
        public HidLaControllerSupportMode Mode;
        public byte ControllerSupportCaller;
        public uint NpadStyleSet;
        public uint NpadJoyHoldType;
    }

    struct HidLaControllerSupportArgHeader {
        public sbyte PlayerCountMin;
        public sbyte PlayerCountMax;
        public byte EnableTakeOverConnection;
        public byte EnableLeftJustify;
        public byte EnablePermitJoyDual;
        public byte EnableSingleMode;
        public byte EnableIdentificationColor;
    }

    // (8.0.0+ version)
    unsafe struct HidLaControllerSupportArg
    {
        public HidLaControllerSupportArgHeader Header;
        public fixed uint IdentificationColor[8];
        public byte EnableExplainText;
        public fixed byte ExplainText[8 * 0x81];
    }

    // (<8.0.0 version)
    unsafe struct HidLaControllerSupportArgV3
    {
        public HidLaControllerSupportArgHeader Header;
        public fixed uint IdentificationColor[4];
        public byte EnableExplainText;
        public fixed byte ExplainText[4 * 0x81];
    }

    // squashed into single struct
    unsafe struct HidLaControllerSupportResultInfo
    {
        public sbyte PlayerCount;
        public fixed byte _Pad[3];
        public uint SelectedId;
        public uint Result;
    }
}