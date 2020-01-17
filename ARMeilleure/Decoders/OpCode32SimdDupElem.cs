using System;
using System.Collections.Generic;
using System.Text;

namespace ARMeilleure.Decoders
{
    class OpCode32SimdDupElem : BaseOpCode32Simd
    {
        public bool Q { get; private set; }

        public int Index { get; private set; }

        public OpCode32SimdDupElem(InstDescriptor inst, ulong address, int opCode) : base(inst, address, opCode)
        {
            var opc = (opCode >> 16) & 0xf;

            if ((opc & 0b1) == 1)
            {
                Size = 0;
                Index = (opc >> 1) & 0x7;
            }
            else if ((opc & 0b11) == 0b10)
            {
                Size = 1;
                Index = (opc >> 2) & 0x3;
            }
            else if ((opc & 0b111) == 0b100)
            {
                Size = 2;
                Index = (opc >> 3) & 0x1;
            }
            else
            {
                throw new Exception("Undefined");
            }

            Vd = ((opCode >> 18) & 0x10) | ((opCode >> 12) & 0xf);
            Vm = ((opCode >> 1) & 0x10) | ((opCode >> 0) & 0xf);
            Q = (opCode & (1 << 6)) != 0;
            if (Q) RegisterSize = RegisterSize.Simd128;
        }
    }
}
