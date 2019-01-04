using ChocolArm64.Memory;
using System;

namespace Luea.Devices
{
    public class Device : IBus
    {
        public byte ReadByte(ulong address)
        {
            Console.WriteLine($"ReadByte to unimplemented device at 0x{address:X16}.");

            return 0;
        }

        public ushort ReadUInt16(ulong address)
        {
            Console.WriteLine($"ReadUInt16 to unimplemented device at 0x{address:X16}.");

            return 0;
        }

        public uint ReadUInt32(ulong address)
        {
            Console.WriteLine($"ReadUInt32 to unimplemented device at 0x{address:X16}.");

            return 0;
        }

        public ulong ReadUInt64(ulong address)
        {
            Console.WriteLine($"ReadUInt64 to unimplemented device at 0x{address:X16}.");

            return 0;
        }

        public void WriteByte(ulong address, byte value)
        {
            Console.WriteLine($"WriteByte to unimplemented device at 0x{address:X16} = 0x{value:X2}.");
        }

        public void WriteUInt16(ulong address, ushort value)
        {
            Console.WriteLine($"WriteUInt16 to unimplemented device at 0x{address:X16} = 0x{value:X4}.");
        }

        public void WriteUInt32(ulong address, uint value)
        {
            Console.WriteLine($"WriteUInt32 to unimplemented device at 0x{address:X16} = 0x{value:X8}.");
        }

        public void WriteUInt64(ulong address, ulong value)
        {
            Console.WriteLine($"WriteUInt64 to unimplemented device at 0x{address:X16} = 0x{value:X16}.");
        }
    }
}
