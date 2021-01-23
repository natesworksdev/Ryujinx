using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Pcv
{
    [StructLayout(LayoutKind.Sequential)]
    struct PowerDomainState
    {
        [MarshalAs(UnmanagedType.U1)]
        public bool Enabled;
        [MarshalAs(UnmanagedType.U1, SizeConst = 0x3)]
        byte[] Reserved;
        public int Voltage;
    }
}