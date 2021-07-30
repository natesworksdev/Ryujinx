using ARMeilleure.Translation.PTC;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ARMeilleure.IntermediateRepresentation
{
    unsafe struct Operand
    {
        private struct Data
        {
            public byte Kind;
            public byte Type;
            public byte SymbolType;
            public ArenaList<Operation> Assignments;
            public ArenaList<Operation> Uses;
            public ulong Value;
            public ulong SymbolValue;
        }

        private Data* _data;

        public OperandKind Kind
        {
            get => (OperandKind)_data->Kind;
            private set => _data->Kind = (byte)value;
        }

        public OperandType Type
        {
            get => (OperandType)_data->Type;
            private set => _data->Type = (byte)value;
        }

        public ulong Value
        {
            get => _data->Value;
            private set => _data->Value = value;
        }

        public Symbol Symbol
        {
            get
            {
                Debug.Assert(Kind != OperandKind.Memory);

                return new Symbol((SymbolType)_data->SymbolType, _data->SymbolValue);
            }
            private set
            {
                Debug.Assert(Kind != OperandKind.Memory);

                if (value.Type == SymbolType.None)
                {
                    _data->SymbolType = (byte)SymbolType.None;
                }
                else
                {
                    _data->SymbolType = (byte)value.Type;
                    _data->SymbolValue = value.Value;
                }
            }
        }

        public ref ArenaList<Operation> Assignments
        {
            get
            {
                Debug.Assert(Kind != OperandKind.Memory);

                return ref _data->Assignments;
            }
        }

        public ref ArenaList<Operation> Uses
        {
            get
            {
                Debug.Assert(Kind != OperandKind.Memory);

                return ref _data->Uses;
            }
        }

        public bool Relocatable => Symbol.Type != SymbolType.None;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Register GetRegister()
        {
            Debug.Assert(Kind == OperandKind.Register);

            return new Register((int)Value & 0xffffff, (RegisterType)(Value >> 24));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MemoryOperand GetMemory()
        {
            Debug.Assert(Kind == OperandKind.Memory);

            return MemoryOperand.Cast(this);
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
            private const int InternTableSize = 128;

            [ThreadStatic]
            private static Data* _internTable;

            private static Data* InternTable
            {
                get
                {
                    if (_internTable == null)
                    {
                        _internTable = (Data*)Marshal.AllocHGlobal(sizeof(Data) * InternTableSize);

                        if (_internTable == null)
                        {
                            throw new OutOfMemoryException();
                        }

                        new Span<Data>(_internTable, InternTableSize).Clear();
                    }

                    return _internTable;
                }
            }

            private static Operand Make(OperandKind kind, OperandType type, ulong value, Symbol symbol = default)
            {
                Debug.Assert(kind != OperandKind.None);

                Data* data;

                // If constant or register, then try to look up in the intern table before allocating.
                if (kind == OperandKind.Constant || kind == OperandKind.Register)
                {
                    data = &InternTable[(uint)HashCode.Combine(kind, type, value) % InternTableSize];

                    Operand interned = new();
                    interned._data = data;

                    // If slot matches the allocation request then return that slot.
                    if (interned.Kind == kind && interned.Type == type && interned.Value == value && interned.Symbol == symbol)
                    {
                        return interned;
                    }
                    // Otherwise if the slot is already occupied we have to store elsewhere.
                    else if (interned.Kind != OperandKind.None)
                    {
                        data = Arena<Data>.Alloc();
                    }
                }
                else
                {
                    data = Arena<Data>.Alloc();
                }

                *data = default;

                Operand result = new();
                result._data = data;
                result.Value = value;
                result.Kind = kind;
                result.Type = type;

                // If local variable, then the use and def list is initialized with default sizes.
                if (kind == OperandKind.LocalVariable)
                {
                    result._data->Assignments = ArenaList<Operation>.New(1);
                    result._data->Uses = ArenaList<Operation>.New(4);
                }

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
                return Const(value, symbol: default);
            }

            public static Operand Const<T>(ref T reference, Symbol symbol = default)
            {
                return Const((long)Unsafe.AsPointer(ref reference), symbol);
            }

            public static Operand Const(long value, Symbol symbol)
            {
                return Make(OperandKind.Constant, OperandType.I64, (ulong)value, symbol);
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

                MemoryOperand memory = result.GetMemory();
                memory.BaseAddress = baseAddress;
                memory.Index = index;
                memory.Scale = scale;
                memory.Displacement = displacement;

                return result;
            }
        }
    }
}