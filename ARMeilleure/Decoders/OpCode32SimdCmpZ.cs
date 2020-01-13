using System;
using System.Collections.Generic;
using System.Text;

namespace ARMeilleure.Decoders
{
    class OpCode32SimdCmpZ : OpCode32Simd
    {
        public OpCode32SimdCmpZ(InstDescriptor inst, ulong address, int opCode) : base(inst, address, opCode)
        {
            Size = (opCode >> 18) & 0x3; //fvector size: 1 for 16 bit
        }
    }
}
