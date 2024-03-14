using System.Runtime.InteropServices;

namespace Ryujinx.Horizon.Sdk.Am
{
    [StructLayout(LayoutKind.Sequential, Size = 0x80)]
    public struct AppletAttribute
    {
        // TODO: Better way to rep single bit flag
        public bool flag;
    }
}
