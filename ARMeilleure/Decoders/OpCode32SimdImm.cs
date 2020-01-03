using System;
using System.Collections.Generic;
using System.Text;

namespace ARMeilleure.Decoders
{
    class OpCode32SimdImm : OpCode32, IOpCode32SimdImm
    {
        public int Vd { get; private set; }
        public bool Q { get; private set; }
        public long Immediate { get; private set; }
        public int Size { get; private set; }
        public int Elems => GetBytesCount() >> Size;
        public OpCode32SimdImm(InstDescriptor inst, ulong address, int opCode) : base(inst, address, opCode)
        {
            Vd = (opCode >> 12) & 0xf;
            Vd |= (opCode >> 18) & 0x10;
            
            Q = ((opCode >> 6) & 0x1) > 0;

            int cMode = (opCode >> 8) & 0xf;
            int op = (opCode >> 5) & 0x1;

            long imm;

            imm = ((uint)opCode >> 0) & 0xf;
            imm |= ((uint)opCode >> 12) & 0x70;
            imm |= ((uint)opCode >> 17) & 0x80;

            Tuple<long, int> parsedImm = OpCodeSimdHelper.GetSimdImmediateAndSize(cMode, op, imm, fpBaseSize: 2);

            Immediate = parsedImm.Item1;
            Size = parsedImm.Item2;

            RegisterSize = Q ? RegisterSize.Simd128 : RegisterSize.Simd64;
        }
    }
}
