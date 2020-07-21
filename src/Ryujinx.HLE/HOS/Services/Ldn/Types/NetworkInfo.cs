using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Ldn.Types
{
    [StructLayout(LayoutKind.Sequential, Size = 0x480, CharSet = CharSet.Ansi)]
    struct NetworkInfo
    {
        public NetworkId         NetworkId;
        public CommonNetworkInfo Common;
        public LdnNetworkInfo    Ldn;
    }
}
