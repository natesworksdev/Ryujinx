using ARMeilleure.Decoders;
using ARMeilleure.IntermediateRepresentation;
using ARMeilleure.State;
using ARMeilleure.Translation;

using static ARMeilleure.Instructions.InstEmitHelper;
using static ARMeilleure.Instructions.InstEmitAluHelper;
using static ARMeilleure.IntermediateRepresentation.OperandHelper;

namespace ARMeilleure.Instructions
{
    static partial class InstEmit32
    {
        public static void Umull(ArmEmitterContext context)
        {
            OpCode32AluUmull op = (OpCode32AluUmull)context.CurrOp;

            Operand n = context.ZeroExtend32(OperandType.I64, GetIntA32(context, op.Rn));
            Operand m = context.ZeroExtend32(OperandType.I64, GetIntA32(context, op.Rm));

            Operand res = context.Multiply(n, m);

            Operand hi = context.ConvertI64ToI32(context.ShiftRightUI(res, Const(32)));
            Operand lo = context.ConvertI64ToI32(res);

            if (op.SetFlags)
            {
                EmitNZFlagsCheck(context, res);
            }

            EmitGenericStore(context, op.RdHi, op.SetFlags, hi);
            EmitGenericStore(context, op.RdLo, op.SetFlags, lo);
        }

        private static void EmitGenericStore(ArmEmitterContext context, int Rd, bool setFlags, Operand value)
        {
            if (Rd == RegisterAlias.Aarch32Pc)
            {
                if (setFlags)
                {
                    // TODO: Load SPSR etc.
                    Operand isThumb = GetFlag(PState.TFlag);

                    Operand lblThumb = Label();

                    context.BranchIfTrue(lblThumb, isThumb);

                    context.Return(context.ZeroExtend32(OperandType.I64, context.BitwiseAnd(value, Const(~3))));

                    context.MarkLabel(lblThumb);

                    context.Return(context.ZeroExtend32(OperandType.I64, context.BitwiseAnd(value, Const(~1))));
                }
                else
                {
                    EmitAluWritePc(context, value);
                }
            }
            else
            {
                SetIntA32(context, Rd, value);
            }
        }
    }
}
