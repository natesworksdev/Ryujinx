using ARMeilleure.Decoders;
using ARMeilleure.IntermediateRepresentation;
using ARMeilleure.State;
using ARMeilleure.Translation;

using static ARMeilleure.Instructions.InstEmitHelper;
using static ARMeilleure.Instructions.InstEmitAluHelper;
using static ARMeilleure.IntermediateRepresentation.OperandHelper;
using System;

namespace ARMeilleure.Instructions
{
    static partial class InstEmit32
    {
        [Flags]
        private enum MullFlags
        {
            Subtract = 0,
            Add = 1 << 0,
            Signed = 1 << 1,

            SignedAdd = Signed | Add,
            SignedSubtract = Signed | Subtract
        }

        public static void Umull(ArmEmitterContext context)
        {
            OpCode32AluUmull op = (OpCode32AluUmull)context.CurrOp;

            Operand n = context.ZeroExtend32(OperandType.I64, GetIntA32(context, op.Rn));
            Operand m = context.ZeroExtend32(OperandType.I64, GetIntA32(context, op.Rm));

            Operand res = context.Multiply(n, m);

            Operand hi = context.ConvertI64ToI32(context.ShiftRightUI(res, Const(32)));
            Operand lo = context.ConvertI64ToI32(res);

            if (op.SetFlags)
            {
                EmitNZFlagsCheck(context, res);
            }

            EmitGenericStore(context, op.RdHi, op.SetFlags, hi);
            EmitGenericStore(context, op.RdLo, op.SetFlags, lo);
        }

        public static void Smmul(ArmEmitterContext context)
        {
            OpCode32AluMla op = (OpCode32AluMla)context.CurrOp;

            Operand n = context.SignExtend32(OperandType.I64, GetIntA32(context, op.Rn));
            Operand m = context.SignExtend32(OperandType.I64, GetIntA32(context, op.Rm));

            Operand res = context.Multiply(n, m);

            if ((op.RawOpCode & (1 << 5)) != 0)
            {
                res = context.Add(res, Const(0x80000000L));
            }

            Operand hi = context.ConvertI64ToI32(context.ShiftRightSI(res, Const(32)));

            EmitGenericStore(context, op.Rd, false, hi);
        }


        public static void Smlab(ArmEmitterContext context)
        {
            OpCode32AluMla op = (OpCode32AluMla)context.CurrOp;

            Operand n = GetIntA32(context, op.Rn);
            Operand m = GetIntA32(context, op.Rm);

            if (op.NHigh)
            {
                n = context.SignExtend16(OperandType.I32, context.ShiftRightUI(n, Const(16)));
            }
            else
            {
                n = context.SignExtend16(OperandType.I32, n);
            }

            if (op.MHigh)
            {
                m = context.SignExtend16(OperandType.I32, context.ShiftRightUI(m, Const(16)));
            }
            else
            {
                m = context.SignExtend16(OperandType.I32, m);
            }

            Operand res = context.Multiply(n, m);

            Operand a = GetIntA32(context, op.Ra);
            res = context.Add(res, a);

            //todo: set Q flag when last addition overflows (saturation)?

            EmitGenericStore(context, op.Rd, false, res);
        }

        public static void Smlal(ArmEmitterContext context)
        {
            OpCode32AluUmull op = (OpCode32AluUmull)context.CurrOp;

            Operand n = GetIntA32(context, op.Rn);
            Operand m = GetIntA32(context, op.Rm);

            n = context.SignExtend32(OperandType.I64, n);
            m = context.SignExtend32(OperandType.I64, m);

            Operand res = context.Multiply(n, m);

            Operand toAdd = context.ShiftLeft(context.ZeroExtend32(OperandType.I64, GetIntA32(context, op.RdHi)), Const(32));
            toAdd = context.BitwiseOr(toAdd, context.ZeroExtend32(OperandType.I64, GetIntA32(context, op.RdLo)));
            res = context.Add(res, toAdd);

            Operand hi = context.ConvertI64ToI32(context.ShiftRightUI(res, Const(32)));
            Operand lo = context.ConvertI64ToI32(res);

            if (op.SetFlags)
            {
                EmitNZFlagsCheck(context, res);
            }

            EmitGenericStore(context, op.RdHi, op.SetFlags, hi);
            EmitGenericStore(context, op.RdLo, op.SetFlags, lo);
        }

        public static void Smlalh(ArmEmitterContext context)
        {
            OpCode32AluUmull op = (OpCode32AluUmull)context.CurrOp;

            Operand n = GetIntA32(context, op.Rn);
            Operand m = GetIntA32(context, op.Rm);

            if (op.NHigh)
            {
                n = context.SignExtend16(OperandType.I64, context.ShiftRightUI(n, Const(16)));
            } 
            else
            {
                n = context.SignExtend16(OperandType.I64, n);
            }

            if (op.MHigh)
            {
                m = context.SignExtend16(OperandType.I64, context.ShiftRightUI(m, Const(16)));
            } 
            else
            {
                m = context.SignExtend16(OperandType.I64, m);
            }

            Operand res = context.Multiply(n, m);

            Operand toAdd = context.ShiftLeft(context.ZeroExtend32(OperandType.I64, GetIntA32(context, op.RdHi)), Const(32));
            toAdd = context.BitwiseOr(toAdd, context.ZeroExtend32(OperandType.I64, GetIntA32(context, op.RdLo)));
            res = context.Add(res, toAdd);

            Operand hi = context.ConvertI64ToI32(context.ShiftRightUI(res, Const(32)));
            Operand lo = context.ConvertI64ToI32(res);

            EmitGenericStore(context, op.RdHi, false, hi);
            EmitGenericStore(context, op.RdLo, false, lo);
        }

        private static void EmitGenericStore(ArmEmitterContext context, int Rd, bool setFlags, Operand value)
        {
            if (Rd == RegisterAlias.Aarch32Pc)
            {
                if (setFlags)
                {
                    // TODO: Load SPSR etc.
                    Operand isThumb = GetFlag(PState.TFlag);

                    Operand lblThumb = Label();

                    context.BranchIfTrue(lblThumb, isThumb);

                    context.Return(context.ZeroExtend32(OperandType.I64, context.BitwiseAnd(value, Const(~3))));

                    context.MarkLabel(lblThumb);

                    context.Return(context.ZeroExtend32(OperandType.I64, context.BitwiseAnd(value, Const(~1))));
                }
                else
                {
                    EmitAluWritePc(context, value);
                }
            }
            else
            {
                SetIntA32(context, Rd, value);
            }
        }
    }
}
