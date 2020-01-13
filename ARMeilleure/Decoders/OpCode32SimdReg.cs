using System;
using System.Collections.Generic;
using System.Text;

namespace ARMeilleure.Decoders
{
    class OpCode32SimdReg : OpCode32Simd
    {
        public int Vn { get; private set; }
        public int Index { get; private set; }

        public OpCode32SimdReg(InstDescriptor inst, ulong address, int opCode) : base(inst, address, opCode)
        {
            Vn = ((opCode >> 3) & 0x10) | ((opCode >> 16) & 0xf);
        }
    }
}
