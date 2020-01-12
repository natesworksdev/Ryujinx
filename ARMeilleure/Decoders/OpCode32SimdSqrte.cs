using System;
using System.Collections.Generic;
using System.Text;

namespace ARMeilleure.Decoders
{
    class OpCode32SimdSqrte : OpCode32Simd
    {
        public OpCode32SimdSqrte(InstDescriptor inst, ulong address, int opCode) : base(inst, address, opCode)
        {
            Size = (opCode >> 18) & 0x1; //fvector size: 1 for 16 bit
            F = ((opCode >> 8) & 0x1) != 0;
        }
    }
}
