using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Spv.Generator
{
    public class LiteralInteger : Operand, IEquatable<LiteralInteger>
    {
        public OperandType Type => OperandType.Number;

        private enum IntegerType
        {
            UInt32,
            Int32,
            UInt64,
            Int64,
            Float32,
            Float64,
        }

        private IntegerType _integerType;

        private byte[] _data;

        private LiteralInteger(byte[] data, IntegerType integerType)
        {
            _data = data;
            _integerType = integerType;
        }

        public static implicit operator LiteralInteger(int value) => Create(value, IntegerType.Int32);
        public static implicit operator LiteralInteger(uint value) => Create(value, IntegerType.UInt32);
        public static implicit operator LiteralInteger(long value) => Create(value, IntegerType.Int64);
        public static implicit operator LiteralInteger(ulong value) => Create(value, IntegerType.UInt64);
        public static implicit operator LiteralInteger(float value) => Create(value, IntegerType.Float32);
        public static implicit operator LiteralInteger(double value) => Create(value, IntegerType.Float64);
        public static implicit operator LiteralInteger(Enum value) => Create((int)Convert.ChangeType(value, typeof(int)), IntegerType.Int32);

        // NOTE: this is not in the standard, but this is some syntax sugar useful in some instructions (TypeInt ect)
        public static implicit operator LiteralInteger(bool value) => Create(Convert.ToInt32(value), IntegerType.Int32);


        public static LiteralInteger CreateForEnum<T>(T value) where T : struct
        {
            return Create(value, IntegerType.Int32);
        }

        private static LiteralInteger Create<T>(T value, IntegerType integerType) where T: struct
        {
            return new LiteralInteger(MemoryMarshal.Cast<T, byte>(MemoryMarshal.CreateSpan(ref value, 1)).ToArray(), integerType);
        }

        public ushort WordCount => (ushort)(_data.Length / 4);

        public void WriteOperand(Stream stream)
        {
            stream.Write(_data);
        }

        public override bool Equals(object obj)
        {
            return obj is LiteralInteger literalInteger && Equals(literalInteger);
        }

        public bool Equals(LiteralInteger cmpObj)
        {
            return Type == cmpObj.Type && _integerType == cmpObj._integerType && _data.SequenceEqual(cmpObj._data);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Type, _data);
        }

        public bool Equals(Operand obj)
        {
            return obj is LiteralInteger literalInteger && Equals(literalInteger);
        }
    }
}
