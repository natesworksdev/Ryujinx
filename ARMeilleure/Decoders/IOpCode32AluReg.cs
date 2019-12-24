using System;
using System.Collections.Generic;
using System.Text;

namespace ARMeilleure.Decoders
{
    interface IOpCode32AluReg : IOpCode32Alu
    {
        public int Rm { get; }
    }
}
