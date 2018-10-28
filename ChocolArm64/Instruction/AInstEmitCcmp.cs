using ChocolArm64.Decoder;
using ChocolArm64.State;
using ChocolArm64.Translation;
using System;
using System.Reflection.Emit;

using static ChocolArm64.Instruction.AInstEmitAluHelper;

namespace ChocolArm64.Instruction
{
    static partial class AInstEmit
    {
        private enum CcmpOp
        {
            Cmp,
            Cmn
        }

        public static void Ccmn(AilEmitterCtx context) => EmitCcmp(context, CcmpOp.Cmn);
        public static void Ccmp(AilEmitterCtx context) => EmitCcmp(context, CcmpOp.Cmp);

        private static void EmitCcmp(AilEmitterCtx context, CcmpOp cmpOp)
        {
            AOpCodeCcmp op = (AOpCodeCcmp)context.CurrOp;

            AilLabel lblTrue = new AilLabel();
            AilLabel lblEnd  = new AilLabel();

            context.EmitCondBranch(lblTrue, op.Cond);

            context.EmitLdc_I4((op.Nzcv >> 0) & 1);

            context.EmitStflg((int)ApState.VBit);

            context.EmitLdc_I4((op.Nzcv >> 1) & 1);

            context.EmitStflg((int)ApState.CBit);

            context.EmitLdc_I4((op.Nzcv >> 2) & 1);

            context.EmitStflg((int)ApState.ZBit);

            context.EmitLdc_I4((op.Nzcv >> 3) & 1);

            context.EmitStflg((int)ApState.NBit);

            context.Emit(OpCodes.Br_S, lblEnd);

            context.MarkLabel(lblTrue);

            EmitDataLoadOpers(context);

            if (cmpOp == CcmpOp.Cmp)
            {
                context.Emit(OpCodes.Sub);

                context.EmitZnFlagCheck();

                EmitSubsCCheck(context);
                EmitSubsVCheck(context);
            }
            else if (cmpOp == CcmpOp.Cmn)
            {
                context.Emit(OpCodes.Add);

                context.EmitZnFlagCheck();

                EmitAddsCCheck(context);
                EmitAddsVCheck(context);
            }
            else
            {
                throw new ArgumentException(nameof(cmpOp));
            }

            context.Emit(OpCodes.Pop);

            context.MarkLabel(lblEnd);
        }
    }
}