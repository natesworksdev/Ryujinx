using Ryujinx.Common.Memory;
using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Applets.Error
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct ApplicationErrorArg
    {
        public uint            ErrorNumber;
        public ulong           LanguageCode;
        public Array2048<byte> MessageText;
        public Array2048<byte> DetailsText;
    }
}