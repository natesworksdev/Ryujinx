using Ryujinx.HLE.HOS.Services.Mii.Types;
using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Mii
{
    [StructLayout(LayoutKind.Sequential, Pack = 4, Size = 0x10)]
    class DatabaseSessionMetadata
    {
        public uint  InterfaceVersion;
        public ulong UpdateCounter;

        public SpecialMiiKeyCode MiiKeyCode { get; private set; }


        public DatabaseSessionMetadata(ulong updateCounter, SpecialMiiKeyCode miiKeyCode)
        {
            InterfaceVersion = 0;
            UpdateCounter    = updateCounter;
            MiiKeyCode       = miiKeyCode;
        }

        public bool IsInterfaceVersionSupported(uint interfaceVersion)
        {
            return InterfaceVersion >= interfaceVersion;
        }
    }
}
