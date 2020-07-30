using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.Network.Types
{
    [StructLayout(LayoutKind.Sequential, Size = 0x1, Pack = 1)]
    struct SetAcceptPolicyRequest
    {
        public byte StationAcceptPolicy;
    }
}
