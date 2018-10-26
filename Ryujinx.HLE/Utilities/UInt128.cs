using System;
using System.IO;
using System.Linq;

namespace Ryujinx.HLE.Utilities
{
    public struct UInt128
    {
        public long High { get; private set; }
        public long Low  { get; private set; }

        public UInt128(long low, long high)
        {
            Low  = low;
            High = high;
        }

        public UInt128(string uInt128Hex)
        {
            if (uInt128Hex == null || uInt128Hex.Length != 32 || !uInt128Hex.All("0123456789abcdefABCDEF".Contains)) throw new ArgumentException("Invalid Hex value!", nameof(uInt128Hex));

            Low  = Convert.ToInt64(uInt128Hex.Substring(16), 16);
            High = Convert.ToInt64(uInt128Hex.Substring(0, 16), 16);
        }

        public void Write(BinaryWriter binaryWriter)
        {
            binaryWriter.Write(Low);
            binaryWriter.Write(High);
        }

        public override string ToString()
        {
            return High.ToString("x16") + Low.ToString("x16");
        }

        public bool IsZero()
        {
            return (Low | High) == 0;
        }
    }
}
