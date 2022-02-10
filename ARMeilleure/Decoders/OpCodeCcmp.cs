using ARMeilleure.State;

namespace ARMeilleure.Decoders
{
    class OpCodeCcmp : OpCodeAlu, IOpCodeCond
    {
        public    int Nzcv { get; }
        protected int RmImm;

        public Condition Cond { get; }

        public new static OpCode Create(InstDescriptor inst, ulong address, int opCode, bool inITBlock) => new OpCodeCcmp(inst, address, opCode, inITBlock);

        public OpCodeCcmp(InstDescriptor inst, ulong address, int opCode, bool inITBlock) : base(inst, address, opCode, inITBlock)
        {
            int o3 = (opCode >> 4) & 1;

            if (o3 != 0)
            {
                Instruction = InstDescriptor.Undefined;

                return;
            }

            Nzcv  =             (opCode >>  0) & 0xf;
            Cond  = (Condition)((opCode >> 12) & 0xf);
            RmImm =             (opCode >> 16) & 0x1f;

            Rd = RegisterAlias.Zr;
        }
    }
}