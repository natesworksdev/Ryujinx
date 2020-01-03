using System;
using System.Collections.Generic;
using System.Text;

namespace ARMeilleure.Decoders
{
    class OpCode32SimdImm44 : OpCode32, IOpCode32SimdImm
    {
        public int Vd { get; private set; }
        public long Immediate { get; private set; }
        public int Size { get; private set; }
        public int Elems { get; private set; }
        public OpCode32SimdImm44(InstDescriptor inst, ulong address, int opCode) : base(inst, address, opCode)
        {
            Vd = (opCode >> 12) & 0xf;
            Vd |= (opCode >> 18) & 0x10;

            Size = ((opCode >> 8) & 0x3) + 1;

            long imm;

            imm = ((uint)opCode >> 0) & 0xf;
            imm |= ((uint)opCode >> 12) & 0xf0;

            Immediate = OpCodeSimdHelper.VFPExpandImm(imm, 8 << (Size));

            RegisterSize = (Size == 3) ? RegisterSize.Simd64 : RegisterSize.Simd32;
            Elems = 1;
        }
    }
}
