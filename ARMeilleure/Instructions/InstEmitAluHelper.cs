using ARMeilleure.Decoders;
using ARMeilleure.IntermediateRepresentation;
using ARMeilleure.State;
using ARMeilleure.Translation;
using System;

using static ARMeilleure.Instructions.InstEmitHelper;
using static ARMeilleure.IntermediateRepresentation.OperandHelper;

namespace ARMeilleure.Instructions
{
    static class InstEmitAluHelper
    {
        public static void EmitAdcsCCheck(EmitterContext context, Operand n, Operand d)
        {
            //C = (Rd == Rn && CIn) || Rd < Rn
            Operand cIn = GetFlag(PState.CFlag);

            Operand cOut = context.BitwiseAnd(context.ICompareEqual(d, n), cIn);

            cOut = context.BitwiseOr(cOut, context.ICompareLessUI(d, n));

            context.Copy(GetFlag(PState.CFlag), cOut);
        }

        public static void EmitAddsCCheck(EmitterContext context, Operand n, Operand d)
        {
            //C = Rd < Rn
            context.Copy(GetFlag(PState.CFlag), context.ICompareLessUI(d, n));
        }

        public static void EmitAddsVCheck(EmitterContext context, Operand n, Operand m, Operand d)
        {
            //V = (Rd ^ Rn) & ~(Rn ^ Rm) < 0
            Operand vOut = context.BitwiseExclusiveOr(d, n);

            vOut = context.BitwiseAnd(vOut, context.BitwiseNot(context.BitwiseExclusiveOr(n, m)));

            vOut = context.ICompareLess(vOut, Const(vOut.Type, 0));

            context.Copy(GetFlag(PState.VFlag), vOut);
        }

        public static void EmitSbcsCCheck(EmitterContext context, Operand n, Operand m)
        {
            //C = (Rn == Rm && CIn) || Rn > Rm
            Operand cIn = GetFlag(PState.CFlag);

            Operand cOut = context.BitwiseAnd(context.ICompareEqual(n, m), cIn);

            cOut = context.BitwiseOr(cOut, context.ICompareGreaterUI(n, m));

            context.Copy(GetFlag(PState.CFlag), cOut);
        }

        public static void EmitSubsCCheck(EmitterContext context, Operand n, Operand m)
        {
            //C = Rn >= Rm
            context.Copy(GetFlag(PState.CFlag), context.ICompareGreaterOrEqualUI(n, m));
        }

        public static void EmitSubsVCheck(EmitterContext context, Operand n, Operand m, Operand d)
        {
            //V = (Rd ^ Rn) & (Rn ^ Rm) < 0
            Operand vOut = context.BitwiseExclusiveOr(d, n);

            vOut = context.BitwiseAnd(vOut, context.BitwiseExclusiveOr(n, m));

            vOut = context.ICompareLess(vOut, Const(vOut.Type, 0));

            context.Copy(GetFlag(PState.VFlag), vOut);
        }

        public static Operand GetAluN(EmitterContext context)
        {
            if (context.CurrOp is IOpCodeAlu op)
            {
                if (op.DataOp == DataOp.Logical || op is IOpCodeAluRs)
                {
                    return GetIntOrZR(op, op.Rn);
                }
                else
                {
                    return GetIntOrSP(op, op.Rn);
                }
            }
            else if (context.CurrOp is IOpCode32Alu op32)
            {
                return GetIntA32(context, op32.Rn);
            }
            else
            {
                throw InvalidOpCodeType(context.CurrOp);
            }
        }

        public static Operand GetAluM(EmitterContext context, bool setCarry = true)
        {
            switch (context.CurrOp)
            {
                //ARM32.
                case OpCode32AluImm op:
                {
                    if (op.SetFlags && op.IsRotated)
                    {
                        context.Copy(GetFlag(PState.CFlag), Const((uint)op.Immediate >> 31));
                    }

                    return Const(op.Immediate);
                }

                case OpCode32AluRsImm op: return GetMShiftedByImmediate(context, op, setCarry);

                case OpCodeT16AluImm8 op: return Const(op.Immediate);

                //ARM64.
                case IOpCodeAluImm op:
                {
                    if (op.GetOperandType() == OperandType.I32)
                    {
                        return Const((int)op.Immediate);
                    }
                    else
                    {
                        return Const(op.Immediate);
                    }
                }

                case IOpCodeAluRs op:
                {
                    Operand value = GetIntOrZR(op, op.Rm);

                    switch (op.ShiftType)
                    {
                        case ShiftType.Lsl: value = context.ShiftLeft   (value, Const(op.Shift)); break;
                        case ShiftType.Lsr: value = context.ShiftRightUI(value, Const(op.Shift)); break;
                        case ShiftType.Asr: value = context.ShiftRightSI(value, Const(op.Shift)); break;
                        case ShiftType.Ror: value = context.RotateRight (value, Const(op.Shift)); break;
                    }

                    return value;
                }

                case IOpCodeAluRx op:
                {
                    Operand value = GetExtendedM(context, op.Rm, op.IntType);

                    value = context.ShiftLeft(value, Const(op.Shift));

                    return value;
                }

                default: throw InvalidOpCodeType(context.CurrOp);
            }
        }

