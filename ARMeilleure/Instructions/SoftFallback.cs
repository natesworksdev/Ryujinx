using System;

namespace ARMeilleure.Instructions
{
    static class SoftFallback
    {
#region "ShlReg"

#endregion

#region "ShrImm64"

#endregion

#region "Count"
        public static ulong CountSetBits8(ulong value) // "size" is 8 (SIMD&FP Inst.).
        {
            value = ((value >> 1) & 0x55ul) + (value & 0x55ul);
            value = ((value >> 2) & 0x33ul) + (value & 0x33ul);

            return (value >> 4) + (value & 0x0ful);
        }
#endregion

#region "Crc32"
        private const uint Crc32RevPoly  = 0xedb88320;
        private const uint Crc32cRevPoly = 0x82f63b78;

        public static uint Crc32b(uint crc, byte   val) => Crc32 (crc, Crc32RevPoly, val);
        public static uint Crc32h(uint crc, ushort val) => Crc32h(crc, Crc32RevPoly, val);
        public static uint Crc32w(uint crc, uint   val) => Crc32w(crc, Crc32RevPoly, val);
        public static uint Crc32x(uint crc, ulong  val) => Crc32x(crc, Crc32RevPoly, val);

        public static uint Crc32cb(uint crc, byte   val) => Crc32 (crc, Crc32cRevPoly, val);
        public static uint Crc32ch(uint crc, ushort val) => Crc32h(crc, Crc32cRevPoly, val);
        public static uint Crc32cw(uint crc, uint   val) => Crc32w(crc, Crc32cRevPoly, val);
        public static uint Crc32cx(uint crc, ulong  val) => Crc32x(crc, Crc32cRevPoly, val);

        private static uint Crc32h(uint crc, uint poly, ushort val)
        {
            crc = Crc32(crc, poly, (byte)(val >> 0));
            crc = Crc32(crc, poly, (byte)(val >> 8));

            return crc;
        }

        private static uint Crc32w(uint crc, uint poly, uint val)
        {
            crc = Crc32(crc, poly, (byte)(val >> 0 ));
            crc = Crc32(crc, poly, (byte)(val >> 8 ));
            crc = Crc32(crc, poly, (byte)(val >> 16));
            crc = Crc32(crc, poly, (byte)(val >> 24));

            return crc;
        }

        private static uint Crc32x(uint crc, uint poly, ulong val)
        {
            crc = Crc32(crc, poly, (byte)(val >> 0 ));
            crc = Crc32(crc, poly, (byte)(val >> 8 ));
            crc = Crc32(crc, poly, (byte)(val >> 16));
            crc = Crc32(crc, poly, (byte)(val >> 24));
            crc = Crc32(crc, poly, (byte)(val >> 32));
            crc = Crc32(crc, poly, (byte)(val >> 40));
            crc = Crc32(crc, poly, (byte)(val >> 48));
            crc = Crc32(crc, poly, (byte)(val >> 56));

            return crc;
        }

        private static uint Crc32(uint crc, uint poly, byte val)
        {
            crc ^= val;

            for (int bit = 7; bit >= 0; bit--)
            {
                uint mask = (uint)(-(int)(crc & 1));

                crc = (crc >> 1) ^ (poly & mask);
            }

            return crc;
        }
#endregion

#region "Aes"

#endregion

#region "Sha1"

#endregion

#region "Sha256"

#endregion

#region "Reverse"
        public static uint ReverseBits8(uint value)
        {
            value = ((value & 0xaa) >> 1) | ((value & 0x55) << 1);
            value = ((value & 0xcc) >> 2) | ((value & 0x33) << 2);

            return (value >> 4) | ((value & 0x0f) << 4);
        }

        public static uint ReverseBits32(uint value)
        {
            value = ((value & 0xaaaaaaaa) >> 1) | ((value & 0x55555555) << 1);
            value = ((value & 0xcccccccc) >> 2) | ((value & 0x33333333) << 2);
            value = ((value & 0xf0f0f0f0) >> 4) | ((value & 0x0f0f0f0f) << 4);
            value = ((value & 0xff00ff00) >> 8) | ((value & 0x00ff00ff) << 8);

            return (value >> 16) | (value << 16);
        }

        public static ulong ReverseBits64(ulong value)
        {
            value = ((value & 0xaaaaaaaaaaaaaaaa) >> 1 ) | ((value & 0x5555555555555555) << 1 );
            value = ((value & 0xcccccccccccccccc) >> 2 ) | ((value & 0x3333333333333333) << 2 );
            value = ((value & 0xf0f0f0f0f0f0f0f0) >> 4 ) | ((value & 0x0f0f0f0f0f0f0f0f) << 4 );
            value = ((value & 0xff00ff00ff00ff00) >> 8 ) | ((value & 0x00ff00ff00ff00ff) << 8 );
            value = ((value & 0xffff0000ffff0000) >> 16) | ((value & 0x0000ffff0000ffff) << 16);

            return (value >> 32) | (value << 32);
        }

        public static uint ReverseBytes16_32(uint value) => (uint)ReverseBytes16_64(value);

        public static ulong ReverseBytes16_64(ulong value) => ReverseBytes(value, RevSize.Rev16);
        public static ulong ReverseBytes32_64(ulong value) => ReverseBytes(value, RevSize.Rev32);

        private enum RevSize
        {
            Rev16,
            Rev32,
            Rev64
        }

        private static ulong ReverseBytes(ulong value, RevSize size)
        {
            value = ((value & 0xff00ff00ff00ff00) >> 8) | ((value & 0x00ff00ff00ff00ff) << 8);

            if (size == RevSize.Rev16)
            {
                return value;
            }

            value = ((value & 0xffff0000ffff0000) >> 16) | ((value & 0x0000ffff0000ffff) << 16);

            if (size == RevSize.Rev32)
            {
                return value;
            }

            value = ((value & 0xffffffff00000000) >> 32) | ((value & 0x00000000ffffffff) << 32);

            if (size == RevSize.Rev64)
            {
                return value;
            }

            throw new ArgumentException(nameof(size));
        }
#endregion
    }
}
