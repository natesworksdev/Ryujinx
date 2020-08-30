using Ryujinx.HLE.HOS.Services.Ldn.Types;
using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.RyuLdn.Types
{
    [StructLayout(LayoutKind.Sequential, Size = 0x13C, Pack = 1)]
    struct CreateAccessPointPrivateRequest
    {
        public SecurityConfig SecurityConfig;
        public SecurityParameter SecurityParameter;
        public UserConfig UserConfig;
        public NetworkConfig NetworkConfig;
        public AddressList AddressList;

        // Advertise data is appended separately. (remaining data in the buffer)
    }
}
