using ChocolArm64.Decoder;
using ChocolArm64.Translation;
using System.Reflection.Emit;

using static ChocolArm64.Instruction.AInstEmitMemoryHelper;

namespace ChocolArm64.Instruction
{
    static partial class AInstEmit
    {
        public static void Adr(AilEmitterCtx context)
        {
            AOpCodeAdr op = (AOpCodeAdr)context.CurrOp;

            context.EmitLdc_I(op.Position + op.Imm);
            context.EmitStintzr(op.Rd);
        }

        public static void Adrp(AilEmitterCtx context)
        {
            AOpCodeAdr op = (AOpCodeAdr)context.CurrOp;

            context.EmitLdc_I((op.Position & ~0xfffL) + (op.Imm << 12));
            context.EmitStintzr(op.Rd);
        }

        public static void Ldr(AilEmitterCtx context)  => EmitLdr(context, false);
        public static void Ldrs(AilEmitterCtx context) => EmitLdr(context, true);

        private static void EmitLdr(AilEmitterCtx context, bool signed)
        {
            AOpCodeMem op = (AOpCodeMem)context.CurrOp;

            context.EmitLdarg(ATranslatedSub.MemoryArgIdx);

            EmitLoadAddress(context);

            if (signed && op.Extend64)
            {
                EmitReadSx64Call(context, op.Size);
            }
            else if (signed)
            {
                EmitReadSx32Call(context, op.Size);
            }
            else
            {
                EmitReadZxCall(context, op.Size);
            }

            if (op is IaOpCodeSimd)
            {
                context.EmitStvec(op.Rt);
            }
            else
            {
                context.EmitStintzr(op.Rt);
            }

            EmitWBackIfNeeded(context);
        }

        public static void LdrLit(AilEmitterCtx context)
        {
            IaOpCodeLit op = (IaOpCodeLit)context.CurrOp;

            if (op.Prefetch)
            {
                return;
            }

            context.EmitLdarg(ATranslatedSub.MemoryArgIdx);
            context.EmitLdc_I8(op.Imm);

            if (op.Signed)
            {
                EmitReadSx64Call(context, op.Size);
            }
            else
            {
                EmitReadZxCall(context, op.Size);
            }

            if (op is IaOpCodeSimd)
            {
                context.EmitStvec(op.Rt);
            }
            else
            {
                context.EmitStint(op.Rt);
            }
        }

        public static void Ldp(AilEmitterCtx context)
        {
            AOpCodeMemPair op = (AOpCodeMemPair)context.CurrOp;

            void EmitReadAndStore(int rt)
            {
                if (op.Extend64)
                {
                    EmitReadSx64Call(context, op.Size);
                }
                else
                {
                    EmitReadZxCall(context, op.Size);
                }

                if (op is IaOpCodeSimd)
                {
                    context.EmitStvec(rt);
                }
                else
                {
                    context.EmitStintzr(rt);
                }
            }

            context.EmitLdarg(ATranslatedSub.MemoryArgIdx);

            EmitLoadAddress(context);

            EmitReadAndStore(op.Rt);

            context.EmitLdarg(ATranslatedSub.MemoryArgIdx);
            context.EmitLdtmp();
            context.EmitLdc_I8(1 << op.Size);

            context.Emit(OpCodes.Add);

            EmitReadAndStore(op.Rt2);

            EmitWBackIfNeeded(context);
        }

        public static void Str(AilEmitterCtx context)
        {
            AOpCodeMem op = (AOpCodeMem)context.CurrOp;

            context.EmitLdarg(ATranslatedSub.MemoryArgIdx);

            EmitLoadAddress(context);

            if (op is IaOpCodeSimd)
            {
                context.EmitLdvec(op.Rt);
            }
            else
            {
                context.EmitLdintzr(op.Rt);
            }

            EmitWriteCall(context, op.Size);

            EmitWBackIfNeeded(context);
        }

        public static void Stp(AilEmitterCtx context)
        {
            AOpCodeMemPair op = (AOpCodeMemPair)context.CurrOp;

            context.EmitLdarg(ATranslatedSub.MemoryArgIdx);

            EmitLoadAddress(context);

            if (op is IaOpCodeSimd)
            {
                context.EmitLdvec(op.Rt);
            }
            else
            {
                context.EmitLdintzr(op.Rt);
            }

            EmitWriteCall(context, op.Size);

            context.EmitLdarg(ATranslatedSub.MemoryArgIdx);
            context.EmitLdtmp();
            context.EmitLdc_I8(1 << op.Size);

            context.Emit(OpCodes.Add);

            if (op is IaOpCodeSimd)
            {
                context.EmitLdvec(op.Rt2);
            }
            else
            {
                context.EmitLdintzr(op.Rt2);
            }

            EmitWriteCall(context, op.Size);

            EmitWBackIfNeeded(context);
        }

        private static void EmitLoadAddress(AilEmitterCtx context)
        {
            switch (context.CurrOp)
            {
                case AOpCodeMemImm op:
                    context.EmitLdint(op.Rn);

                    if (!op.PostIdx)
                    {
                        //Pre-indexing.
                        context.EmitLdc_I(op.Imm);

                        context.Emit(OpCodes.Add);
                    }
                    break;

                case AOpCodeMemReg op:
                    context.EmitLdint(op.Rn);
                    context.EmitLdintzr(op.Rm);
                    context.EmitCast(op.IntType);

                    if (op.Shift)
                    {
                        context.EmitLsl(op.Size);
                    }

                    context.Emit(OpCodes.Add);
                    break;
            }

            //Save address to Scratch var since the register value may change.
            context.Emit(OpCodes.Dup);

            context.EmitSttmp();
        }

        private static void EmitWBackIfNeeded(AilEmitterCtx context)
        {
            //Check whenever the current OpCode has post-indexed write back, if so write it.
            //Note: AOpCodeMemPair inherits from AOpCodeMemImm, so this works for both.
            if (context.CurrOp is AOpCodeMemImm op && op.WBack)
            {
                context.EmitLdtmp();

                if (op.PostIdx)
                {
                    context.EmitLdc_I(op.Imm);

                    context.Emit(OpCodes.Add);
                }

                context.EmitStint(op.Rn);
            }
        }
    }
}