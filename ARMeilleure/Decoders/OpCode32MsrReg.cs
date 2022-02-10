using ARMeilleure.State;

namespace ARMeilleure.Decoders
{
    class OpCode32MsrReg : OpCode32
    {
        public bool R      { get; }
        public int  Mask   { get; }
        public int  Rd     { get; }
        public bool Banked { get; }
        public int  Rn     { get; }

        public new static OpCode Create(InstDescriptor inst, ulong address, int opCode, bool inITBlock) => new OpCode32MsrReg(inst, address, opCode, inITBlock);

        public OpCode32MsrReg(InstDescriptor inst, ulong address, int opCode, bool inITBlock) : base(inst, address, opCode, inITBlock)
        {
            R = ((opCode >> 22) & 1) != 0;
            Mask = (opCode >> 16) & 0xf;
            Rd = (opCode >> 12) & 0xf;
            Banked = ((opCode >> 9) & 1) != 0;
            Rn = (opCode >> 0) & 0xf;

            if (Rn == RegisterAlias.Aarch32Pc || Mask == 0)
            {
                Instruction = InstDescriptor.Undefined;
            }
        }
    }
}
