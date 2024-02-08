using Ryujinx.Common.Memory;
using System.Runtime.InteropServices;

namespace Ryujinx.Horizon.Sdk.Audio.Detail
{
    [StructLayout(LayoutKind.Sequential, Size = 0x8, Pack = 0x1)]
    struct AudioInProtocol
    {
        public Array8<byte> Value;

        public AudioInProtocol(bool value)
        {
            Value[0] = value ? (byte)1 : (byte)0;
        }

        public override string ToString()
        {
            return (Value[0] != 0).ToString();
        }
    }
}
