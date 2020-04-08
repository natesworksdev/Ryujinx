using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ARMeilleure.State
{
     [StructLayout(LayoutKind.Explicit, Size = 16)]
    public struct V128 : IEquatable<V128>
    {
        [FieldOffset(0)]
        private ulong _e0;
        [FieldOffset(8)]
        private ulong _e1;

        public static V128 Zero => new V128(0, 0);

        public V128(double value) : this(value, 0) { }
        public V128(double e0, double e1)
        {
            _e0 = (ulong)BitConverter.DoubleToInt64Bits(e0);
            _e1 = (ulong)BitConverter.DoubleToInt64Bits(e1);
        }

        public V128(float value) : this(value, 0, 0, 0) { }
        public V128(float e0, float e1, float e2, float e3)
        {
            _e0  = (ulong)(uint)BitConverter.SingleToInt32Bits(e0) << 0;
            _e0 |= (ulong)(uint)BitConverter.SingleToInt32Bits(e1) << 32;
            _e1  = (ulong)(uint)BitConverter.SingleToInt32Bits(e2) << 0;
            _e1 |= (ulong)(uint)BitConverter.SingleToInt32Bits(e3) << 32;
        }

        public V128(long e0, long e1) : this((ulong)e0, (ulong)e1) { }
        public V128(ulong e0, ulong e1)
        {
            _e0 = e0;
            _e1 = e1;
        }

        public V128(int e0, int e1, int e2, int e3) : this((uint)e0, (uint)e1, (uint)e2, (uint)e3) { }
        public V128(uint e0, uint e1, uint e2, uint e3)
        {
            _e0  = (ulong)e0 << 0;
            _e0 |= (ulong)e1 << 32;
            _e1  = (ulong)e2 << 0;
            _e1 |= (ulong)e3 << 32;
        }

        public V128(byte[] data)
        {
            _e0 = (ulong)BitConverter.ToInt64(data, 0);
            _e1 = (ulong)BitConverter.ToInt64(data, 8);
        }

        public float  AsFloat()            => GetFloat(0);
        public double AsDouble()           => GetDouble(0);
        public float  GetFloat(int index)  => BitConverter.Int32BitsToSingle(GetInt32(index));
        public double GetDouble(int index) => BitConverter.Int64BitsToDouble(GetInt64(index));
        public int    GetInt32(int index)  => (int)GetUInt32(index);
        public long   GetInt64(int index)  => (long)GetUInt64(index);

        public uint GetUInt32(int index)
        {
            if ((uint)index > 3U)
                ThrowIndexOutOfRange();

            return Unsafe.Add(ref Unsafe.As<ulong, uint>(ref _e0), index);
        }

        public ulong GetUInt64(int index)
        {
            if ((uint)index > 1U)
                ThrowIndexOutOfRange();

            return Unsafe.Add(ref _e0, index);
        }

        public void Insert(int index, uint value)
        {
            if ((uint)index > 3U)
                ThrowIndexOutOfRange();

            Unsafe.Add(ref Unsafe.As<ulong, uint>(ref _e0), index) = value;
        }

        public void Insert(int index, ulong value)
        {
            if ((uint)index > 1U)
                ThrowIndexOutOfRange();

            Unsafe.Add(ref _e0, index) = value;
        }

        public byte[] ToArray()
        {
            byte[]     data = new byte[16];
            Span<byte> span = data;

            BitConverter.TryWriteBytes(span, _e0);
            BitConverter.TryWriteBytes(span.Slice(8), _e1);

            return data;
        }

        public static V128 operator <<(V128 x, int shift)
        {
            ulong shiftOut = x._e0 >> (64 - shift);

            return new V128(x._e0 << shift, (x._e1 << shift) | shiftOut);
        }

        public static V128 operator >>(V128 x, int shift)
        {
            ulong shiftOut = x._e1 & ((1UL << shift) - 1);

            return new V128((x._e0 >> shift) | (shiftOut << (64 - shift)), x._e1 >> shift);
        }

        public static V128 operator ~(V128 x)          => new V128(~x._e0, ~x._e1);
        public static V128 operator &(V128 x, V128 y)  => new V128(x._e0 & y._e0, x._e1 & y._e1);
        public static V128 operator |(V128 x, V128 y)  => new V128(x._e0 | y._e0, x._e1 | y._e1);
        public static V128 operator ^(V128 x, V128 y)  => new V128(x._e0 ^ y._e0, x._e1 ^ y._e1);

        public static bool operator ==(V128 x, V128 y) =>  x.Equals(y);
        public static bool operator !=(V128 x, V128 y) => !x.Equals(y);

        public          bool Equals(V128 other) => other._e0 == _e0   && other._e1 == _e1;
        public override bool Equals(object obj) => obj is V128 vector && Equals(vector);

        public override int    GetHashCode() => HashCode.Combine(_e0, _e1);
        public override string ToString()    => $"0x{_e1:X16}{_e0:X16}";

        private static void ThrowIndexOutOfRange() => throw new ArgumentOutOfRangeException("index");
    }
}