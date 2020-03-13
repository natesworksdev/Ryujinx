using ARMeilleure.Decoders;
using ARMeilleure.IntermediateRepresentation;
using ARMeilleure.Translation;
using System;

using static ARMeilleure.Instructions.InstEmitHelper;

namespace ARMeilleure.Instructions
{
    static partial class InstEmit32
    {
        public static void Crc32b(ArmEmitterContext context)
        {
            EmitCrc32Call(context, new _U32_U32_U8(SoftFallback.Crc32b));
        }

        public static void Crc32h(ArmEmitterContext context)
        {
            EmitCrc32Call(context, new _U32_U32_U16(SoftFallback.Crc32h));
        }

        public static void Crc32w(ArmEmitterContext context)
        {
            EmitCrc32Call(context, new _U32_U32_U32(SoftFallback.Crc32w));
        }

        public static void Crc32cb(ArmEmitterContext context)
        {
            EmitCrc32Call(context, new _U32_U32_U8(SoftFallback.Crc32cb));
        }

        public static void Crc32ch(ArmEmitterContext context)
        {
            EmitCrc32Call(context, new _U32_U32_U16(SoftFallback.Crc32ch));
        }

        public static void Crc32cw(ArmEmitterContext context)
        {
            EmitCrc32Call(context, new _U32_U32_U32(SoftFallback.Crc32cw));
        }

        private static void EmitCrc32Call(ArmEmitterContext context, Delegate dlg)
        {
            IOpCode32AluReg op = (IOpCode32AluReg)context.CurrOp;

            Operand n = GetIntA32(context, op.Rn);
            Operand m = GetIntA32(context, op.Rm);

            Operand d = context.Call(dlg, n, m);

            EmitAluStore(context, d);
        }
    }
}
