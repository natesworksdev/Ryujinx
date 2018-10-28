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
        public static void Crc32b(AilEmitterCtx context)
        {
            EmitCrc32(context, nameof(ASoftFallback.Crc32B));
        }

        public static void Crc32h(AilEmitterCtx context)
        {
            EmitCrc32(context, nameof(ASoftFallback.Crc32H));
        }

        public static void Crc32w(AilEmitterCtx context)
        {
            EmitCrc32(context, nameof(ASoftFallback.Crc32W));
        }

        public static void Crc32x(AilEmitterCtx context)
        {
            EmitCrc32(context, nameof(ASoftFallback.Crc32X));
        }

        public static void Crc32cb(AilEmitterCtx context)
        {
            if (AOptimizations.UseSse42)
            {
                EmitSse42Crc32(context, typeof(uint), typeof(byte));
            }
            else
            {
                EmitCrc32(context, nameof(ASoftFallback.Crc32Cb));
            }
        }

        public static void Crc32ch(AilEmitterCtx context)
        {
            if (AOptimizations.UseSse42)
            {
                EmitSse42Crc32(context, typeof(uint), typeof(ushort));
            }
            else
            {
                EmitCrc32(context, nameof(ASoftFallback.Crc32Ch));
            }
        }

        public static void Crc32cw(AilEmitterCtx context)
        {
            if (AOptimizations.UseSse42)
            {
                EmitSse42Crc32(context, typeof(uint), typeof(uint));
            }
            else
            {
                EmitCrc32(context, nameof(ASoftFallback.Crc32Cw));
            }
        }

        public static void Crc32cx(AilEmitterCtx context)
        {
            if (AOptimizations.UseSse42)
            {
                EmitSse42Crc32(context, typeof(ulong), typeof(ulong));
            }
            else
            {
                EmitCrc32(context, nameof(ASoftFallback.Crc32Cx));
            }
        }

        private static void EmitSse42Crc32(AilEmitterCtx context, Type tCrc, Type tData)
        {
            AOpCodeAluRs op = (AOpCodeAluRs)context.CurrOp;

            context.EmitLdintzr(op.Rn);
            context.EmitLdintzr(op.Rm);

            context.EmitCall(typeof(Sse42).GetMethod(nameof(Sse42.Crc32), new Type[] { tCrc, tData }));

            context.EmitStintzr(op.Rd);
        }

        private static void EmitCrc32(AilEmitterCtx context, string name)
        {
            AOpCodeAluRs op = (AOpCodeAluRs)context.CurrOp;

            context.EmitLdintzr(op.Rn);

            if (op.RegisterSize != ARegisterSize.Int32)
            {
                context.Emit(OpCodes.Conv_U4);
            }

            context.EmitLdintzr(op.Rm);

            ASoftFallback.EmitCall(context, name);

            if (op.RegisterSize != ARegisterSize.Int32)
            {
                context.Emit(OpCodes.Conv_U8);
            }

            context.EmitStintzr(op.Rd);
        }
    }
}
