using ChocolArm64.Decoder;
using ChocolArm64.State;
using ChocolArm64.Translation;
using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Intrinsics.X86;

using static ChocolArm64.Instruction.AInstEmitAluHelper;

namespace ChocolArm64.Instruction
{
    internal static partial class AInstEmit
    {
        public static void Adc(AILEmitterCtx context)
        {
            EmitAdc(context, false);
        }

        public static void Adcs(AILEmitterCtx context)
        {
            EmitAdc(context, true);
        }

        private static void EmitAdc(AILEmitterCtx context, bool setFlags)
        {
            EmitDataLoadOpers(context);

            context.Emit(OpCodes.Add);

            context.EmitLdflg((int)APState.CBit);

            Type[] mthdTypes  = new Type[] { typeof(bool) };

            MethodInfo mthdInfo = typeof(Convert).GetMethod(nameof(Convert.ToInt32), mthdTypes);

            context.EmitCall(mthdInfo);

            if (context.CurrOp.RegisterSize != ARegisterSize.Int32)
            {
                context.Emit(OpCodes.Conv_U8);
            }

            context.Emit(OpCodes.Add);

            if (setFlags)
            {
                context.EmitZnFlagCheck();

                EmitAdcsCCheck(context);
                EmitAddsVCheck(context);
            }

            EmitDataStore(context);
        }

        public static void Add(AILEmitterCtx context)
        {
            EmitDataOp(context, OpCodes.Add);
        }

        public static void Adds(AILEmitterCtx context)
        {
            EmitDataLoadOpers(context);

            context.Emit(OpCodes.Add);

            context.EmitZnFlagCheck();

            EmitAddsCCheck(context);
            EmitAddsVCheck(context);
            EmitDataStoreS(context);
        }

        public static void And(AILEmitterCtx context)
        {
            EmitDataOp(context, OpCodes.And);
        }

        public static void Ands(AILEmitterCtx context)
        {
            EmitDataLoadOpers(context);

            context.Emit(OpCodes.And);

            EmitZeroCvFlags(context);

            context.EmitZnFlagCheck();

            EmitDataStoreS(context);
        }

        public static void Asrv(AILEmitterCtx context)
        {
            EmitDataOpShift(context, OpCodes.Shr);
        }

        public static void Bic(AILEmitterCtx context)
        {
            EmitBic(context, false);
        }

        public static void Bics(AILEmitterCtx context)
        {
            EmitBic(context, true);
        }

        private static void EmitBic(AILEmitterCtx context, bool setFlags)
        {
            EmitDataLoadOpers(context);

            context.Emit(OpCodes.Not);
            context.Emit(OpCodes.And);

            if (setFlags)
            {
                EmitZeroCvFlags(context);

                context.EmitZnFlagCheck();
            }

            EmitDataStore(context, setFlags);
        }

        public static void Cls(AILEmitterCtx context)
        {
            AOpCodeAlu op = (AOpCodeAlu)context.CurrOp;

            context.EmitLdintzr(op.Rn);

            context.EmitLdc_I4(op.RegisterSize == ARegisterSize.Int32 ? 32 : 64);

            ASoftFallback.EmitCall(context, nameof(ASoftFallback.CountLeadingSigns));

            context.EmitStintzr(op.Rd);
        }

        public static void Clz(AILEmitterCtx context)
        {
            AOpCodeAlu op = (AOpCodeAlu)context.CurrOp;

            context.EmitLdintzr(op.Rn);

            if (Lzcnt.IsSupported)
            {
                Type value = op.RegisterSize == ARegisterSize.Int32 ? typeof(uint) : typeof(ulong);

                context.EmitCall(typeof(Lzcnt).GetMethod(nameof(Lzcnt.LeadingZeroCount), new Type[] { value }));
            }
            else
            {
                context.EmitLdc_I4(op.RegisterSize == ARegisterSize.Int32 ? 32 : 64);

                ASoftFallback.EmitCall(context, nameof(ASoftFallback.CountLeadingZeros));
            }

            context.EmitStintzr(op.Rd);
        }

