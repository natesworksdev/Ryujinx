using System;
using System.Collections.Generic;
using System.Text;

namespace ARMeilleure.Decoders
{
    class OpCode32System : OpCode32
    {
        public int Opc1 { get; internal set; }
        public int CRn { get; internal set; }
        public int Rt { get; internal set; }
        public int Opc2 { get; internal set; }
        public int CRm { get; internal set; }

        public int Coproc { get; internal set; }

        public OpCode32System(InstDescriptor inst, ulong address, int opCode) : base(inst, address, opCode)
        {
            Opc1 = (opCode >> 21) & 0x7;
            CRn = (opCode >> 16) & 0xf;
            Rt = (opCode >> 12) & 0xf;
            Opc2 = (opCode >> 5) & 0x7;
            CRm = (opCode >> 0) & 0xf;

            Coproc = (opCode >> 8) & 0xf;
        }
    }
}
