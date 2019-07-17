using ARMeilleure.Decoders;
using ARMeilleure.IntermediateRepresentation;
using ARMeilleure.State;
using ARMeilleure.Translation;
using System;

using static ARMeilleure.IntermediateRepresentation.OperandHelper;

namespace ARMeilleure.Instructions
{
    static class InstEmitHelper
    {
        public static bool IsThumb(OpCode op)
        {
            return op is OpCodeT16;
        }

        public static Operand GetExtendedM(ArmEmitterContext context, int rm, IntType type)
        {
            Operand value = GetIntOrZR(context, rm);

            switch (type)
            {
                case IntType.UInt8:  value = context.ZeroExtend8 (value.Type, value); break;
                case IntType.UInt16: value = context.ZeroExtend16(value.Type, value); break;
                case IntType.UInt32: value = context.ZeroExtend32(value.Type, value); break;

                case IntType.Int8:  value = context.SignExtend8 (value.Type, value); break;
                case IntType.Int16: value = context.SignExtend16(value.Type, value); break;
                case IntType.Int32: value = context.SignExtend32(value.Type, value); break;
            }

            return value;
        }

        public static Operand GetIntA32(ArmEmitterContext context, int register)
        {
            if (register == RegisterAlias.Aarch32Pc)
            {
                OpCode32 op = (OpCode32)context.CurrOp;

                return Const((int)op.GetPc());
            }
            else
            {
                return GetIntOrSP(context, GetRegisterAlias(context.Mode, register));
            }
        }

        public static int GetRegisterAlias(Aarch32Mode mode, int register)
        {
            //Only registers >= 8 are banked,
            //with registers in the range [8, 12] being
            //banked for the FIQ mode, and registers
            //13 and 14 being banked for all modes.
            if ((uint)register < 8)
            {
                return register;
            }

            return GetBankedRegisterAlias(mode, register);
        }

        public static int GetBankedRegisterAlias(Aarch32Mode mode, int register)
        {
            switch (register)
            {
                case 8: return mode == Aarch32Mode.Fiq
                    ? RegisterAlias.R8Fiq
                    : RegisterAlias.R8Usr;

                case 9: return mode == Aarch32Mode.Fiq
                    ? RegisterAlias.R9Fiq
                    : RegisterAlias.R9Usr;

                case 10: return mode == Aarch32Mode.Fiq
                    ? RegisterAlias.R10Fiq
                    : RegisterAlias.R10Usr;

                case 11: return mode == Aarch32Mode.Fiq
                    ? RegisterAlias.R11Fiq
                    : RegisterAlias.R11Usr;

                case 12: return mode == Aarch32Mode.Fiq
                    ? RegisterAlias.R12Fiq
                    : RegisterAlias.R12Usr;

                case 13:
                    switch (mode)
                    {
                        case Aarch32Mode.User:
                        case Aarch32Mode.System:     return RegisterAlias.SpUsr;
                        case Aarch32Mode.Fiq:        return RegisterAlias.SpFiq;
                        case Aarch32Mode.Irq:        return RegisterAlias.SpIrq;
                        case Aarch32Mode.Supervisor: return RegisterAlias.SpSvc;
                        case Aarch32Mode.Abort:      return RegisterAlias.SpAbt;
                        case Aarch32Mode.Hypervisor: return RegisterAlias.SpHyp;
                        case Aarch32Mode.Undefined:  return RegisterAlias.SpUnd;

                        default: throw new ArgumentException(nameof(mode));
                    }

                case 14:
                    switch (mode)
                    {
                        case Aarch32Mode.User:
                        case Aarch32Mode.Hypervisor:
                        case Aarch32Mode.System:     return RegisterAlias.LrUsr;
                        case Aarch32Mode.Fiq:        return RegisterAlias.LrFiq;
                        case Aarch32Mode.Irq:        return RegisterAlias.LrIrq;
                        case Aarch32Mode.Supervisor: return RegisterAlias.LrSvc;
                        case Aarch32Mode.Abort:      return RegisterAlias.LrAbt;
                        case Aarch32Mode.Undefined:  return RegisterAlias.LrUnd;

                        default: throw new ArgumentException(nameof(mode));
                    }

                default: throw new ArgumentOutOfRangeException(nameof(register));
            }
        }

        public static Operand GetIntOrZR32(int regIndex)
        {
            if (regIndex == RegisterConsts.ZeroIndex)
            {
                return Const(0);
            }
            else
            {
                return Register(regIndex, RegisterType.Integer, OperandType.I32);
            }
        }

        public static Operand GetIntOrZR(ArmEmitterContext context, int regIndex)
        {
            if (regIndex == RegisterConsts.ZeroIndex)
            {
                OperandType type = context.CurrOp.GetOperandType();

                return type == OperandType.I32 ? Const(0) : Const(0L);
            }
            else
            {
                return GetIntOrSP(context, regIndex);
            }
        }

        public static void SetIntOrZR(ArmEmitterContext context, int regIndex, Operand value)
        {
            if (regIndex == RegisterConsts.ZeroIndex)
            {
                return;
            }

            SetIntOrSP(context, regIndex, value);
        }

        public static Operand GetIntOrSP(ArmEmitterContext context, int regIndex)
        {
            Operand value = Register(regIndex, RegisterType.Integer, OperandType.I64);

            if (context.CurrOp.RegisterSize == RegisterSize.Int32)
            {
                value = context.ConvertI64ToI32(value);
            }

            return value;
        }

        public static void SetIntOrSP(ArmEmitterContext context, int regIndex, Operand value)
        {
            Operand reg = Register(regIndex, RegisterType.Integer, OperandType.I64);

            if (value.Type == OperandType.I32)
            {
                value = context.ZeroExtend32(OperandType.I64, value);
            }

            context.Copy(reg, value);
        }

        public static Operand GetVec(int regIndex)
        {
            return Register(regIndex, RegisterType.Vector, OperandType.V128);
        }

        public static Operand GetFlag(PState stateFlag)
        {
            return Register((int)stateFlag, RegisterType.Flag, OperandType.I32);
        }

        public static void SetFlag(ArmEmitterContext context, PState stateFlag, Operand value)
        {
            context.Copy(GetFlag(stateFlag), value);

            context.MarkFlagSet(stateFlag);
        }
    }
}
