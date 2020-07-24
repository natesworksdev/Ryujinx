using Ryujinx.HLE.HOS.Services.Ldn.Types;
using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.Network.Types
{
    [StructLayout(LayoutKind.Sequential, Size = 0x4FC, CharSet = CharSet.Ansi)]
    struct ConnectRequest
    {
        public ConnectNetworkData Data;
        public NetworkInfo Info;
    }
}
