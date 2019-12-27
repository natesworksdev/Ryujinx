using System;
using System.Collections.Generic;
using System.Text;

namespace ARMeilleure.Decoders
{
    class OpCode32AluMla : OpCode32, IOpCode32AluReg
    {
        public int Rn { get; private set; }
        public int Rm { get; private set; }
        public int Ra { get; private set; }
        public int Rd { get; private set; }

        public bool SetFlags { get; private set; }

        public OpCode32AluMla(InstDescriptor inst, ulong address, int opCode) : base(inst, address, opCode)
        {
            Rn = (opCode >> 0) & 0xf;
            Rm = (opCode >> 8) & 0xf;
            Ra = (opCode >> 12) & 0xf;
            Rd = (opCode >> 16) & 0xf;

            SetFlags = ((opCode >> 20) & 1) != 0;
        }
    }
}
