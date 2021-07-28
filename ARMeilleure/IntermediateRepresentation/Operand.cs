using ARMeilleure.Translation.PTC;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using static ARMeilleure.IntermediateRepresentation.OperationHelper;

namespace ARMeilleure.IntermediateRepresentation
{
    unsafe struct Operand
    {
        private struct Data
        {
            public OperandKind Kind;
            public OperandType Type;
            public ulong Value;
            public Symbol Symbol;
            public MemoryOperand Memory;
        }

        private Data *_data;

        public bool Relocatable => Symbol.Type != SymbolType.None;

        public OperandKind Kind
        {
            get => _data->Kind;
            private set => _data->Kind = value; 
        }

        public OperandType Type
        {
            get => _data->Type;
            private set => _data->Type = value;
        }

        public ulong Value
        {
            get => _data->Value;
            private set => _data->Value = value;
        }

        public Symbol Symbol
        {
            get => _data->Symbol;
            private set => _data->Symbol = value;
        }

        // TODO(FIXME);
        public List<Operation> Assignments => new() { Operation(Instruction.Extended, default) };
        public List<Operation> Uses => new() { Operation(Instruction.Extended, default) };

        public Operand(OperandKind kind, OperandType type = OperandType.None) : this()
        {
            Kind = kind;
            Type = type;
        }

        public Operand With(
            OperandKind kind,
            OperandType type = OperandType.None,
            ulong value = 0,
            Symbol symbol = default)
        {
            Kind = kind;
            Type = type;
            Value = value;
            Symbol = symbol;

            Assignments.Clear();
            Uses.Clear();

            return this;
        }

        public Operand With(int value)
        {
            return With(OperandKind.Constant, OperandType.I32, (uint)value);
        }

        public Operand With(uint value)
        {
            return With(OperandKind.Constant, OperandType.I32, value);
        }

        public Operand With(long value)
        {
            return With(OperandKind.Constant, OperandType.I64, (ulong)value);
        }

        public Operand With(long value, Symbol symbol)
        {
            return With(OperandKind.Constant, OperandType.I64, (ulong)value, symbol);
        }

        public Operand With(ulong value)
        {
            return With(OperandKind.Constant, OperandType.I64, value);
        }

        public Operand With(float value)
        {
            return With(OperandKind.Constant, OperandType.FP32, (ulong)BitConverter.SingleToInt32Bits(value));
        }

        public Operand With(double value)
        {
            return With(OperandKind.Constant, OperandType.FP64, (ulong)BitConverter.DoubleToInt64Bits(value));
        }

        public Operand With(int index, RegisterType regType, OperandType type)
        {
            return With(OperandKind.Register, type, (ulong)((int)regType << 24 | index));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Register GetRegister()
        {
            Debug.Assert(Kind == OperandKind.Register);

            return new Register((int)Value & 0xffffff, (RegisterType)(Value >> 24));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref MemoryOperand GetMemory()
        {
            Debug.Assert(Kind == OperandKind.Memory);

            return ref _data->Memory;
        }

        public int GetLocalNumber()
        {
            Debug.Assert(Kind == OperandKind.LocalVariable);

            return (int)Value;
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

        public static Operand New()
        {
            var result = new Operand();

            result._data = (Data*)Marshal.AllocHGlobal(sizeof(Data));

            if (result._data == null)
            {
                throw new OutOfMemoryException();
            }

            return result;
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

        public bool Equals(Operand operand)
        {
            return operand._data == _data;
        }

        public override bool Equals(object obj)
        {
            return obj is Operand operand && Equals(operand);
        }

        public static bool operator ==(Operand a, Operand b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(Operand a, Operand b)
        {
            return !a.Equals(b);
        }
    }
}