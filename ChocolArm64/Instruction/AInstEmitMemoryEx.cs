using ChocolArm64.Decoder;
using ChocolArm64.Memory;
using ChocolArm64.State;
using ChocolArm64.Translation;
using System;
using System.Reflection.Emit;
using System.Threading;

using static ChocolArm64.Instruction.AInstEmitMemoryHelper;

namespace ChocolArm64.Instruction
{
    static partial class AInstEmit
    {
        [Flags]
        private enum AccessType
        {
            None      = 0,
            Ordered   = 1,
            Exclusive = 2,
            OrderedEx = Ordered | Exclusive
        }

        public static void Clrex(AilEmitterCtx context)
        {
            EmitMemoryCall(context, nameof(AMemory.ClearExclusive));
        }

        public static void Dmb(AilEmitterCtx context) => EmitBarrier(context);
        public static void Dsb(AilEmitterCtx context) => EmitBarrier(context);

        public static void Ldar(AilEmitterCtx context)  => EmitLdr(context, AccessType.Ordered);
        public static void Ldaxr(AilEmitterCtx context) => EmitLdr(context, AccessType.OrderedEx);
        public static void Ldxr(AilEmitterCtx context)  => EmitLdr(context, AccessType.Exclusive);
        public static void Ldxp(AilEmitterCtx context)  => EmitLdp(context, AccessType.Exclusive);
        public static void Ldaxp(AilEmitterCtx context) => EmitLdp(context, AccessType.OrderedEx);

        private static void EmitLdr(AilEmitterCtx context, AccessType accType)
        {
            EmitLoad(context, accType, false);
        }

        private static void EmitLdp(AilEmitterCtx context, AccessType accType)
        {
            EmitLoad(context, accType, true);
        }

        private static void EmitLoad(AilEmitterCtx context, AccessType accType, bool pair)
        {
            AOpCodeMemEx op = (AOpCodeMemEx)context.CurrOp;

            bool ordered   = (accType & AccessType.Ordered)   != 0;
            bool exclusive = (accType & AccessType.Exclusive) != 0;

            if (ordered)
            {
                EmitBarrier(context);
            }

            if (exclusive)
            {
                EmitMemoryCall(context, nameof(AMemory.SetExclusive), op.Rn);
            }

            context.EmitLdint(op.Rn);
            context.EmitSttmp();

            context.EmitLdarg(ATranslatedSub.MemoryArgIdx);
            context.EmitLdtmp();

            EmitReadZxCall(context, op.Size);

            context.EmitStintzr(op.Rt);

            if (pair)
            {
                context.EmitLdarg(ATranslatedSub.MemoryArgIdx);
                context.EmitLdtmp();
                context.EmitLdc_I8(1 << op.Size);

                context.Emit(OpCodes.Add);

                EmitReadZxCall(context, op.Size);

                context.EmitStintzr(op.Rt2);
            }
        }

        public static void Pfrm(AilEmitterCtx context)
        {
            //Memory Prefetch, execute as no-op.
        }

        public static void Stlr(AilEmitterCtx context)  => EmitStr(context, AccessType.Ordered);
        public static void Stlxr(AilEmitterCtx context) => EmitStr(context, AccessType.OrderedEx);
        public static void Stxr(AilEmitterCtx context)  => EmitStr(context, AccessType.Exclusive);
        public static void Stxp(AilEmitterCtx context)  => EmitStp(context, AccessType.Exclusive);
        public static void Stlxp(AilEmitterCtx context) => EmitStp(context, AccessType.OrderedEx);

        private static void EmitStr(AilEmitterCtx context, AccessType accType)
        {
            EmitStore(context, accType, false);
        }

        private static void EmitStp(AilEmitterCtx context, AccessType accType)
        {
            EmitStore(context, accType, true);
        }

        private static void EmitStore(AilEmitterCtx context, AccessType accType, bool pair)
        {
            AOpCodeMemEx op = (AOpCodeMemEx)context.CurrOp;

            bool ordered   = (accType & AccessType.Ordered)   != 0;
            bool exclusive = (accType & AccessType.Exclusive) != 0;

            if (ordered)
            {
                EmitBarrier(context);
            }

            AilLabel lblEx  = new AilLabel();
            AilLabel lblEnd = new AilLabel();

            if (exclusive)
            {
                EmitMemoryCall(context, nameof(AMemory.TestExclusive), op.Rn);

                context.Emit(OpCodes.Brtrue_S, lblEx);

                context.EmitLdc_I8(1);
                context.EmitStintzr(op.Rs);

                context.Emit(OpCodes.Br_S, lblEnd);
            }

            context.MarkLabel(lblEx);

            context.EmitLdarg(ATranslatedSub.MemoryArgIdx);
            context.EmitLdint(op.Rn);
            context.EmitLdintzr(op.Rt);

            EmitWriteCall(context, op.Size);

            if (pair)
            {
                context.EmitLdarg(ATranslatedSub.MemoryArgIdx);
                context.EmitLdint(op.Rn);
                context.EmitLdc_I8(1 << op.Size);

                context.Emit(OpCodes.Add);

                context.EmitLdintzr(op.Rt2);

                EmitWriteCall(context, op.Size);
            }

            if (exclusive)
            {
                context.EmitLdc_I8(0);
                context.EmitStintzr(op.Rs);

                EmitMemoryCall(context, nameof(AMemory.ClearExclusiveForStore));
            }

            context.MarkLabel(lblEnd);
        }

        private static void EmitMemoryCall(AilEmitterCtx context, string name, int rn = -1)
        {
            context.EmitLdarg(ATranslatedSub.MemoryArgIdx);
            context.EmitLdarg(ATranslatedSub.StateArgIdx);

            context.EmitCallPropGet(typeof(AThreadState), nameof(AThreadState.Core));

            if (rn != -1)
            {
                context.EmitLdint(rn);
            }

            context.EmitCall(typeof(AMemory), name);
        }

        private static void EmitBarrier(AilEmitterCtx context)
        {
            //Note: This barrier is most likely not necessary, and probably
            //doesn't make any difference since we need to do a ton of stuff
            //(software MMU emulation) to read or write anything anyway.
            context.EmitCall(typeof(Thread), nameof(Thread.MemoryBarrier));
        }
    }
}