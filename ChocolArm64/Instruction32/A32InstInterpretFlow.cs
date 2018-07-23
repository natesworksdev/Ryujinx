using ChocolArm64.Decoder;
using ChocolArm64.Decoder32;
using ChocolArm64.Memory;
using ChocolArm64.State;

using static ChocolArm64.Instruction32.A32InstInterpretHelper;

namespace ChocolArm64.Instruction32
{
    static partial class A32InstInterpret
    {
        public static void B(AThreadState State, AMemory Memory, AOpCode OpCode)
        {
            A32OpCodeBImmAl Op = (A32OpCodeBImmAl)OpCode;

            if (IsConditionTrue(State, Op.Cond))
            {
                BranchWritePc(State, GetPc(State) + (uint)Op.Imm);
            }
        }

        public static void Bl(AThreadState State, AMemory Memory, AOpCode OpCode)
        {
            Blx_Imm(State, Memory, OpCode, false);
        }

        public static void Blx(AThreadState State, AMemory Memory, AOpCode OpCode)
        {
            switch (OpCode)
            {
                case A32OpCodeBImmAl Op:
                    Blx_Imm(State, Memory, Op, true);
                    break;
                case A32OpCodeBReg Op:
                    Blx_Reg(State, Memory, Op);
                    break;
            }
        }

        private static void Blx_Imm(AThreadState State, AMemory Memory, AOpCode OpCode, bool X)
        {
            A32OpCodeBImmAl Op = (A32OpCodeBImmAl)OpCode;

            if (IsConditionTrue(State, Op.Cond))
            {
                uint Pc = GetPc(State);

                if (State.Thumb)
                {
                    State.R14 = Pc | 1;
                }
                else
                {
                    State.R14 = Pc - 4U;
                }

                if (X)
                {
                    State.Thumb = !State.Thumb;
                }

                if (!State.Thumb)
                {
                    Pc &= ~3U;
                }

                BranchWritePc(State, Pc + (uint)Op.Imm);
            }
        }

        private static void Blx_Reg(AThreadState State, AMemory Memory, AOpCode OpCode)
        {
            A32OpCodeBReg Op = (A32OpCodeBReg)OpCode;

            if (IsConditionTrue(State, Op.Cond))
            {
                uint Pc = GetPc(State);

                if (State.Thumb)
                {
                    State.R14 = (Pc - 2U) | 1;
                }
                else
                {
                    State.R14 = Pc - 4U;
                }

                BXWritePC(State, GetReg(State, Op.Rm));
            }
        }

        private static void BranchWritePc(AThreadState State, uint Pc)
        {
            State.R15 = State.Thumb
                ? Pc & ~1U
                : Pc & ~3U;
        }

        private static void BXWritePC(AThreadState State, uint Pc)
        {
            if ((Pc & 1U) == 1)
            {
                State.Thumb = true;
                State.R15 = Pc & ~1U;
            }
            else
            {
                State.Thumb = false;
                // For branches to an unaligned PC counter in A32 state, the processor takes the branch
                // and does one of:
                // * Forces the address to be aligned
                // * Leaves the PC unaligned, meaning the target generates a PC Alignment fault. 
                if ((Pc & 2U) == 2 /*&& ConstrainUnpredictableBool()*/)
                {
                    State.R15 = Pc & ~2U;
                }
            }
        }
    }
}