using Ryujinx.Common.Memory;

namespace Ryujinx.HLE.HOS.Applets.Browser
{
    public struct WebCommonReturnValue
    {
        public WebExitReason   ExitReason;
        public uint            Padding;
        public Array4096<byte> LastUrl;
        public ulong           LastUrlSize;
    }
}