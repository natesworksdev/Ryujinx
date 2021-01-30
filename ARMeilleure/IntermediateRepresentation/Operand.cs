using System;

namespace ARMeilleure.IntermediateRepresentation
{
    class Operand : IEquatable<Operand>
    {
        public OperandKind Kind { get; }
        public OperandType Type { get; }

        public ulong Value { get; }

        public bool Relocatable { get; }
        public int? PtcIndex { get; }

        public Operand()
        {
        }

        public Operand(OperandKind kind, OperandType type = OperandType.None)
        {
            Kind = kind;
            Type = type;
        }

        public Operand(
            OperandKind kind,
            OperandType type = OperandType.None,
            ulong value = 0,
            bool relocatable = false,
            int? index = null)
        {
            Kind = kind;
            Type = type;

            Value = value;

            Relocatable = relocatable;
            PtcIndex = index;
        }

        public Operand(int value) : this(OperandKind.Constant, OperandType.I32, (uint)value)
        {
        }

        public Operand(uint value) : this(OperandKind.Constant, OperandType.I32, value)
        {
        }

        public Operand(long value, bool relocatable = false, int? index = null) : this(OperandKind.Constant, OperandType.I64, (ulong)value, relocatable, index)
        {
        }

        public Operand(ulong value) : this(OperandKind.Constant, OperandType.I64, value)
        {
        }

        public Operand(float value) : this(OperandKind.Constant, OperandType.FP32, (ulong)BitConverter.SingleToInt32Bits(value))
        {
        }

        public Operand(double value) : this(OperandKind.Constant, OperandType.FP64, (ulong)BitConverter.DoubleToInt64Bits(value))
        {
        }

        public Operand(int index, RegisterType regType, OperandType type) : this(OperandKind.Register, type, (ulong)((int)regType << 24 | index))
        {
        }

        public Register GetRegister()
        {
            return new Register((int)Value & 0xffffff, (RegisterType)(Value >> 24));
        }

        public byte AsByte()
        {
            return (byte)Value;
        }

        public short AsInt16()
        {
            return (short)Value;
        }

        public int AsInt32()
        {
            return (int)Value;
        }

        public long AsInt64()
        {
            return (long)Value;
        }

        public float AsFloat()
        {
            return BitConverter.Int32BitsToSingle((int)Value);
        }

        public double AsDouble()
        {
            return BitConverter.Int64BitsToDouble((long)Value);
        }

        /* public static bool operator ==(Operand x, Operand y)
        {
            return x.Equals(y);
        }

        public static bool operator !=(Operand x, Operand y)
        {
            return !(x == y);
        } */

        public override bool Equals(object obj)
        {
            return obj is Operand other && Equals(other);
        }

        public bool Equals(Operand other)
        {
            return Kind == other.Kind && Type == other.Type && Value == other.Value && Relocatable == other.Relocatable && PtcIndex == other.PtcIndex;
        }

        public int GetBaseHashCode()
        {
            return base.GetHashCode();
        }

        public override int GetHashCode()
        {
            return (int)Value ^ ((int)Kind << 16) ^ ((int)Type << 20);
        }
    }
}