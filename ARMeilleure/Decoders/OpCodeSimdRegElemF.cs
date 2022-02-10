namespace ARMeilleure.Decoders
{
    class OpCodeSimdRegElemF : OpCodeSimdReg
    {
        public int Index { get; }

        public new static OpCode Create(InstDescriptor inst, ulong address, int opCode, bool inITBlock) => new OpCodeSimdRegElemF(inst, address, opCode, inITBlock);

        public OpCodeSimdRegElemF(InstDescriptor inst, ulong address, int opCode, bool inITBlock) : base(inst, address, opCode, inITBlock)
        {
            switch ((opCode >> 21) & 3) // sz:L
            {
                case 0: // H:0
                    Index = (opCode >> 10) & 2; // 0, 2

                    break;

                case 1: // H:1
                    Index = (opCode >> 10) & 2;
                    Index++; // 1, 3

                    break;

                case 2: // H
                    Index = (opCode >> 11) & 1; // 0, 1

                    break;

                default: Instruction = InstDescriptor.Undefined; break;
            }
        }
    }
}
