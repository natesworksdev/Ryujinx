using ChocolArm64.Decoder;
using ChocolArm64.Translation;
using System.Reflection.Emit;

namespace ChocolArm64.Instruction
{
    static partial class AInstEmit
    {
        public static void Movk(AilEmitterCtx context)
        {
            AOpCodeMov op = (AOpCodeMov)context.CurrOp;

            context.EmitLdintzr(op.Rd);
            context.EmitLdc_I(~(0xffffL << op.Pos));

            context.Emit(OpCodes.And);

            context.EmitLdc_I(op.Imm);

            context.Emit(OpCodes.Or);

            context.EmitStintzr(op.Rd);
        }

        public static void Movn(AilEmitterCtx context)
        {
            AOpCodeMov op = (AOpCodeMov)context.CurrOp;

            context.EmitLdc_I(~op.Imm);
            context.EmitStintzr(op.Rd);
        }

        public static void Movz(AilEmitterCtx context)
        {
            AOpCodeMov op = (AOpCodeMov)context.CurrOp;

            context.EmitLdc_I(op.Imm);
            context.EmitStintzr(op.Rd);
        }
    }
}