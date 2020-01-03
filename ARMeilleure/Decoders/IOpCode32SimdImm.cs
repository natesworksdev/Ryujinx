using System;
using System.Collections.Generic;
using System.Text;

namespace ARMeilleure.Decoders
{
    interface IOpCode32SimdImm : IOpCode32Simd
    {
        public int Vd { get; }
        public long Immediate { get; }
    }
}
