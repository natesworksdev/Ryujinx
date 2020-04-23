namespace Ryujinx.HLE.HOS.Applets
{
    // (8.0.0+ version)
    unsafe struct ControllerSupportArg
    {
#pragma warning disable CS0649
        public ControllerSupportArgHeader Header;
        public fixed uint IdentificationColor[8];
        public byte EnableExplainText;
        public fixed byte ExplainText[8 * 0x81];
#pragma warning restore CS0649
    }
}