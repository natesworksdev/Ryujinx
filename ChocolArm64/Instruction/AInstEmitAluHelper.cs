using ChocolArm64.Decoder;
using ChocolArm64.State;
using ChocolArm64.Translation;
using System.Reflection.Emit;

namespace ChocolArm64.Instruction
{
    static class AInstEmitAluHelper
    {
        public static void EmitAdcsCCheck(AilEmitterCtx context)
        {
            //C = (Rd == Rn && CIn) || Rd < Rn
            context.EmitSttmp();
            context.EmitLdtmp();
            context.EmitLdtmp();

            EmitDataLoadRn(context);

            context.Emit(OpCodes.Ceq);

            context.EmitLdflg((int)ApState.CBit);

            context.Emit(OpCodes.And);

            context.EmitLdtmp();

            EmitDataLoadRn(context);

            context.Emit(OpCodes.Clt_Un);
            context.Emit(OpCodes.Or);

            context.EmitStflg((int)ApState.CBit);
        }

        public static void EmitAddsCCheck(AilEmitterCtx context)
        {
            //C = Rd < Rn
            context.Emit(OpCodes.Dup);

            EmitDataLoadRn(context);

            context.Emit(OpCodes.Clt_Un);

            context.EmitStflg((int)ApState.CBit);
        }

        public static void EmitAddsVCheck(AilEmitterCtx context)
        {
            //V = (Rd ^ Rn) & ~(Rn ^ Rm) < 0
            context.Emit(OpCodes.Dup);

            EmitDataLoadRn(context);

            context.Emit(OpCodes.Xor);

            EmitDataLoadOpers(context);

            context.Emit(OpCodes.Xor);
            context.Emit(OpCodes.Not);
            context.Emit(OpCodes.And);

            context.EmitLdc_I(0);

            context.Emit(OpCodes.Clt);

            context.EmitStflg((int)ApState.VBit);
        }

        public static void EmitSbcsCCheck(AilEmitterCtx context)
        {
            //C = (Rn == Rm && CIn) || Rn > Rm
            EmitDataLoadOpers(context);

            context.Emit(OpCodes.Ceq);

            context.EmitLdflg((int)ApState.CBit);

            context.Emit(OpCodes.And);

            EmitDataLoadOpers(context);

            context.Emit(OpCodes.Cgt_Un);
            context.Emit(OpCodes.Or);

            context.EmitStflg((int)ApState.CBit);
        }

        public static void EmitSubsCCheck(AilEmitterCtx context)
        {
            //C = Rn == Rm || Rn > Rm = !(Rn < Rm)
            EmitDataLoadOpers(context);

            context.Emit(OpCodes.Clt_Un);

            context.EmitLdc_I4(1);

            context.Emit(OpCodes.Xor);

            context.EmitStflg((int)ApState.CBit);
        }

        public static void EmitSubsVCheck(AilEmitterCtx context)
        {
            //V = (Rd ^ Rn) & (Rn ^ Rm) < 0
            context.Emit(OpCodes.Dup);

            EmitDataLoadRn(context);

            context.Emit(OpCodes.Xor);

            EmitDataLoadOpers(context);

            context.Emit(OpCodes.Xor);
            context.Emit(OpCodes.And);

            context.EmitLdc_I(0);

            context.Emit(OpCodes.Clt);

            context.EmitStflg((int)ApState.VBit);
        }

        public static void EmitDataLoadRm(AilEmitterCtx context)
        {
            context.EmitLdintzr(((IaOpCodeAluRs)context.CurrOp).Rm);
        }

        public static void EmitDataLoadOpers(AilEmitterCtx context)
        {
            EmitDataLoadRn(context);
            EmitDataLoadOper2(context);
        }

        public static void EmitDataLoadRn(AilEmitterCtx context)
        {
            IaOpCodeAlu op = (IaOpCodeAlu)context.CurrOp;

            if (op.DataOp == ADataOp.Logical || op is IaOpCodeAluRs)
            {
                context.EmitLdintzr(op.Rn);
            }
            else
            {
                context.EmitLdint(op.Rn);
            }
        }

        public static void EmitDataLoadOper2(AilEmitterCtx context)
        {
            switch (context.CurrOp)
            {
                case IaOpCodeAluImm op:
                    context.EmitLdc_I(op.Imm);
                    break;

                case IaOpCodeAluRs op:
                    context.EmitLdintzr(op.Rm);

                    switch (op.ShiftType)
                    {
                        case AShiftType.Lsl: context.EmitLsl(op.Shift); break;
                        case AShiftType.Lsr: context.EmitLsr(op.Shift); break;
                        case AShiftType.Asr: context.EmitAsr(op.Shift); break;
                        case AShiftType.Ror: context.EmitRor(op.Shift); break;
                    }
                    break;

                case IaOpCodeAluRx op:
                    context.EmitLdintzr(op.Rm);
                    context.EmitCast(op.IntType);
                    context.EmitLsl(op.Shift);
                    break;
            }
        }

        public static void EmitDataStore(AilEmitterCtx context)  => EmitDataStore(context, false);
        public static void EmitDataStoreS(AilEmitterCtx context) => EmitDataStore(context, true);

        public static void EmitDataStore(AilEmitterCtx context, bool setFlags)
        {
            IaOpCodeAlu op = (IaOpCodeAlu)context.CurrOp;

            if (setFlags || op is IaOpCodeAluRs)
            {
                context.EmitStintzr(op.Rd);
            }
            else
            {
                context.EmitStint(op.Rd);
            }
        }

        public static void EmitSetNzcv(AilEmitterCtx context, int nzcv)
        {
            context.EmitLdc_I4((nzcv >> 0) & 1);

            context.EmitStflg((int)ApState.VBit);

            context.EmitLdc_I4((nzcv >> 1) & 1);

            context.EmitStflg((int)ApState.CBit);

            context.EmitLdc_I4((nzcv >> 2) & 1);

            context.EmitStflg((int)ApState.ZBit);

            context.EmitLdc_I4((nzcv >> 3) & 1);

            context.EmitStflg((int)ApState.NBit);
        }
    }
}