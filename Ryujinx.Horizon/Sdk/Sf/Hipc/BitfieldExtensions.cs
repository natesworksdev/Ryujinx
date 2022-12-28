using System.Runtime.CompilerServices;

namespace Ryujinx.Horizon.Sdk.Sf.Hipc
{
    static class BitfieldExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Extract(this uint value, int lsb)
        {
            return ((value >> lsb) & 1) != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint Extract(this uint value, int lsb, int length)
        {
            return (value >> lsb) & (uint.MaxValue >> (32 - length));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint Insert(this uint value, int lsb, bool toInsert)
        {
            uint mask = 1u << lsb;

            return (value & ~mask) | (toInsert ? mask : 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint Insert(this uint value, int lsb, int length, uint toInsert)
        {
            uint mask = (uint.MaxValue >> (32 - length)) << lsb;

            return (value & ~mask) | ((toInsert << lsb) & mask);
        }
    }
}