        public static void Eon(AILEmitterCtx context)
        {
            EmitDataLoadOpers(context);

            context.Emit(OpCodes.Not);
            context.Emit(OpCodes.Xor);

            EmitDataStore(context);
        }

        public static void Eor(AILEmitterCtx context)
        {
            EmitDataOp(context, OpCodes.Xor);
        }

        public static void Extr(AILEmitterCtx context)
        {
            //TODO: Ensure that the Shift is valid for the Is64Bits.
            AOpCodeAluRs op = (AOpCodeAluRs)context.CurrOp;

            context.EmitLdintzr(op.Rm);

            if (op.Shift > 0)
            {
                context.EmitLdc_I4(op.Shift);

                context.Emit(OpCodes.Shr_Un);

                context.EmitLdintzr(op.Rn);
                context.EmitLdc_I4(op.GetBitsCount() - op.Shift);

                context.Emit(OpCodes.Shl);
                context.Emit(OpCodes.Or);
            }

            EmitDataStore(context);
        }

        public static void Lslv(AILEmitterCtx context)
        {
            EmitDataOpShift(context, OpCodes.Shl);
        }

        public static void Lsrv(AILEmitterCtx context)
        {
            EmitDataOpShift(context, OpCodes.Shr_Un);
        }

        public static void Sbc(AILEmitterCtx context)
        {
            EmitSbc(context, false);
        }

        public static void Sbcs(AILEmitterCtx context)
        {
            EmitSbc(context, true);
        }

        private static void EmitSbc(AILEmitterCtx context, bool setFlags)
        {
            EmitDataLoadOpers(context);

            context.Emit(OpCodes.Sub);

            context.EmitLdflg((int)APState.CBit);

            Type[] mthdTypes  = new Type[] { typeof(bool) };

            MethodInfo mthdInfo = typeof(Convert).GetMethod(nameof(Convert.ToInt32), mthdTypes);

            context.EmitCall(mthdInfo);

            context.EmitLdc_I4(1);

            context.Emit(OpCodes.Xor);

            if (context.CurrOp.RegisterSize != ARegisterSize.Int32)
            {
                context.Emit(OpCodes.Conv_U8);
            }

            context.Emit(OpCodes.Sub);

            if (setFlags)
            {
                context.EmitZnFlagCheck();

                EmitSbcsCCheck(context);
                EmitSubsVCheck(context);
            }

            EmitDataStore(context);
        }

        public static void Sub(AILEmitterCtx context)
        {
            EmitDataOp(context, OpCodes.Sub);
        }

        public static void Subs(AILEmitterCtx context)
        {
            context.TryOptMarkCondWithoutCmp();

            EmitDataLoadOpers(context);

            context.Emit(OpCodes.Sub);

            context.EmitZnFlagCheck();

            EmitSubsCCheck(context);
            EmitSubsVCheck(context);
            EmitDataStoreS(context);
        }

        public static void Orn(AILEmitterCtx context)
        {
            EmitDataLoadOpers(context);

            context.Emit(OpCodes.Not);
            context.Emit(OpCodes.Or);

            EmitDataStore(context);
        }

        public static void Orr(AILEmitterCtx context)
        {
            EmitDataOp(context, OpCodes.Or);
        }

        public static void Rbit(AILEmitterCtx context)
        {
            EmitFallback32_64(context,
                nameof(ASoftFallback.ReverseBits32),
                nameof(ASoftFallback.ReverseBits64));
        }

        public static void Rev16(AILEmitterCtx context)
        {
            EmitFallback32_64(context,
                nameof(ASoftFallback.ReverseBytes16_32),
                nameof(ASoftFallback.ReverseBytes16_64));
        }

        public static void Rev32(AILEmitterCtx context)
        {
            EmitFallback32_64(context,
                nameof(ASoftFallback.ReverseBytes32_32),
                nameof(ASoftFallback.ReverseBytes32_64));
        }

