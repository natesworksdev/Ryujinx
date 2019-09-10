using ARMeilleure.Decoders;
using ARMeilleure.IntermediateRepresentation;
using ARMeilleure.Translation;
using System;

using static ARMeilleure.Instructions.InstEmitHelper;
using static ARMeilleure.IntermediateRepresentation.OperandHelper;

namespace ARMeilleure.Instructions
{
    static partial class InstEmit
    {
        private const int DczSizeLog2 = 4;

        public static void Hint(ArmEmitterContext context)
        {
            // Execute as no-op.
        }

        public static void Isb(ArmEmitterContext context)
        {
            // Execute as no-op.
        }

        public static void Mrs(ArmEmitterContext context)
        {
            OpCodeSystem op = (OpCodeSystem)context.CurrOp;

            string name;

            switch (GetPackedId(op))
            {
                case 0b11_011_0000_0000_001: name = nameof(NativeInterface.GetCtrEl0);    break;
                case 0b11_011_0000_0000_111: name = nameof(NativeInterface.GetDczidEl0);  break;
                case 0b11_011_0100_0100_000: name = nameof(NativeInterface.GetFpcr);      break;
                case 0b11_011_0100_0100_001: name = nameof(NativeInterface.GetFpsr);      break;
                case 0b11_011_1101_0000_010: name = nameof(NativeInterface.GetTpidrEl0);  break;
                case 0b11_011_1101_0000_011: name = nameof(NativeInterface.GetTpidr);     break;
                case 0b11_011_1110_0000_000: name = nameof(NativeInterface.GetCntfrqEl0); break;
                case 0b11_011_1110_0000_001: name = nameof(NativeInterface.GetCntpctEl0); break;

                default: throw new NotImplementedException($"Unknown MRS 0x{op.RawOpCode:X8} at 0x{op.Address:X16}.");
            }

            SetIntOrZR(context, op.Rt, context.NativeInterfaceCall(name));
        }

        public static void Msr(ArmEmitterContext context)
        {
            OpCodeSystem op = (OpCodeSystem)context.CurrOp;

            string name;

            switch (GetPackedId(op))
            {
                case 0b11_011_0100_0100_000: name = nameof(NativeInterface.SetFpcr);     break;
                case 0b11_011_0100_0100_001: name = nameof(NativeInterface.SetFpsr);     break;
                case 0b11_011_1101_0000_010: name = nameof(NativeInterface.SetTpidrEl0); break;

                default: throw new NotImplementedException($"Unknown MSR 0x{op.RawOpCode:X8} at 0x{op.Address:X16}.");
            }

            context.NativeInterfaceCall(name, GetIntOrZR(context, op.Rt));
        }

        public static void Nop(ArmEmitterContext context)
        {
            // Do nothing.
        }

        public static void Sys(ArmEmitterContext context)
        {
            // This instruction is used to do some operations on the CPU like cache invalidation,
            // address translation and the like.
            // We treat it as no-op here since we don't have any cache being emulated anyway.
            OpCodeSystem op = (OpCodeSystem)context.CurrOp;

            switch (GetPackedId(op))
            {
                case 0b11_011_0111_0100_001:
                {
                    // DC ZVA
                    Operand t = GetIntOrZR(context, op.Rt);

                    for (long offset = 0; offset < (4 << DczSizeLog2); offset += 8)
                    {
                        Operand address = context.Add(t, Const(offset));

                        context.NativeInterfaceCall(nameof(NativeInterface.WriteUInt64), address, Const(0L));
                    }

                    break;
                }

                // No-op
                case 0b11_011_0111_1110_001: //DC CIVAC
                    break;
            }
        }

        private static int GetPackedId(OpCodeSystem op)
        {
            int id;

            id  = op.Op2 << 0;
            id |= op.CRm << 3;
            id |= op.CRn << 7;
            id |= op.Op1 << 11;
            id |= op.Op0 << 14;

            return id;
        }
    }
}
