using ARMeilleure.Translation.PTC;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

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
            public NativeList<Operation> Assignments;
            public NativeList<Operation> Uses;
        }

        private Data *_data;

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

        public ref NativeList<Operation> Assignments => ref _data->Assignments;
        public ref NativeList<Operation> Uses => ref _data->Uses;

        public bool Relocatable => Symbol.Type != SymbolType.None;

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

        public static class Factory
        {
            private static Operand Make(OperandKind kind, OperandType type, ulong value)
            {
                Data* data = Arena<Data>.Alloc();
                *data = default;

                Operand result = new();
                result._data = data;
                result._data->Kind = kind;
                result._data->Type = type;
                result._data->Value = value;
                result._data->Assignments = NativeList<Operation>.New(1);
                result._data->Uses = NativeList<Operation>.New(4);

                return result;
            }

            public static Operand Const(OperandType type, long value)
            {
                Debug.Assert(type is OperandType.I32 or OperandType.I64);

                return type == OperandType.I32 ? Const((int)value) : Const(value);
            }

            public static Operand Const(bool value)
            {
                return Const(value ? 1 : 0);
            }

            public static Operand Const(int value)
            {
                return Const((uint)value);
            }

            public static Operand Const(uint value)
            {
                return Make(OperandKind.Constant, OperandType.I32, value);
            }

            public static Operand Const(long value)
            {
                return Const((ulong)value);
            }

            public static Operand Const(long value, Symbol symbol)
            {
                Operand result = Const(value);
                result.Symbol = symbol;
                return result;
            }

            public static Operand Const<T>(ref T reference, Symbol symbol = default)
            {
                return Const((long)Unsafe.AsPointer(ref reference), symbol);
            }

            public static Operand Const(ulong value)
            {
                return Make(OperandKind.Constant, OperandType.I64, value);
            }

            public static Operand ConstF(float value)
            {
                return Make(OperandKind.Constant, OperandType.FP32, (ulong)BitConverter.SingleToInt32Bits(value));
            }

            public static Operand ConstF(double value)
            {
                return Make(OperandKind.Constant, OperandType.FP64, (ulong)BitConverter.DoubleToInt64Bits(value));
            }

            public static Operand Label()
            {
                return Make(OperandKind.Label, OperandType.None, 0);
            }

            public static Operand Local(OperandType type)
            {
                return Make(OperandKind.LocalVariable, type, 0);
            }

            public static Operand Register(int index, RegisterType regType, OperandType type)
            {
                return Make(OperandKind.Register, type, (ulong)((int)regType << 24 | index));
            }

            public static Operand Undef()
            {
                return Make(OperandKind.Undefined, OperandType.None, 0);
            }

            public static Operand MemoryOp(
                OperandType type,
                Operand baseAddress,
                Operand index = default,
                Multiplier scale = Multiplier.x1,
                int displacement = 0)
            {
                Operand result = Make(OperandKind.Memory, type, 0);

                ref MemoryOperand memory = ref result.GetMemory();
                memory.BaseAddress = baseAddress;
                memory.Index = index;
                memory.Scale = scale;
                memory.Displacement = displacement;

                return result;
            }
        }
    }
}