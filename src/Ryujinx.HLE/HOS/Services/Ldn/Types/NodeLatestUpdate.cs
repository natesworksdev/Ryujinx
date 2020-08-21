using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Ldn.Types
{
    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct NodeLatestUpdate
    {
        public byte   State;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 7)]
        public byte[] Reserved;
    }
}
