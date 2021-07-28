using ARMeilleure.Translation.PTC;
using System.Runtime.CompilerServices;

namespace ARMeilleure.IntermediateRepresentation
{
    static class OperandHelper
    {
        public static Operand Const(OperandType type, long value)
        {
            return type == OperandType.I32 ? Operand().With((int)value) : Operand().With(value);
        }

        public static Operand Const(bool value)
        {
            return Operand().With(value ? 1 : 0);
        }

        public static Operand Const(int value)
        {
            return Operand().With(value);
        }

        public static Operand Const(uint value)
        {
            return Operand().With(value);
        }

        public static Operand Const(long value)
        {
            return Operand().With(value);
        }

        public static Operand Const(long value, Symbol symbol)
        {
            return Operand().With(value, symbol);
        }

        public static Operand Const(ulong value)
        {
            return Operand().With(value);
        }

        public static unsafe Operand Const<T>(ref T reference, Symbol symbol = default)
        {
            return Operand().With((long)Unsafe.AsPointer(ref reference), symbol);
        }

        public static Operand ConstF(float value)
        {
            return Operand().With(value);
        }

        public static Operand ConstF(double value)
        {
            return Operand().With(value);
        }

        public static Operand Label()
        {
            return Operand().With(OperandKind.Label);
        }

        public static Operand Local(OperandType type)
        {
            return Operand().With(OperandKind.LocalVariable, type);
        }

        public static Operand Register(int index, RegisterType regType, OperandType type)
        {
            return Operand().With(index, regType, type);
        }

        public static Operand Undef()
        {
            return Operand().With(OperandKind.Undefined);
        }

        public static Operand MemoryOp(
            OperandType type,
            Operand baseAddress,
            Operand index = default,
            Multiplier scale = Multiplier.x1,
            int displacement = 0)
        {
            Operand result = Operand().With(OperandKind.Memory, type);
            ref MemoryOperand memory = ref result.GetMemory();
            memory.BaseAddress = baseAddress;
            memory.Index = index;
            memory.Scale = scale;
            memory.Displacement = displacement;
            return result;
        }

        private static Operand Operand()
        {
            return IntermediateRepresentation.Operand.New();
        }
    }
}
