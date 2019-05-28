using ARMeilleure.Common;
using ARMeilleure.IntermediateRepresentation;
using System;
using System.Collections.Generic;

namespace ARMeilleure.CodeGen.RegisterAllocators
{
    class StackAllocator
    {
        private List<ulong> _masks;

        public StackAllocator()
        {
            _masks = new List<ulong>();
        }

        public int Allocate(OperandType type)
        {
            return Allocate(GetSizeInWords(type));
        }

        public int Free(int offset, OperandType type)
        {
            return Allocate(GetSizeInWords(type));
        }

        private int Allocate(int sizeInWords)
        {
            ulong requiredMask = (1UL << sizeInWords) - 1;

            for (int index = 0; ; index++)
            {
                ulong free = GetFreeMask(index);

                while ((int)free != 0)
                {
                    int freeBit = BitUtils.LowestBitSet((int)free);

                    ulong useMask = requiredMask << freeBit;

                    if ((free & useMask) == useMask)
                    {
                        free &= ~useMask;

                        SetFreeMask(index, free);

                        return -((index * 32 + freeBit) * 4 + sizeInWords * 4);
                    }

                    free &= ~useMask;
                }
            }
        }

        private void Free(int offset, int sizeInWords)
        {
            int index = offset / 32;

            ulong requiredMask = (1UL << sizeInWords) - 1;

            ulong freeMask = (requiredMask << (offset & 31)) - 1;

            SetFreeMask(index, GetFreeMask(index) & ~freeMask);
        }

        private ulong GetFreeMask(int index)
        {
            int hi = index >> 1;

            EnsureSize(hi);

            ulong mask;

            if ((index & 1) != 0)
            {
                EnsureSize(hi + 1);

                mask  = _masks[hi + 0] >> 32;
                mask |= _masks[hi + 1] << 32;
            }
            else
            {
                EnsureSize(hi);

                mask = _masks[hi];
            }

            return mask;
        }

        private void SetFreeMask(int index, ulong mask)
        {
            int hi = index >> 1;

            if ((index & 1) != 0)
            {
                EnsureSize(hi + 1);

                _masks[hi + 0] &= 0x00000000ffffffffUL;
                _masks[hi + 1] &= 0xffffffff00000000UL;

                _masks[hi + 0] |= mask << 32;
                _masks[hi + 1] |= mask >> 32;
            }
            else
            {
                EnsureSize(hi);

                _masks[hi] = mask;
            }
        }

        private void EnsureSize(int size)
        {
            while (size >= _masks.Count)
            {
                _masks.Add(ulong.MaxValue);
            }
        }

        private static int GetSizeInWords(OperandType type)
        {
            switch (type)
            {
                case OperandType.I32:
                case OperandType.FP32:
                    return 1;

                case OperandType.I64:
                case OperandType.FP64:
                    return 2;

                case OperandType.V128: return 4;
            }

            throw new ArgumentException($"Invalid operand type \"{type}\".");
        }
    }
}