        private static Exception InvalidOpCodeType(OpCode opCode)
        {
            return new InvalidOperationException($"Invalid OpCode type \"{opCode?.GetType().Name ?? "null"}\".");
        }

        //ARM32 helpers.
        private static Operand GetMShiftedByImmediate(EmitterContext context, OpCode32AluRsImm op, bool setCarry)
        {
            Operand m = GetIntA32(context, op.Rm);

            int shift = op.Imm;

            if (shift == 0)
            {
                switch (op.ShiftType)
                {
                    case ShiftType.Lsr: shift = 32; break;
                    case ShiftType.Asr: shift = 32; break;
                    case ShiftType.Ror: shift = 1;  break;
                }
            }

            if (shift != 0)
            {
                setCarry &= op.SetFlags;

                switch (op.ShiftType)
                {
                    case ShiftType.Lsl: m = GetLslC(context, m, setCarry, shift); break;
                    case ShiftType.Lsr: m = GetLsrC(context, m, setCarry, shift); break;
                    case ShiftType.Asr: m = GetAsrC(context, m, setCarry, shift); break;
                    case ShiftType.Ror:
                        if (op.Imm != 0)
                        {
                            m = GetRorC(context, m, setCarry, shift);
                        }
                        else
                        {
                            m = GetRrxC(context, m, setCarry);
                        }
                        break;
                }
            }

            return m;
        }

        private static Operand GetLslC(EmitterContext context, Operand m, bool setCarry, int shift)
        {
            if ((uint)shift > 32)
            {
                return GetShiftByMoreThan32(context, setCarry);
            }
            else if (shift == 32)
            {
                if (setCarry)
                {
                    SetCarryMLsb(context, m);
                }

                return Const(0);
            }
            else
            {
                if (setCarry)
                {
                    Operand cOut = context.ShiftRightUI(m, Const(32 - shift));

                    cOut = context.BitwiseAnd(cOut, Const(1));

                    context.Copy(GetFlag(PState.CFlag), cOut);
                }

                return context.ShiftLeft(m, Const(shift));
            }
        }

        private static Operand GetLsrC(EmitterContext context, Operand m, bool setCarry, int shift)
        {
            if ((uint)shift > 32)
            {
                return GetShiftByMoreThan32(context, setCarry);
            }
            else if (shift == 32)
            {
                if (setCarry)
                {
                    SetCarryMMsb(context, m);
                }

                return Const(0);
            }
            else
            {
                if (setCarry)
                {
                    SetCarryMShrOut(context, m, shift);
                }

                return context.ShiftRightUI(m, Const(shift));
            }
        }

        private static Operand GetShiftByMoreThan32(EmitterContext context, bool setCarry)
        {
            if (setCarry)
            {
                context.Copy(GetFlag(PState.CFlag), Const(0));;
            }

            return Const(0);
        }

        private static Operand GetAsrC(EmitterContext context, Operand m, bool setCarry, int shift)
        {
            if ((uint)shift >= 32)
            {
                m = context.ShiftRightSI(m, Const(31));

                if (setCarry)
                {
                    SetCarryMLsb(context, m);
                }

                return m;
            }
            else
            {
                if (setCarry)
                {
                    SetCarryMShrOut(context, m, shift);
                }

                return context.ShiftRightSI(m, Const(shift));
            }
        }

        private static Operand GetRorC(EmitterContext context, Operand m, bool setCarry, int shift)
        {
            shift &= 0x1f;

            m = context.RotateRight(m, Const(shift));

            if (setCarry)
            {
                SetCarryMMsb(context, m);
            }

            return m;
        }

        private static Operand GetRrxC(EmitterContext context, Operand m, bool setCarry)
        {
            //Rotate right by 1 with carry.
            Operand cIn = context.Copy(GetFlag(PState.CFlag));

            if (setCarry)
            {
                SetCarryMLsb(context, m);
            }

            m = context.ShiftRightUI(m, Const(1));

            m = context.BitwiseOr(m, context.ShiftLeft(cIn, Const(31)));

            return m;
        }

        private static void SetCarryMLsb(EmitterContext context, Operand m)
        {
            context.Copy(GetFlag(PState.CFlag), context.BitwiseAnd(m, Const(1)));
        }

        private static void SetCarryMMsb(EmitterContext context, Operand m)
        {
            context.Copy(GetFlag(PState.CFlag), context.ShiftRightUI(m, Const(31)));
        }

        private static void SetCarryMShrOut(EmitterContext context, Operand m, int shift)
        {
            Operand cOut = context.ShiftRightUI(m, Const(shift - 1));

            cOut = context.BitwiseAnd(cOut, Const(1));

            context.Copy(GetFlag(PState.CFlag), cOut);
        }
    }
}
