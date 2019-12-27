using System;
using System.Collections.Generic;
using System.Text;

namespace ARMeilleure.Decoders
{
    interface IOpCode32MemEx : IOpCode32Mem
    {
        public int Rd { get; }
    }
}
