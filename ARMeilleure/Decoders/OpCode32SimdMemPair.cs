using System;
using System.Collections.Generic;
using System.Text;

namespace ARMeilleure.Decoders
{
    class OpCode32SimdMemPair : OpCode32, IOpCode32Simd
    {
        private static Dictionary<int, int> RegsMap = new Dictionary<int, int>()
        {
            { 0b0111, 1 },
            { 0b1010, 2 },
            { 0b0110, 3 },
            { 0b0010, 4 },

            { 0b1000, 1 },
            { 0b1001, 1 },
            { 0b0011, 2 },
        };
        public int Vd { get; private set; }
        public int Rn { get; private set; }
        public int Rm { get; private set; }
        public int Align { get; private set; }
        public bool WBack { get; private set; }
        public bool RegisterIndex { get; private set; }
        public int Size { get; private set; }
        public int Elems => GetBytesCount() >> Size;
        public int Regs { get; private set; }
        public int Increment { get; private set; }
        public OpCode32SimdMemPair(InstDescriptor inst, ulong address, int opCode) : base(inst, address, opCode)
        {
            Vd = (opCode >> 12) & 0xf;
            Vd |= (opCode >> 18) & 0x10;

            Size = (opCode >> 6) & 0x3;

            Align = (opCode >> 4) & 0x3;
            Rm = (opCode >> 0) & 0xf;
            Rn = (opCode >> 16) & 0xf;

            WBack = Rm != 15;
            RegisterIndex = (Rm != 15 && Rm != 13);

            int regs;
            if (!RegsMap.TryGetValue((opCode >> 8) & 0xf, out regs))
            {
                regs = 1;
            }
            Regs = regs;

            Increment = Math.Min(Regs, ((opCode >> 8) & 0x1) + 1);
        }
    }
}
