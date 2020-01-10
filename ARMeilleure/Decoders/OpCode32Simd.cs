using System;
using System.Collections.Generic;
using System.Text;

namespace ARMeilleure.Decoders
{
    class OpCode32Simd : OpCode32, IOpCode32Simd
    {
        public int Vd { get; private set; }
        public int Vm { get; private set; }
        public int Opc { get; private set; }
        public int Size { get; protected set; }
        public bool Q { get; private set; }
        public bool F { get; private set; }
        public bool U { get; private set; }
        public int Elems => GetBytesCount() >> ((Size == 1) ? 1 : 2);

        public OpCode32Simd(InstDescriptor inst, ulong address, int opCode) : base(inst, address, opCode)
        {
            Size = (opCode >> 20) & 0x1; //fvector size: 1 for 16 bit
            Q = ((opCode >> 6) & 0x1) != 0;
            F = ((opCode >> 10) & 0x1) != 0;
            U = ((opCode >> 24) & 0x1) != 0;

            RegisterSize = Q ? RegisterSize.Simd128 : RegisterSize.Simd64;

            Vd = ((opCode >> 18) & 0x10) | ((opCode >> 12) & 0xf);
            Vm = ((opCode >> 1) & 0x10) | ((opCode >> 0) & 0xf);
        }
    }
}
