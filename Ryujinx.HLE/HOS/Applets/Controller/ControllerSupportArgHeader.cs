using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Applets
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct ControllerSupportArgHeader
    {
        public sbyte PlayerCountMin;
        public sbyte PlayerCountMax;
        public byte  EnableTakeOverConnection;
        public byte  EnableLeftJustify;
        public byte  EnablePermitJoyDual;
        public byte  EnableSingleMode;
        public byte  EnableIdentificationColor;
    }
}