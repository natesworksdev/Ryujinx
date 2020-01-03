using System;
using System.Collections.Generic;
using System.Text;

namespace ARMeilleure.Decoders
{
    class OpCode32SimdMemSingle : OpCode32, IOpCode32Simd
    {
        public int Vd { get; private set; }
        public int Rn { get; private set; }
        public int Rm { get; private set; }
        public int IndexAlign { get; private set; }
        public int Index => IndexAlign >> (1 + Size);
        public bool WBack { get; private set; }
        public bool RegisterIndex { get; private set; }
        public int Size { get; private set; }
        public int Elems => GetBytesCount() >> Size;

        public int Increment => (((IndexAlign >> Size) & 1) == 0) ? 1 : 2;
        public OpCode32SimdMemSingle(InstDescriptor inst, ulong address, int opCode) : base(inst, address, opCode)
        {
            Vd = (opCode >> 12) & 0xf;
            Vd |= (opCode >> 18) & 0x10;

            Size = (opCode >> 10) & 0x3;

            IndexAlign = (opCode >> 4) & 0xf;
            Rm = (opCode >> 0) & 0xf;
            Rn = (opCode >> 16) & 0xf;

            WBack = Rm != 15;
            RegisterIndex = (Rm != 15 && Rm != 13);
        }
    }
}
