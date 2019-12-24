using System;
using System.Collections.Generic;
using System.Text;

namespace ARMeilleure.Decoders
{
    interface IOpCode32AluUx : IOpCode32AluReg
    {
        public int RotateBits { get; }
        public bool Add { get; }
    }
}
