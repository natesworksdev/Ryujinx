using ARMeilleure.IntermediateRepresentation;
using System;

using static ARMeilleure.IntermediateRepresentation.OperandHelper;

namespace ARMeilleure.CodeGen.Optimizations
{
    static class Simplification
    {
        public static Operand? RunPass(Operation operation)
        {
            return operation.Instruction switch
            {
                Instruction.Add => TryEliminateBinaryOpComutative(operation, 0),
                Instruction.BitwiseAnd => TryEliminateBitwiseAnd(operation),
                Instruction.BitwiseOr => TryEliminateBitwiseOr(operation),
                Instruction.BitwiseExclusiveOr => TryEliminateBitwiseExclusiveOr(operation),
                Instruction.ConditionalSelect => TryEliminateConditionalSelect(operation),
                Instruction.Divide => TryEliminateBinaryOpY(operation, 1),
                Instruction.Multiply => TryEliminateBinaryOpComutative(operation, 1),
                Instruction.ShiftLeft or Instruction.ShiftRightSI or Instruction.ShiftRightUI or Instruction.Subtract => TryEliminateBinaryOpY(operation, 0),
                _ => null
            };
        }

        private static Operand? TryEliminateBitwiseAnd(Operation operation)
        {
            // Try to recognize and optimize those 3 patterns (in order):
            // x & 0xFFFFFFFF == x,          0xFFFFFFFF & y == y,
            // x & 0x00000000 == 0x00000000, 0x00000000 & y == 0x00000000
            Operand x = operation.GetSource(0);
            Operand y = operation.GetSource(1);

            if (IsConstEqual(x, AllOnes(x.Type)))
            {
                return y;
            }
            else if (IsConstEqual(y, AllOnes(y.Type)))
            {
                return x;
            }
            else if (IsConstEqual(x, 0) || IsConstEqual(y, 0))
            {
                return Const(0);
            }

            return null;
        }

        private static Operand? TryEliminateBitwiseOr(Operation operation)
        {
            // Try to recognize and optimize those 3 patterns (in order):
            // x | 0x00000000 == x,          0x00000000 | y == y,
            // x | 0xFFFFFFFF == 0xFFFFFFFF, 0xFFFFFFFF | y == 0xFFFFFFFF
            Operand x = operation.GetSource(0);
            Operand y = operation.GetSource(1);

            if (IsConstEqual(x, 0))
            {
                return y;
            }
            else if (IsConstEqual(y, 0))
            {
                return x;
            }
            else if (IsConstEqual(x, AllOnes(x.Type)) || IsConstEqual(y, AllOnes(y.Type)))
            {
                return Const(AllOnes(x.Type));
            }

            return null;
        }

        private static Operand? TryEliminateBitwiseExclusiveOr(Operation operation)
        {
            // Try to recognize and optimize those 2 patterns (in order):
            // x ^ y == 0x00000000 when x == y
            // 0x00000000 ^ y == y, x ^ 0x00000000 == x
            Operand x = operation.GetSource(0);
            Operand y = operation.GetSource(1);

            if (x == y && x.Type.IsInteger())
            {
                return Const(x.Type, 0);
            }
            else
            {
                return TryEliminateBinaryOpComutative(operation, 0);
            }
        }

        private static Operand? TryEliminateBinaryOpY(Operation operation, ulong comparand)
        {
            Operand x = operation.GetSource(0);
            Operand y = operation.GetSource(1);

            if (IsConstEqual(y, comparand))
            {
                return x;
            }

            return null;
        }

        private static Operand? TryEliminateBinaryOpComutative(Operation operation, ulong comparand)
        {
            Operand x = operation.GetSource(0);
            Operand y = operation.GetSource(1);

            if (IsConstEqual(x, comparand))
            {
                return y;
            }
            else if (IsConstEqual(y, comparand))
            {
                return x;
            }

            return null;
        }

        private static Operand? TryEliminateConditionalSelect(Operation operation)
        {
            Operand cond = operation.GetSource(0);

            if (cond.Kind != OperandKind.Constant)
            {
                return null;
            }

            // The condition is constant, we can turn it into a copy, and select
            // the source based on the condition value.
            int srcIndex = cond.Value != 0 ? 1 : 2;

            Operand source = operation.GetSource(srcIndex);

            return source;
        }

        private static bool IsConstEqual(Operand operand, ulong comparand)
        {
            if (operand.Kind != OperandKind.Constant || !operand.Type.IsInteger())
            {
                return false;
            }

            return operand.Value == comparand;
        }

        private static ulong AllOnes(OperandType type)
        {
            switch (type)
            {
                case OperandType.I32: return ~0U;
                case OperandType.I64: return ~0UL;
            }

            throw new ArgumentException("Invalid operand type \"" + type + "\".");
        }
    }
}