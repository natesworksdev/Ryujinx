using System;

namespace Ryujinx.HLE.Utilities
{
    static class UInt128Utils
    {
        public static UInt128 FromHex(string hex)
        {
            return new UInt128((ulong)Convert.ToInt64(hex.Substring(16), 16), (ulong)Convert.ToInt64(hex.Substring(0, 16), 16));
        }

        public static UInt128 CreateRandom()
        {
            Random random = new Random();
            return new UInt128((ulong)random.NextInt64(), (ulong)random.NextInt64());
        }
    }
}