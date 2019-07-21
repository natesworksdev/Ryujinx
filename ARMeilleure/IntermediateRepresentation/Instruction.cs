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
        CompareAndSwap128,
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
        ConvertI64ToI32,
        ConvertToFP,
        ConvertToFPUI,
        Copy,
        CountLeadingZeros,
        Divide,
        DivideUI,
        Load,
        Load16,
        Load8,
        LoadArgument,
        Multiply,
        Multiply64HighSI,
        Multiply64HighUI,
        Negate,
        Return,
        RotateRight,
        ShiftLeft,
        ShiftRightSI,
        ShiftRightUI,
        SignExtend16,
        SignExtend32,
        SignExtend8,
        StackAlloc,
        Store,
        Store16,
        Store8,
        Subtract,
        VectorCreateScalar,
        VectorExtract,
        VectorExtract16,
        VectorExtract8,
        VectorInsert,
        VectorInsert16,
        VectorInsert8,
        VectorOne,
        VectorZero,
        VectorZeroUpper64,
        VectorZeroUpper96,
        ZeroExtend16,
        ZeroExtend32,
        ZeroExtend8,

        Extended,
        Fill,
        LoadFromContext,
        Spill,
        SpillArg,
        StoreToContext
    }

    static class InstructionExtensions
    {
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