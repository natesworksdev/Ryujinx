using ChocolArm64.Decoders;
using ChocolArm64.Decoders32;
using ChocolArm64.Memory;
using ChocolArm64.State;

using static ChocolArm64.Instructions32.A32InstInterpretHelper;

namespace ChocolArm64.Instructions32
{
    static partial class A32InstInterpret
    {
        public static void B(CpuThreadState state, MemoryManager memory, OpCode64 opCode)
        {
            A32OpCodeBImmAl op = (A32OpCodeBImmAl)opCode;

            if (IsConditionTrue(state, op.Cond))
            {
                BranchWritePc(state, GetPc(state) + (uint)op.Imm);
            }
        }

        public static void Bl(CpuThreadState state, MemoryManager memory, OpCode64 opCode)
        {
            Blx_Imm(state, memory, opCode, false);
        }

        public static void Blx(CpuThreadState state, MemoryManager memory, OpCode64 opCode)
        {
            switch (opCode)
            {
                case A32OpCodeBImmAl op:
                    Blx_Imm(state, memory, op, true);
                    break;
                case A32OpCodeBReg op:
                    Blx_Reg(state, memory, op);
                    break;
            }
        }

        private static void Blx_Imm(CpuThreadState state, MemoryManager memory, OpCode64 opCode, bool x)
        {
            A32OpCodeBImmAl op = (A32OpCodeBImmAl)opCode;

            if (IsConditionTrue(state, op.Cond))
            {
                uint pc = GetPc(state);

                if (state.Thumb)
                {
                    state.R14 = pc | 1;
                }
                else
                {
                    state.R14 = pc - 4U;
                }

                if (x)
                {
                    state.Thumb = !state.Thumb;
                }

                if (!state.Thumb)
                {
                    pc &= ~3U;
                }

                BranchWritePc(state, pc + (uint)op.Imm);
            }
        }

        private static void Blx_Reg(CpuThreadState state, MemoryManager memory, OpCode64 opCode)
        {
            A32OpCodeBReg op = (A32OpCodeBReg)opCode;
            if (IsConditionTrue(state, op.Cond))
            {
                uint pc = GetPc(state);
                if (state.Thumb)
                {
                    state.R14 = (pc - 2U) | 1;
                }
                else
                {
                    state.R14 = pc - 4U;
                }
                BXWritePC(state, GetReg(state, op.Rm));
            }
        }

        private static void BranchWritePc(CpuThreadState state, uint pc)
        {
            state.R15 = state.Thumb
                ? pc & ~1U
                : pc & ~3U;
        }

        private static void BXWritePC(CpuThreadState state, uint pc)
        {
            if ((pc & 1U) == 1)
            {
                state.Thumb = true;
                state.R15 = pc & ~1U;
            }
            else
            {
                state.Thumb = false;
                // For branches to an unaligned PC counter in A32 state, the processor takes the branch
                // and does one of:
                // * Forces the address to be aligned
                // * Leaves the PC unaligned, meaning the target generates a PC Alignment fault.
                if ((pc & 2U) == 2 /*&& ConstrainUnpredictableBool()*/)
                {
                    state.R15 = pc & ~2U;
                }
            }
        }
    }
}