using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Ldn.Types
{
    [StructLayout(LayoutKind.Sequential, Size = 0x7C)]
    struct ConnectNetworkData
    {
        public SecurityConfig SecurityConfig;
        public UserConfig     UserConfig;
        public uint           LocalCommunicationVersion;
        public uint           Option;
    }
}