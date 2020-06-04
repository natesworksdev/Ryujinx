using ARMeilleure.Decoders;
using ARMeilleure.IntermediateRepresentation;
using ARMeilleure.Translation;
using System;

using static ARMeilleure.Instructions.InstEmitHelper;
using static ARMeilleure.Instructions.InstEmitSimdHelper;
using static ARMeilleure.IntermediateRepresentation.OperandHelper;

namespace ARMeilleure.Instructions
{
    static partial class InstEmit
    {
        public static void Crc32b(ArmEmitterContext context)
        {
            if (Optimizations.UsePclmulqdq)
            {
                EmitCrc32Optimized(context, false, 8);
                return;
            }

            EmitCrc32Call(context, new _U32_U32_U8(SoftFallback.Crc32b));
        }

        public static void Crc32h(ArmEmitterContext context)
        {
            if (Optimizations.UsePclmulqdq)
            {
                EmitCrc32Optimized(context, false, 16);
                return;
            }

            EmitCrc32Call(context, new _U32_U32_U16(SoftFallback.Crc32h));
        }

        public static void Crc32w(ArmEmitterContext context)
        {
            if (Optimizations.UsePclmulqdq)
            {
                EmitCrc32Optimized(context, false, 32);
                return;
            }

            EmitCrc32Call(context, new _U32_U32_U32(SoftFallback.Crc32w));
        }

        public static void Crc32x(ArmEmitterContext context)
        {
            if (Optimizations.UsePclmulqdq)
            {
                EmitCrc32Optimized64(context, false);
                return;
            }

            EmitCrc32Call(context, new _U32_U32_U64(SoftFallback.Crc32x));
        }

        public static void Crc32cb(ArmEmitterContext context)
        {
            if (Optimizations.UsePclmulqdq)
            {
                EmitCrc32Optimized(context, true, 8);
                return;
            }

            EmitCrc32Call(context, new _U32_U32_U8(SoftFallback.Crc32cb));
        }

        public static void Crc32ch(ArmEmitterContext context)
        {
            if (Optimizations.UsePclmulqdq)
            {
                EmitCrc32Optimized(context, true, 16);
                return;
            }

            EmitCrc32Call(context, new _U32_U32_U16(SoftFallback.Crc32ch));
        }

        public static void Crc32cw(ArmEmitterContext context)
        {
            if (Optimizations.UsePclmulqdq)
            {
                EmitCrc32Optimized(context, true, 32);
                return;
            }

            EmitCrc32Call(context, new _U32_U32_U32(SoftFallback.Crc32cw));
        }

        public static void Crc32cx(ArmEmitterContext context)
        {
            if (Optimizations.UsePclmulqdq)
            {
                EmitCrc32Optimized64(context, true);
                return;
            }

            EmitCrc32Call(context, new _U32_U32_U64(SoftFallback.Crc32cx));
        }

        private static void EmitCrc32Optimized(ArmEmitterContext context, bool castagnoli, int bitsize)
        {
            OpCodeAluBinary op = (OpCodeAluBinary)context.CurrOp;

            Operand crc = GetIntOrZR(context, op.Rn);
            Operand data = GetIntOrZR(context, op.Rm);

            crc = context.VectorInsert(context.VectorZero(), crc, 0);

            switch (bitsize)
            {
            case 8:
                data = context.VectorInsert8(context.VectorZero(), data, 0);
                break;
            case 16:
                data = context.VectorInsert16(context.VectorZero(), data, 0);
                break;
            case 32:
                data = context.VectorInsert(context.VectorZero(), data, 0);
                break;
            }

            Operand tmp = context.AddIntrinsic(Intrinsic.X86Pxor, crc, data);
            tmp = context.AddIntrinsic(Intrinsic.X86Psllq, tmp, Const(64 - bitsize));
            tmp = context.AddIntrinsic(Intrinsic.X86Pclmulqdq, tmp, X86GetScalar(context, castagnoli ? 0x0DEA713F1 : 0x1F7011641), Const(0));
            tmp = context.AddIntrinsic(Intrinsic.X86Pclmulqdq, tmp, X86GetScalar(context, castagnoli ? 0x105EC76F0 : 0x1DB710641), Const(0));

            if (bitsize < 32)
            {
                crc = context.AddIntrinsic(Intrinsic.X86Pslldq, crc, Const((64 - bitsize) / 8));
                tmp = context.AddIntrinsic(Intrinsic.X86Pxor, tmp, crc);
            }

            SetIntOrZR(context, op.Rd, context.VectorExtract(OperandType.I32, tmp, 2));
        }

        private static void EmitCrc32Optimized64(ArmEmitterContext context, bool castagnoli)
        {
            OpCodeAluBinary op = (OpCodeAluBinary)context.CurrOp;

            Operand crc = GetIntOrZR(context, op.Rn);
            Operand data = GetIntOrZR(context, op.Rm);

            crc = context.VectorInsert(context.VectorZero(), crc, 0);
            data = context.VectorInsert(context.VectorZero(), data, 0);

            Operand tmp = context.AddIntrinsic(Intrinsic.X86Pxor, crc, data);
            Operand res = context.AddIntrinsic(Intrinsic.X86Pslldq, tmp, Const(4));

            tmp = context.AddIntrinsic(Intrinsic.X86Pclmulqdq, res, X86GetScalar(context, castagnoli ? 0x0DEA713F1 : 0x1F7011641), Const(0));
            tmp = context.AddIntrinsic(Intrinsic.X86Pclmulqdq, tmp, X86GetScalar(context, castagnoli ? 0x105EC76F0 : 0x1DB710641), Const(0));

            tmp = context.AddIntrinsic(Intrinsic.X86Pxor, tmp, res);
            tmp = context.AddIntrinsic(Intrinsic.X86Psllq, tmp, Const(32));

            tmp = context.AddIntrinsic(Intrinsic.X86Pclmulqdq, tmp, X86GetScalar(context, castagnoli ? 0x0DEA713F1 : 0x1F7011641), Const(1));
            tmp = context.AddIntrinsic(Intrinsic.X86Pclmulqdq, tmp, X86GetScalar(context, castagnoli ? 0x105EC76F0 : 0x1DB710641), Const(0));

            SetIntOrZR(context, op.Rd, context.VectorExtract(OperandType.I32, tmp, 2));
        }

        private static void EmitCrc32Call(ArmEmitterContext context, Delegate dlg)
        {
            OpCodeAluBinary op = (OpCodeAluBinary)context.CurrOp;

            Operand n = GetIntOrZR(context, op.Rn);
            Operand m = GetIntOrZR(context, op.Rm);

            Operand d = context.Call(dlg, n, m);

            SetIntOrZR(context, op.Rd, d);
        }
    }
}
