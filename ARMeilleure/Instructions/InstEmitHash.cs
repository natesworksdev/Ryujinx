using ARMeilleure.Decoders;
using ARMeilleure.IntermediateRepresentation;
using ARMeilleure.Translation;
using System.Reflection;

using static ARMeilleure.Instructions.InstEmitHelper;

namespace ARMeilleure.Instructions
{
    static partial class InstEmit
    {
        public static void Crc32b(EmitterContext context)
        {
            EmitCrc32Call(context, nameof(SoftFallback.Crc32b));
        }

        public static void Crc32h(EmitterContext context)
        {
            EmitCrc32Call(context, nameof(SoftFallback.Crc32h));
        }

        public static void Crc32w(EmitterContext context)
        {
            EmitCrc32Call(context, nameof(SoftFallback.Crc32w));
        }

        public static void Crc32x(EmitterContext context)
        {
            EmitCrc32Call(context, nameof(SoftFallback.Crc32x));
        }

        public static void Crc32cb(EmitterContext context)
        {
            EmitCrc32Call(context, nameof(SoftFallback.Crc32cb));
        }

        public static void Crc32ch(EmitterContext context)
        {
            EmitCrc32Call(context, nameof(SoftFallback.Crc32ch));
        }

        public static void Crc32cw(EmitterContext context)
        {
            EmitCrc32Call(context, nameof(SoftFallback.Crc32cw));
        }

        public static void Crc32cx(EmitterContext context)
        {
            EmitCrc32Call(context, nameof(SoftFallback.Crc32cx));
        }

        private static void EmitCrc32Call(EmitterContext context, string name)
        {
            OpCodeAluBinary op = (OpCodeAluBinary)context.CurrOp;

            MethodInfo info = typeof(SoftFallback).GetMethod(name);

            Operand n = GetIntOrZR(context, op.Rn);
            Operand m = GetIntOrZR(context, op.Rm);

            Operand d = context.Call(info, n, m);

            SetIntOrZR(context, op.Rd, d);
        }
    }
}
