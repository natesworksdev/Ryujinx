using ARMeilleure.Decoders;
using ARMeilleure.IntermediateRepresentation;
using ARMeilleure.Translation;
using System;
using System.Reflection;

using static ARMeilleure.Instructions.InstEmitHelper;
using static ARMeilleure.IntermediateRepresentation.OperandHelper;

namespace ARMeilleure.Instructions
{
    static partial class InstEmit
    {
        private const int DczSizeLog2 = 4;

        public static void Hint(EmitterContext context)
        {
            //Execute as no-op.
        }

        public static void Isb(EmitterContext context)
        {
            //Execute as no-op.
        }

        public static void Mrs(EmitterContext context)
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

            MethodInfo info = typeof(NativeInterface).GetMethod(name);

            SetIntOrZR(context, op.Rt, context.Call(info));
        }

        public static void Msr(EmitterContext context)
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

            MethodInfo info = typeof(NativeInterface).GetMethod(name);

            context.Call(info, GetIntOrZR(context, op.Rt));
        }

        public static void Nop(EmitterContext context)
        {
            //Do nothing.
        }

        public static void Sys(EmitterContext context)
        {
            //This instruction is used to do some operations on the CPU like cache invalidation,
            //address translation and the like.
            //We treat it as no-op here since we don't have any cache being emulated anyway.
            OpCodeSystem op = (OpCodeSystem)context.CurrOp;

            switch (GetPackedId(op))
            {
                case 0b11_011_0111_0100_001:
                {
                    //DC ZVA
                    Operand t = GetIntOrZR(context, op.Rt);

                    MethodInfo info = typeof(NativeInterface).GetMethod(nameof(NativeInterface.WriteUInt64));

                    for (long offset = 0; offset < (4 << DczSizeLog2); offset += 8)
                    {
                        Operand address = context.Add(t, Const(offset));

                        context.Call(info, address, Const(0L));
                    }

                    break;
                }

                //No-op
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
