using ChocolArm64.Decoder;
using ChocolArm64.State;
using ChocolArm64.Translation;
using System;
using System.Reflection.Emit;
using System.Runtime.Intrinsics.X86;

namespace ChocolArm64.Instruction
{
    static partial class AInstEmit
    {
        public static void Crc32b(AILEmitterCtx Context)
        {
            EmitCrc32(Context, nameof(ASoftFallback.Crc32b));
        }

        public static void Crc32h(AILEmitterCtx Context)
        {
            EmitCrc32(Context, nameof(ASoftFallback.Crc32h));
        }

        public static void Crc32w(AILEmitterCtx Context)
        {
            EmitCrc32(Context, nameof(ASoftFallback.Crc32w));
        }

        public static void Crc32x(AILEmitterCtx Context)
        {
            EmitCrc32(Context, nameof(ASoftFallback.Crc32x));
        }

        public static void Crc32cb(AILEmitterCtx Context)
        {
            if (AOptimizations.UseSse42)
            {
                AOpCodeAluRs Op = (AOpCodeAluRs)Context.CurrOp;

                Context.EmitLdintzr(Op.Rn);
                Context.EmitLdintzr(Op.Rm);

                Context.EmitCall(typeof(Sse42).GetMethod(nameof(Sse42.Crc32), new Type[] { typeof(uint), typeof(byte) }));

                Context.EmitStintzr(Op.Rd);
            }
            else
            {
                EmitCrc32(Context, nameof(ASoftFallback.Crc32cb));
            }
        }

        public static void Crc32ch(AILEmitterCtx Context)
        {
            if (AOptimizations.UseSse42)
            {
                AOpCodeAluRs Op = (AOpCodeAluRs)Context.CurrOp;

                Context.EmitLdintzr(Op.Rn);
                Context.EmitLdintzr(Op.Rm);

                Context.EmitCall(typeof(Sse42).GetMethod(nameof(Sse42.Crc32), new Type[] { typeof(uint), typeof(ushort) }));

                Context.EmitStintzr(Op.Rd);
            }
            else
            {
                EmitCrc32(Context, nameof(ASoftFallback.Crc32ch));
            }
        }

        public static void Crc32cw(AILEmitterCtx Context)
        {
            if (AOptimizations.UseSse42)
            {
                AOpCodeAluRs Op = (AOpCodeAluRs)Context.CurrOp;

                Context.EmitLdintzr(Op.Rn);
                Context.EmitLdintzr(Op.Rm);

                Context.EmitCall(typeof(Sse42).GetMethod(nameof(Sse42.Crc32), new Type[] { typeof(uint), typeof(uint) }));

                Context.EmitStintzr(Op.Rd);
            }
            else
            {
                EmitCrc32(Context, nameof(ASoftFallback.Crc32cw));
            }
        }

        public static void Crc32cx(AILEmitterCtx Context)
        {
            if (AOptimizations.UseSse42)
            {
                AOpCodeAluRs Op = (AOpCodeAluRs)Context.CurrOp;

                Context.EmitLdintzr(Op.Rn);
                Context.EmitLdintzr(Op.Rm);

                Context.EmitCall(typeof(Sse42).GetMethod(nameof(Sse42.Crc32), new Type[] { typeof(ulong), typeof(ulong) }));

                Context.EmitStintzr(Op.Rd);
            }
            else
            {
                EmitCrc32(Context, nameof(ASoftFallback.Crc32cx));
            }
        }

        private static void EmitCrc32(AILEmitterCtx Context, string Name)
        {
            AOpCodeAluRs Op = (AOpCodeAluRs)Context.CurrOp;

            Context.EmitLdintzr(Op.Rn);

            if (Op.RegisterSize != ARegisterSize.Int32)
            {
                Context.Emit(OpCodes.Conv_U4);
            }

            Context.EmitLdintzr(Op.Rm);

            ASoftFallback.EmitCall(Context, Name);

            if (Op.RegisterSize != ARegisterSize.Int32)
            {
                Context.Emit(OpCodes.Conv_U8);
            }

            Context.EmitStintzr(Op.Rd);
        }
    }
}