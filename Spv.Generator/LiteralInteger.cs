using System;
using System.IO;

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
        private ulong _data;

        public ushort WordCount { get; }

        private LiteralInteger(ulong data, IntegerType integerType, ushort wordCount)
        {
            _data = data;
            _integerType = integerType;

            WordCount = wordCount;
        }

        public static implicit operator LiteralInteger(int value) => new LiteralInteger((ulong)value, IntegerType.Int32, 1);
        public static implicit operator LiteralInteger(uint value) => new LiteralInteger(value, IntegerType.UInt32, 1);
        public static implicit operator LiteralInteger(long value) => new LiteralInteger((ulong)value, IntegerType.Int64, 2);
        public static implicit operator LiteralInteger(ulong value) => new LiteralInteger(value, IntegerType.UInt64, 2);
        public static implicit operator LiteralInteger(float value) => new LiteralInteger(BitConverter.SingleToUInt32Bits(value), IntegerType.Float32, 1);
        public static implicit operator LiteralInteger(double value) => new LiteralInteger(BitConverter.DoubleToUInt64Bits(value), IntegerType.Float64, 2);
        public static implicit operator LiteralInteger(Enum value) => new LiteralInteger((ulong)Convert.ChangeType(value, typeof(ulong)), IntegerType.Int32, 1);

        // NOTE: this is not in the standard, but this is some syntax sugar useful in some instructions (TypeInt ect)
        public static implicit operator LiteralInteger(bool value) => new LiteralInteger(Convert.ToUInt64(value), IntegerType.Int32, 1);

        public static LiteralInteger CreateForEnum<T>(T value) where T : Enum
        {
            return value;
        }

        public void WriteOperand(BinaryWriter writer)
        {
            if (WordCount == 1)
            {
                writer.Write((uint)_data);
            }
            else
            {
                writer.Write(_data);
            }
        }

        public override bool Equals(object obj)
        {
            return obj is LiteralInteger literalInteger && Equals(literalInteger);
        }

        public bool Equals(LiteralInteger cmpObj)
        {
            return Type == cmpObj.Type && _integerType == cmpObj._integerType && _data == cmpObj._data;
        }

        public override int GetHashCode()
        {
            return DeterministicHashCode.Combine(Type, _data);
        }

        public bool Equals(Operand obj)
        {
            return obj is LiteralInteger literalInteger && Equals(literalInteger);
        }
    }
}
