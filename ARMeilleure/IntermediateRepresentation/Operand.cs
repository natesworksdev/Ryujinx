using System;
using System.Collections.Generic;

namespace ARMeilleure.IntermediateRepresentation
{
    class Operand
    {
        public OperandKind Kind { get; private set; }

        public OperandType Type { get; private set; }

        public ulong Value { get; private set; }

        public List<Node> Assignments { get; }
        public List<Node> Uses        { get; }

        public Operand()
        {
            Assignments = new List<Node>();
            Uses        = new List<Node>();
        }

        public Operand(OperandKind kind, OperandType type = OperandType.None) : this()
        {
            Kind = kind;
            Type = type;
        }

        public Operand With(OperandKind kind, OperandType type = OperandType.None)
        {
            Kind = kind;
            Type = type;
            Value = 0;

            Assignments.Clear();
            Uses.Clear();
            return this;
        }

        public Operand With(int value)
        {
            With(OperandKind.Constant, OperandType.I32);
            Value = (uint)value;
            return this;
        }

        public Operand With(uint value)
        {
            With(OperandKind.Constant, OperandType.I32);
            Value = (uint)value;
            return this;
        }

        public Operand With(long value)
        {
            With(OperandKind.Constant, OperandType.I64);
            Value = (ulong)value;
            return this;
        }

        public Operand With(ulong value)
        {
            With(OperandKind.Constant, OperandType.I64);
            Value = value;
            return this;
        }

        public Operand With(float value)
        {
            With(OperandKind.Constant, OperandType.FP32);
            Value = (ulong)BitConverter.SingleToInt32Bits(value);
            return this;
        }

        public Operand With(double value)
        {
            With(OperandKind.Constant, OperandType.FP64);
            Value = (ulong)BitConverter.DoubleToInt64Bits(value);
            return this;
        }

        public Operand With(int index, RegisterType regType, OperandType type)
        {
            With(OperandKind.Register, type);
            Value = (ulong)((int)regType << 24 | index);
            return this;
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

        internal void NumberLocal(int number)
        {
            if (Kind != OperandKind.LocalVariable)
            {
                throw new InvalidOperationException("The operand is not a local variable.");
            }

            Value = (ulong)number;
        }

        public override int GetHashCode()
        {
            if (Kind == OperandKind.LocalVariable)
            {
                return base.GetHashCode();
            }
            else
            {
                return (int)Value ^ ((int)Kind << 16) ^ ((int)Type << 20);
            }
        }
    }
}