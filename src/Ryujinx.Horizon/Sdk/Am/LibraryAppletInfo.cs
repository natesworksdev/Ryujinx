using System.Runtime.InteropServices;

namespace Ryujinx.Horizon.Sdk.Am
{
    [StructLayout(LayoutKind.Sequential, Size = 0x8)]
    struct LibraryAppletInfo
    {
        public AppletId AppletId;
        public LibraryAppletMode LibraryAppletMode;
    }
}
