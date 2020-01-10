using ARMeilleure.Decoders;
using ARMeilleure.IntermediateRepresentation;
using ARMeilleure.Translation;

using static ARMeilleure.Instructions.InstEmitHelper;

namespace ARMeilleure.Instructions
{
    static partial class InstEmit
    {
        public static void Crc32b(ArmEmitterContext context)
        {
            EmitCrc32Call(context, nameof(SoftFallback.Crc32b));
        }

        public static void Crc32h(ArmEmitterContext context)
        {
            EmitCrc32Call(context, nameof(SoftFallback.Crc32h));
        }

        public static void Crc32w(ArmEmitterContext context)
        {
            EmitCrc32Call(context, nameof(SoftFallback.Crc32w));
        }

        public static void Crc32x(ArmEmitterContext context)
        {
            EmitCrc32Call(context, nameof(SoftFallback.Crc32x));
        }

        public static void Crc32cb(ArmEmitterContext context)
        {
            EmitCrc32Call(context, nameof(SoftFallback.Crc32cb));
        }

        public static void Crc32ch(ArmEmitterContext context)
        {
            EmitCrc32Call(context, nameof(SoftFallback.Crc32ch));
        }

        public static void Crc32cw(ArmEmitterContext context)
        {
            EmitCrc32Call(context, nameof(SoftFallback.Crc32cw));
        }

        public static void Crc32cx(ArmEmitterContext context)
        {
            EmitCrc32Call(context, nameof(SoftFallback.Crc32cx));
        }

        private static void EmitCrc32Call(ArmEmitterContext context, string name)
        {
            OpCodeAluBinary op = (OpCodeAluBinary)context.CurrOp;

            Operand n = GetIntOrZR(context, op.Rn);
            Operand m = GetIntOrZR(context, op.Rm);

            Operand d = context.Call(typeof(SoftFallback).GetMethod(name), n, m);

            SetIntOrZR(context, op.Rd, d);
        }
    }
}
