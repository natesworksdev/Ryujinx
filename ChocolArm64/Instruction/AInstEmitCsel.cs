using ChocolArm64.Decoder;
using ChocolArm64.Translation;
using System.Reflection.Emit;

namespace ChocolArm64.Instruction
{
    internal static partial class AInstEmit
    {
        private enum CselOperation
        {
            None,
            Increment,
            Invert,
            Negate
        }

        public static void Csel(AILEmitterCtx context)
        {
            EmitCsel(context, CselOperation.None);
        }

        public static void Csinc(AILEmitterCtx context)
        {
            EmitCsel(context, CselOperation.Increment);
        }

        public static void Csinv(AILEmitterCtx context)
        {
            EmitCsel(context, CselOperation.Invert);
        }

        public static void Csneg(AILEmitterCtx context)
        {
            EmitCsel(context, CselOperation.Negate);
        }

        private static void EmitCsel(AILEmitterCtx context, CselOperation cselOp)
        {
            AOpCodeCsel op = (AOpCodeCsel)context.CurrOp;

            AILLabel lblTrue = new AILLabel();
            AILLabel lblEnd  = new AILLabel();

            context.EmitCondBranch(lblTrue, op.Cond);
            context.EmitLdintzr(op.Rm);

            if (cselOp == CselOperation.Increment)
            {
                context.EmitLdc_I(1);

                context.Emit(OpCodes.Add);
            }
            else if (cselOp == CselOperation.Invert)
            {
                context.Emit(OpCodes.Not);
            }
            else if (cselOp == CselOperation.Negate)
            {
                context.Emit(OpCodes.Neg);
            }

            context.Emit(OpCodes.Br_S, lblEnd);

            context.MarkLabel(lblTrue);

            context.EmitLdintzr(op.Rn);

            context.MarkLabel(lblEnd);

            context.EmitStintzr(op.Rd);
        }
    }
}