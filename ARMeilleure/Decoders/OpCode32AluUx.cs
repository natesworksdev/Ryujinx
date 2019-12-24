using System;
using System.Collections.Generic;
using System.Text;

namespace ARMeilleure.Decoders
{
    class OpCode32AluUx : OpCode32AluReg, IOpCode32AluUx
    {
        public int Rotate { get; private set; }
        public int RotateBits => Rotate * 8;
        public bool Add => Rn != 15;

        public OpCode32AluUx(InstDescriptor inst, ulong address, int opCode) : base(inst, address, opCode)
        {
            Rotate = (opCode >> 10) & 0x3;
        }
    }
}
