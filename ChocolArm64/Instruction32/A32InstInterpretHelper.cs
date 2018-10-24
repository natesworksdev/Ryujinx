using ChocolArm64.Decoder;
using ChocolArm64.State;
using System;

namespace ChocolArm64.Instruction32
{
    static class A32InstInterpretHelper
    {
        public static bool IsConditionTrue(AThreadState state, ACond cond)
        {
            switch (cond)
            {
                case ACond.Eq:    return  state.Zero;
                case ACond.Ne:    return !state.Zero;
                case ACond.GeUn:  return  state.Carry;
                case ACond.LtUn:  return !state.Carry;
                case ACond.Mi:    return  state.Negative;
                case ACond.Pl:    return !state.Negative;
                case ACond.Vs:    return  state.Overflow;
                case ACond.Vc:    return !state.Overflow;
                case ACond.GtUn:  return  state.Carry    && !state.Zero;
                case ACond.LeUn:  return !state.Carry    &&  state.Zero;
                case ACond.Ge:    return  state.Negative ==  state.Overflow;
                case ACond.Lt:    return  state.Negative !=  state.Overflow;
                case ACond.Gt:    return  state.Negative ==  state.Overflow && !state.Zero;
                case ACond.Le:    return  state.Negative !=  state.Overflow &&  state.Zero;
            }

            return true;
        }

        public unsafe static uint GetReg(AThreadState state, int reg)
        {
            if ((uint)reg > 15)
            {
                throw new ArgumentOutOfRangeException(nameof(reg));
            }

            fixed (uint* ptr = &state.R0)
            {
                return *(ptr + reg);
            }
        }

        public unsafe static void SetReg(AThreadState state, int reg, uint value)
        {
            if ((uint)reg > 15)
            {
                throw new ArgumentOutOfRangeException(nameof(reg));
            }

            fixed (uint* ptr = &state.R0)
            {
                *(ptr + reg) = value;
            }
        }

        public static uint GetPc(AThreadState state)
        {
            //Due to the old fetch-decode-execute pipeline of old ARM CPUs,
            //the PC is 4 or 8 bytes (2 instructions) ahead of the current instruction.
            return state.R15 + (state.Thumb ? 2U : 4U);
        }
    }
}