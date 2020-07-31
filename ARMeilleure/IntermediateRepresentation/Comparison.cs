using ARMeilleure.CodeGen.X86;
using System;

namespace ARMeilleure.IntermediateRepresentation
{
    enum Comparison
    {
        // ** Order of these enum matters **
        // See ComparisonExtensions.ConditionToX86Condition.

        Equal,
        Greater,
        GreaterOrEqual,
        GreaterOrEqualUI,
        GreaterUI,
        Less,
        LessOrEqual,
        LessOrEqualUI,
        LessUI,
        NotEqual
    }

    static class ComparisonExtensions
    {
        private static ReadOnlySpan<X86Condition> ComparisonToX86Condition => new X86Condition[]
        {
            X86Condition.Equal,
            X86Condition.Greater,
            X86Condition.GreaterOrEqual,
            X86Condition.AboveOrEqual,
            X86Condition.Above,
            X86Condition.Less,
            X86Condition.LessOrEqual,
            X86Condition.BelowOrEqual,
            X86Condition.Below,
            X86Condition.NotEqual
        };

        public static X86Condition ToX86Condition(this Comparison comp)
        {
            return ComparisonToX86Condition[(int)comp];
        }
    }
}
