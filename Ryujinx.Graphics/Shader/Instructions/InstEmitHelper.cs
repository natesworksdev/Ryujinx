using Ryujinx.Graphics.Shader.Decoders;
using Ryujinx.Graphics.Shader.IntermediateRepresentation;
using Ryujinx.Graphics.Shader.Translation;
using System;

using static Ryujinx.Graphics.Shader.IntermediateRepresentation.OperandHelper;

namespace Ryujinx.Graphics.Shader.Instructions
{
    static class InstEmitHelper
    {
        public static Operand GetZF(EmitterContext context)
        {
            return Register(0, RegisterType.Flag);
        }

        public static Operand GetNF(EmitterContext context)
        {
            return Register(1, RegisterType.Flag);
        }

        public static Operand GetCF(EmitterContext context)
        {
            return Register(2, RegisterType.Flag);
        }

        public static Operand GetVF(EmitterContext context)
        {
            return Register(3, RegisterType.Flag);
        }

        public static Operand GetDest(EmitterContext context)
        {
            return Register(((IOpCodeRd)context.CurrOp).Rd);
        }

        public static Operand GetSrcA(EmitterContext context)
        {
            return Register(((IOpCodeRa)context.CurrOp).Ra);
        }

        public static Operand GetSrcB(EmitterContext context, FPType floatType)
        {
            return GetSrcB(context);
        }

        public static Operand GetSrcB(EmitterContext context)
        {
            switch (context.CurrOp)
            {
                case IOpCodeCbuf op:
                    return Cbuf(op.Slot, op.Offset);

                case IOpCodeImm op:
                    return Const(op.Immediate);

                case IOpCodeImmF op:
                    return ConstF(op.Immediate);

                case IOpCodeReg op:
                    return Register(op.Rb);
            }

            throw new InvalidOperationException($"Unexpected opcode type \"{context.CurrOp.GetType().Name}\".");
        }

        public static Operand GetSrcC(EmitterContext context)
        {
            switch (context.CurrOp)
            {
                case IOpCodeRegCbuf op:
                    return Cbuf(op.Slot, op.Offset);

                case IOpCodeRc op:
                    return Register(op.Rc);
            }

            throw new InvalidOperationException($"Unexpected opcode type \"{context.CurrOp.GetType().Name}\".");
        }

        public static Operand GetPredicate39(EmitterContext context)
        {
            IOpCodeAlu op = (IOpCodeAlu)context.CurrOp;

            Operand local = Register(op.Predicate39);

            if (op.InvertP)
            {
                local = context.BitwiseNot(local);
            }

            return local;
        }

        public static Operand SignExtendTo32(EmitterContext context, Operand src, int srcBits)
        {
            return context.BitfieldExtractS32(src, Const(0), Const(srcBits));
        }

        public static Operand ZeroExtendTo32(EmitterContext context, Operand src, int srcBits)
        {
            int mask = (int)(0xffffffffu >> (32 - srcBits));

            return context.BitwiseAnd(src, Const(mask));
        }
    }
}