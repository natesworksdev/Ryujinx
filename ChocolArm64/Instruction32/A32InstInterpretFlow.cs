using ChocolArm64.Decoder;
using ChocolArm64.Decoder32;
using ChocolArm64.Memory;
using ChocolArm64.State;

using static ChocolArm64.Instruction32.A32InstInterpretHelper;

namespace ChocolArm64.Instruction32
{
    internal static partial class A32InstInterpret
    {
        public static void B(AThreadState state, AMemory memory, AOpCode opCode)
        {
            A32OpCodeBImmAl op = (A32OpCodeBImmAl)opCode;

            if (IsConditionTrue(state, op.Cond))
            {
                BranchWritePc(state, GetPc(state) + (uint)op.Imm);
            }
        }

        public static void Bl(AThreadState state, AMemory memory, AOpCode opCode)
        {
            Blx(state, memory, opCode, false);
        }

        public static void Blx(AThreadState state, AMemory memory, AOpCode opCode)
        {
            Blx(state, memory, opCode, true);
        }

        public static void Blx(AThreadState state, AMemory memory, AOpCode opCode, bool x)
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

        private static void BranchWritePc(AThreadState state, uint pc)
        {
            state.R15 = state.Thumb
                ? pc & ~1U
                : pc & ~3U;
        }
    }
}