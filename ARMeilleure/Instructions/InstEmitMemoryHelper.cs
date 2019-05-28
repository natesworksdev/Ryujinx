using ARMeilleure.Decoders;
using ARMeilleure.IntermediateRepresentation;
using ARMeilleure.State;
using ARMeilleure.Translation;
using System;

using static ARMeilleure.Instructions.InstEmitHelper;
using static ARMeilleure.IntermediateRepresentation.OperandHelper;

namespace ARMeilleure.Instructions
{
    static class InstEmitMemoryHelper
    {
        public static Operand EmitLoadZx(
            EmitterContext context,
            Operand        value,
            Operand        address,
            int            size)
        {
            //TODO: Support vector loads with size < 4.
            //Also handle value.Kind == OperandKind.Constant.

            switch (size)
            {
                case 0: return context.LoadZx8 (value, address);
                case 1: return context.LoadZx16(value, address);
                case 2: return context.Load    (value, address);
                case 3: return context.Load    (value, address);
                case 4: return context.Load    (value, address);

                default: throw new ArgumentOutOfRangeException(nameof(size));
            }
        }

        public static Operand EmitLoadSx(
            EmitterContext context,
            Operand        value,
            Operand        address,
            int            size)
        {
            //TODO: Support vector loads with size < 4.
            //Also handle value.Kind == OperandKind.Constant.

            switch (size)
            {
                case 0: return context.LoadSx8 (value, address);
                case 1: return context.LoadSx16(value, address);
                case 2: return context.LoadSx32(value, address);
                case 3: return context.Load    (value, address);
                case 4: return context.Load    (value, address);

                default: throw new ArgumentOutOfRangeException(nameof(size));
            }
        }

        public static void EmitStore(
            EmitterContext context,
            Operand        address,
            Operand        value,
            int            size)
        {
            //TODO: Support vector stores with size < 4.

            switch (size)
            {
                case 0: context.Store8 (address, value); break;
                case 1: context.Store16(address, value); break;
                case 2: context.Store  (address, value); break;
                case 3: context.Store  (address, value); break;
                case 4: context.Store  (address, value); break;

                default: throw new ArgumentOutOfRangeException(nameof(size));
            }
        }

        public static Operand GetT(EmitterContext context, int rt)
        {
            OpCode op = context.CurrOp;

            if (op is IOpCodeSimd)
            {
                return GetVec(rt);
            }
            else if (op is OpCodeMem opMem)
            {
                bool is32Bits = opMem.Size < 3 && !opMem.Extend64;

                OperandType type = is32Bits ? OperandType.I32 : OperandType.I64;

                if (rt == RegisterConsts.ZeroIndex)
                {
                    return Const(type, 0);
                }

                return Register(rt, RegisterType.Integer, type);
            }
            else
            {
                return GetIntOrZR(context.CurrOp, rt);
            }
        }
    }
}