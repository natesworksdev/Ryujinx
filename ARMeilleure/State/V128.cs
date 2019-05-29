using System;

namespace ARMeilleure.State
{
    public struct V128 : IEquatable<V128>
    {
        private ulong _e0;
        private ulong _e1;

        public V128(float value)
        {
            _e0 = (uint)BitConverter.SingleToInt32Bits(value);
            _e1 = 0;
        }

        public V128(double value)
        {
            _e0 = (ulong)BitConverter.DoubleToInt64Bits(value);
            _e1 = 0;
        }

        public V128(long e0, long e1) : this((ulong)e0, (ulong)e1) { }

        public V128(ulong e0, ulong e1)
        {
            _e0 = e0;
            _e1 = e1;
        }

        public float AsFloat()
        {
            return GetFloat(0);
        }

        public double AsDouble()
        {
            return GetDouble(0);
        }

        public float GetFloat(int index)
        {
            return BitConverter.Int32BitsToSingle(GetInt32(index));
        }

        public double GetDouble(int index)
        {
            return BitConverter.Int64BitsToDouble(GetInt64(index));
        }

        public int GetInt32(int index) => (int)GetUInt32(index);

        public uint GetUInt32(int index)
        {
            if ((uint)index > 3)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return (uint)((((index & 2) != 0) ? _e1 : _e0) >> (index & 1));
        }

        public long GetInt64(int index) => (long)GetUInt64(index);

        public ulong GetUInt64(int index)
        {
            switch (index)
            {
                case 0: return _e0;
                case 1: return _e1;
            }

            throw new ArgumentOutOfRangeException(nameof(index));
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_e0, _e1);
        }

        public static bool operator ==(V128 x, V128 y)
        {
            return x.Equals(y);
        }

        public static bool operator !=(V128 x, V128 y)
        {
            return !x.Equals(y);
        }

        public override bool Equals(object obj)
        {
            return obj is V128 vector && Equals(vector);
        }

        public bool Equals(V128 other)
        {
            return other._e0 == _e0 && other._e1 == _e1;
        }
    }
}