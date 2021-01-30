namespace ARMeilleure.IntermediateRepresentation
{
    static class OperandHelper
    {
        public static Operand Const(OperandType type, long value)
        {
            return type == OperandType.I32 ? new Operand((int)value) : new Operand(value);
        }

        public static Operand Const(bool value)
        {
            return new Operand(value ? 1 : 0);
        }

        public static Operand Const(int value)
        {
            return new Operand(value);
        }

        public static Operand Const(uint value)
        {
            return new Operand(value);
        }

        public static Operand Const(long value, bool relocatable = false, int? index = null)
        {
            return new Operand(value, relocatable, index);
        }

        public static Operand Const(ulong value)
        {
            return new Operand(value);
        }

        public static Operand ConstF(float value)
        {
            return new Operand(value);
        }

        public static Operand ConstF(double value)
        {
            return new Operand(value);
        }

        public static Operand Label(int number)
        {
            return new Operand(OperandKind.Label, OperandType.None, (ulong)number);
        }

        public static Operand Local(OperandType type, int number)
        {
            return new Operand(OperandKind.LocalVariable, type, (ulong)number);
        }

        public static Operand Register(int index, RegisterType regType, OperandType type)
        {
            return new Operand(index, regType, type);
        }

        public static Operand Undef()
        {
            return new Operand(OperandKind.Undefined, OperandType.None);
        }

        public static Operand MemoryOp(
            OperandType type,
            Operand? baseAddress,
            Operand? index = null,
            Multiplier scale = Multiplier.x1,
            int displacement = 0)
        {
            return new Operand(type, new MemoryOperand(baseAddress, index, scale, displacement));
        }
    }
}
