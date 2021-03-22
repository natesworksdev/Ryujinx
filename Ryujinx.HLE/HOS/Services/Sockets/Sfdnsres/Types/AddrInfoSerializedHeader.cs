using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Sockets.Sfdnsres.Types
{
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 6 * sizeof(int))]
    struct AddrInfoSerializedHeader
    {
        public uint Magic;
        public int  Flags;
        public int  Family;
        public int  SocketType;
        public int  Protocol;
        public uint AddressLength;

        public AddrInfoSerializedHeader(IPAddress address, SocketType socketType)
        {
            Magic      = (uint)BinaryPrimitives.ReverseEndianness(unchecked((int)SfdnsresContants.AddrInfoMagic));
            Flags      = BinaryPrimitives.ReverseEndianness(0);
            Family     = BinaryPrimitives.ReverseEndianness((int)address.AddressFamily);
            SocketType = BinaryPrimitives.ReverseEndianness((int)socketType);
            Protocol   = BinaryPrimitives.ReverseEndianness(0);

            if (address.AddressFamily == AddressFamily.InterNetwork)
            {
                AddressLength = (uint)Unsafe.SizeOf<AddrInfo4>();
            }
            else
            {
                AddressLength = 4;
            }
        }
    }
}