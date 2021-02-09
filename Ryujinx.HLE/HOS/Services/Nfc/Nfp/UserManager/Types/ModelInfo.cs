using Ryujinx.Common.Memory;
using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Nfc.Nfp.UserManager
{
    [StructLayout(LayoutKind.Sequential, Size = 0x40)]
    struct ModelInfo
    {
        public Array8<byte>  AmiiboId;
        public Array56<byte> Reserved;
    }
}
