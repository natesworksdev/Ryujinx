using System;
using System.Collections.Generic;
using System.Text;

namespace ARMeilleure.Decoders
{
    class OpCode32SimdVext : OpCode32SimdReg
    {
        public int Immediate { get; private set; }
        public OpCode32SimdVext(InstDescriptor inst, ulong address, int opCode) : base(inst, address, opCode)
        {
            Immediate = (opCode >> 8) & 0xf;
            Size = 0;
        }
    }
}
