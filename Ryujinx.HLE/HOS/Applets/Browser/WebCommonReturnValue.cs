using System;
using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Applets.Browser
{
    public struct WebCommonReturnValue
    {
        public WebExitReason ExitReason;
        public uint          Padding;
        public LastUrlStruct LastUrl;
        public ulong         LastUrlSize;

        [StructLayout(LayoutKind.Sequential, Size = 0x1000)]
        public struct LastUrlStruct
        {
            private byte element;

            public Span<byte> ToSpan() => MemoryMarshal.CreateSpan(ref element, 0x1000);
        }
    }
}
