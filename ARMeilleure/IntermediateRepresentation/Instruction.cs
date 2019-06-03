namespace ARMeilleure.IntermediateRepresentation
{
    enum Instruction
    {
        Add,
        BitwiseAnd,
        BitwiseExclusiveOr,
        BitwiseNot,
        BitwiseOr,
        Branch,
        BranchIfFalse,
        BranchIfTrue,
        ByteSwap,
        Call,
        CompareEqual,
        CompareGreater,
        CompareGreaterOrEqual,
        CompareGreaterOrEqualUI,
        CompareGreaterUI,
        CompareLess,
        CompareLessOrEqual,
        CompareLessOrEqualUI,
        CompareLessUI,
        CompareNotEqual,
        ConditionalSelect,
        Copy,
        CountLeadingZeros,
        Divide,
        DivideUI,
        Fill,
        Load,
        LoadFromContext,
        LoadSx16,
        LoadSx32,
        LoadSx8,
        LoadZx16,
        LoadZx8,
        Multiply,
        Multiply64HighSI,
        Multiply64HighUI,
        Negate,
        Return,
        RotateRight,
        ShiftLeft,
        ShiftRightSI,
        ShiftRightUI,
        SignExtend8,
        SignExtend16,
        SignExtend32,
        Spill,
        SpillArg,
        Store,
        Store16,
        Store8,
        StoreToContext,
        Subtract,

        Count
    }

    static class InstructionExtensions
    {
        public static bool IsComparison(this Instruction inst)
        {
            switch (inst)
            {
                case Instruction.CompareEqual:
                case Instruction.CompareGreater:
                case Instruction.CompareGreaterOrEqual:
                case Instruction.CompareGreaterOrEqualUI:
                case Instruction.CompareGreaterUI:
                case Instruction.CompareLess:
                case Instruction.CompareLessOrEqual:
                case Instruction.CompareLessOrEqualUI:
                case Instruction.CompareLessUI:
                case Instruction.CompareNotEqual:
                    return true;
            }

            return false;
        }

        public static bool IsMemory(this Instruction inst)
        {
            switch (inst)
            {
                case Instruction.Load:
                case Instruction.LoadSx16:
                case Instruction.LoadSx32:
                case Instruction.LoadSx8:
                case Instruction.LoadZx16:
                case Instruction.LoadZx8:
                case Instruction.Store:
                case Instruction.Store16:
                case Instruction.Store8:
                    return true;
            }

            return false;
        }

        public static bool IsShift(this Instruction inst)
        {
            switch (inst)
            {
                case Instruction.RotateRight:
                case Instruction.ShiftLeft:
                case Instruction.ShiftRightSI:
                case Instruction.ShiftRightUI:
                    return true;
            }

            return false;
        }
    }
}