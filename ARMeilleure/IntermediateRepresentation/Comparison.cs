using ARMeilleure.CodeGen.X86;
using System;

namespace ARMeilleure.IntermediateRepresentation
{
    enum Comparison
    {
        Equal             = 0,
        NotEqual          = 1,
        Greater           = 2,
        LessOrEqual       = 3,
        GreaterUI         = 4,
        LessOrEqualUI     = 5,
        GreaterOrEqual    = 6,
        Less              = 7,
        GreaterOrEqualUI  = 8,
        LessUI            = 9
    }

    static class ComparisonExtensions
    {
        public static X86Condition ToX86Condition(this Comparison comp)
        {
            return comp switch
            {
                Comparison.Equal            => X86Condition.Equal,
                Comparison.NotEqual         => X86Condition.NotEqual,
                Comparison.Greater          => X86Condition.Greater,
                Comparison.LessOrEqual      => X86Condition.LessOrEqual,
                Comparison.GreaterUI        => X86Condition.Above,
                Comparison.LessOrEqualUI    => X86Condition.BelowOrEqual,
                Comparison.GreaterOrEqual   => X86Condition.GreaterOrEqual,
                Comparison.Less             => X86Condition.Less,
                Comparison.GreaterOrEqualUI => X86Condition.AboveOrEqual,
                Comparison.LessUI           => X86Condition.Below,

                _ => throw new ArgumentException(),
            };
        }

        public static Comparison Invert(this Comparison comp)
        {
            return (Comparison)((int)comp ^ 1);
        }
    }
}
