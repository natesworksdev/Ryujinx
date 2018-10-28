using ChocolArm64.Decoder;
using ChocolArm64.Memory;
using ChocolArm64.Translation;
using System;
using System.Reflection.Emit;

namespace ChocolArm64.Instruction
{
    static class AInstEmitMemoryHelper
    {
        private enum Extension
        {
            Zx,
            Sx32,
            Sx64
        }

        public static void EmitReadZxCall(AilEmitterCtx context, int size)
        {
            EmitReadCall(context, Extension.Zx, size);
        }

        public static void EmitReadSx32Call(AilEmitterCtx context, int size)
        {
            EmitReadCall(context, Extension.Sx32, size);
        }

        public static void EmitReadSx64Call(AilEmitterCtx context, int size)
        {
            EmitReadCall(context, Extension.Sx64, size);
        }

        private static void EmitReadCall(AilEmitterCtx context, Extension ext, int size)
        {
            bool isSimd = GetIsSimd(context);

            string name = null;

            if (size < 0 || size > (isSimd ? 4 : 3))
            {
                throw new ArgumentOutOfRangeException(nameof(size));
            }

            if (isSimd)
            {
                switch (size)
                {
                    case 0: name = nameof(AMemory.ReadVector8);   break;
                    case 1: name = nameof(AMemory.ReadVector16);  break;
                    case 2: name = nameof(AMemory.ReadVector32);  break;
                    case 3: name = nameof(AMemory.ReadVector64);  break;
                    case 4: name = nameof(AMemory.ReadVector128); break;
                }
            }
            else
            {
                switch (size)
                {
                    case 0: name = nameof(AMemory.ReadByte);   break;
                    case 1: name = nameof(AMemory.ReadUInt16); break;
                    case 2: name = nameof(AMemory.ReadUInt32); break;
                    case 3: name = nameof(AMemory.ReadUInt64); break;
                }
            }

            context.EmitCall(typeof(AMemory), name);

            if (!isSimd)
            {
                if (ext == Extension.Sx32 ||
                    ext == Extension.Sx64)
                {
                    switch (size)
                    {
                        case 0: context.Emit(OpCodes.Conv_I1); break;
                        case 1: context.Emit(OpCodes.Conv_I2); break;
                        case 2: context.Emit(OpCodes.Conv_I4); break;
                    }
                }

                if (size < 3)
                {
                    context.Emit(ext == Extension.Sx64
                        ? OpCodes.Conv_I8
                        : OpCodes.Conv_U8);
                }
            }
        }

        public static void EmitWriteCall(AilEmitterCtx context, int size)
        {
            bool isSimd = GetIsSimd(context);

            string name = null;

            if (size < 0 || size > (isSimd ? 4 : 3))
            {
                throw new ArgumentOutOfRangeException(nameof(size));
            }

            if (size < 3 && !isSimd)
            {
                context.Emit(OpCodes.Conv_I4);
            }

            if (isSimd)
            {
                switch (size)
                {
                    case 0: name = nameof(AMemory.WriteVector8);   break;
                    case 1: name = nameof(AMemory.WriteVector16);  break;
                    case 2: name = nameof(AMemory.WriteVector32);  break;
                    case 3: name = nameof(AMemory.WriteVector64);  break;
                    case 4: name = nameof(AMemory.WriteVector128); break;
                }
            }
            else
            {
                switch (size)
                {
                    case 0: name = nameof(AMemory.WriteByte);   break;
                    case 1: name = nameof(AMemory.WriteUInt16); break;
                    case 2: name = nameof(AMemory.WriteUInt32); break;
                    case 3: name = nameof(AMemory.WriteUInt64); break;
                }
            }

            context.EmitCall(typeof(AMemory), name);
        }

        private static bool GetIsSimd(AilEmitterCtx context)
        {
            return context.CurrOp is IaOpCodeSimd &&
                 !(context.CurrOp is AOpCodeSimdMemMs ||
                   context.CurrOp is AOpCodeSimdMemSs);
        }
    }
}