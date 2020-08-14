using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Ldn.Types
{
    [StructLayout(LayoutKind.Sequential, Size = 0x20)]
    struct NetworkId
    {
        public IntentId IntentId;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x10)]
        public byte[]   SessionId;
    }
}