using System;
using System.Collections.Generic;
using System.Text;

namespace ARMeilleure.Decoders
{
    class OpCode32MemLdEx : OpCode32Mem, IOpCode32MemEx
    {
        public int Rd { get; internal set; }
        public OpCode32MemLdEx(InstDescriptor inst, ulong address, int opCode) : base(inst, address, opCode)
        {
            Rd = opCode & 0xf;
        }
    }
}
