namespace Ryujinx.HLE.OsHle.Utilities
{
    static class EndianSwap
    {
        public static short Swap16(short Value) => (short)(((Value >> 8) & 0xff) | (Value << 8));
        public static ushort Swap16(ushort Value) => (ushort)(((Value >> 8) & 0xff) | (Value << 8));
    }
}
