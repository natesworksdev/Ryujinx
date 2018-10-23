using ChocolArm64.Decoder;
using ChocolArm64.Translation;
using System.Reflection.Emit;

namespace ChocolArm64.Instruction
{
    static partial class AInstEmit
    {
        private enum CselOperation
        {
            None,
            Increment,
            Invert,
            Negate
        }

        public static void Csel(AilEmitterCtx context)  => EmitCsel(context, CselOperation.None);
        public static void Csinc(AilEmitterCtx context) => EmitCsel(context, CselOperation.Increment);
        public static void Csinv(AilEmitterCtx context) => EmitCsel(context, CselOperation.Invert);
        public static void Csneg(AilEmitterCtx context) => EmitCsel(context, CselOperation.Negate);

        private static void EmitCsel(AilEmitterCtx context, CselOperation cselOp)
        {
            AOpCodeCsel op = (AOpCodeCsel)context.CurrOp;

            AilLabel lblTrue = new AilLabel();
            AilLabel lblEnd  = new AilLabel();

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