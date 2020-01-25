using ARMeilleure.Decoders;
using ARMeilleure.IntermediateRepresentation;
using ARMeilleure.Translation;

using static ARMeilleure.Instructions.InstEmitHelper;
using static ARMeilleure.Instructions.InstEmitSimdHelper;
using static ARMeilleure.Instructions.InstEmitSimdHelper32;
using static ARMeilleure.IntermediateRepresentation.OperandHelper;
using System.Diagnostics;

namespace ARMeilleure.Instructions
{
    static partial class InstEmit32
    {
        public static void Vshl(ArmEmitterContext context)
        {
            OpCode32SimdShift op = (OpCode32SimdShift)context.CurrOp;

            EmitVectorUnaryOpZx32(context, (op1) => context.ShiftLeft(op1, Const(op1.Type, op.Shift)));
        }

        public static void Vshl_I(ArmEmitterContext context)
        {
            OpCode32SimdReg op = (OpCode32SimdReg)context.CurrOp;

            if (op.U)
            {
                EmitVectorBinaryOpZx32(context, (op1, op2) => EmitShlRegOp(context, op2, op1, op.Size, true));
            }
            else
            {
                EmitVectorBinaryOpSx32(context, (op1, op2) => EmitShlRegOp(context, op2, op1, op.Size, false));
            }
        }

        private static Operand EmitShlRegOp(ArmEmitterContext context, Operand op, Operand shiftLsB, int size, bool unsigned)
        {
            if (shiftLsB.Type == OperandType.I64) shiftLsB = context.ConvertI64ToI32(shiftLsB);
            shiftLsB = context.SignExtend8(OperandType.I32, shiftLsB);
            Debug.Assert((uint)size < 4u);

            Operand negShiftLsB = context.Negate(shiftLsB);

            Operand isPositive = context.ICompareGreaterOrEqual(shiftLsB, Const(0));

            Operand shl = context.ShiftLeft(op, shiftLsB);
            Operand shr = (unsigned) ? context.ShiftRightUI(op, negShiftLsB) : context.ShiftRightSI(op, negShiftLsB);

            Operand res = context.ConditionalSelect(isPositive, shl, shr);

            if (unsigned)
            {
                Operand isOutOfRange = context.BitwiseOr(
                    context.ICompareGreaterOrEqual(shiftLsB, Const(8 << size)),
                    context.ICompareGreaterOrEqual(negShiftLsB, Const(8 << size)));

                return context.ConditionalSelect(isOutOfRange, Const(op.Type, 0), res);
            }
            else
            {
                Operand isOutOfRange0 = context.ICompareGreaterOrEqual(shiftLsB, Const(8 << size));
                Operand isOutOfRangeN = context.ICompareGreaterOrEqual(negShiftLsB, Const(8 << size));

                // Also zero if shift is too negative, but value was positive.
                isOutOfRange0 = context.BitwiseOr(isOutOfRange0, context.BitwiseAnd(isOutOfRangeN, context.ICompareGreaterOrEqual(op, Const(op.Type, 0))));

                Operand min = (op.Type == OperandType.I64) ? Const(-1L) : Const(-1);

                return context.ConditionalSelect(isOutOfRange0, Const(op.Type, 0), context.ConditionalSelect(isOutOfRangeN, min, res));
            }
        }
    }
}