        private static void EmitFallback32_64(AILEmitterCtx context, string name32, string name64)
        {
            AOpCodeAlu op = (AOpCodeAlu)context.CurrOp;

            context.EmitLdintzr(op.Rn);

            if (op.RegisterSize == ARegisterSize.Int32)
            {
                ASoftFallback.EmitCall(context, name32);
            }
            else
            {
                ASoftFallback.EmitCall(context, name64);
            }

            context.EmitStintzr(op.Rd);
        }

        public static void Rev64(AILEmitterCtx context)
        {
            AOpCodeAlu op = (AOpCodeAlu)context.CurrOp;

            context.EmitLdintzr(op.Rn);

            ASoftFallback.EmitCall(context, nameof(ASoftFallback.ReverseBytes64));

            context.EmitStintzr(op.Rd);
        }

        public static void Rorv(AILEmitterCtx context)
        {
            EmitDataLoadRn(context);
            EmitDataLoadShift(context);

            context.Emit(OpCodes.Shr_Un);

            EmitDataLoadRn(context);

            context.EmitLdc_I4(context.CurrOp.GetBitsCount());

            EmitDataLoadShift(context);

            context.Emit(OpCodes.Sub);
            context.Emit(OpCodes.Shl);
            context.Emit(OpCodes.Or);

            EmitDataStore(context);
        }

        public static void Sdiv(AILEmitterCtx context)
        {
            EmitDiv(context, OpCodes.Div);
        }

        public static void Udiv(AILEmitterCtx context)
        {
            EmitDiv(context, OpCodes.Div_Un);
        }

        private static void EmitDiv(AILEmitterCtx context, OpCode ilOp)
        {
            //If Rm == 0, Rd = 0 (division by zero).
            context.EmitLdc_I(0);

            EmitDataLoadRm(context);

            context.EmitLdc_I(0);

            AILLabel badDiv = new AILLabel();

            context.Emit(OpCodes.Beq_S, badDiv);
            context.Emit(OpCodes.Pop);

            if (ilOp == OpCodes.Div)
            {
                //If Rn == INT_MIN && Rm == -1, Rd = INT_MIN (overflow).
                long intMin = 1L << (context.CurrOp.GetBitsCount() - 1);

                context.EmitLdc_I(intMin);

                EmitDataLoadRn(context);

                context.EmitLdc_I(intMin);

                context.Emit(OpCodes.Ceq);

                EmitDataLoadRm(context);

                context.EmitLdc_I(-1);

                context.Emit(OpCodes.Ceq);
                context.Emit(OpCodes.And);
                context.Emit(OpCodes.Brtrue_S, badDiv);
                context.Emit(OpCodes.Pop);
            }

            EmitDataLoadRn(context);
            EmitDataLoadRm(context);

            context.Emit(ilOp);

            context.MarkLabel(badDiv);

            EmitDataStore(context);
        }

        private static void EmitDataOp(AILEmitterCtx context, OpCode ilOp)
        {
            EmitDataLoadOpers(context);

            context.Emit(ilOp);

            EmitDataStore(context);
        }

        private static void EmitDataOpShift(AILEmitterCtx context, OpCode ilOp)
        {
            EmitDataLoadRn(context);
            EmitDataLoadShift(context);

            context.Emit(ilOp);

            EmitDataStore(context);
        }

        private static void EmitDataLoadShift(AILEmitterCtx context)
        {
            EmitDataLoadRm(context);

            context.EmitLdc_I(context.CurrOp.GetBitsCount() - 1);

            context.Emit(OpCodes.And);

            //Note: Only 32-bits shift values are valid, so when the value is 64-bits
            //we need to cast it to a 32-bits integer. This is fine because we
            //AND the value and only keep the lower 5 or 6 bits anyway -- it
            //could very well fit on a byte.
            if (context.CurrOp.RegisterSize != ARegisterSize.Int32)
            {
                context.Emit(OpCodes.Conv_I4);
            }
        }

        private static void EmitZeroCvFlags(AILEmitterCtx context)
        {
            context.EmitLdc_I4(0);

            context.EmitStflg((int)APState.VBit);

            context.EmitLdc_I4(0);

            context.EmitStflg((int)APState.CBit);
        }
    }
}
