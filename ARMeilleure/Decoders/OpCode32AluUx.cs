using ARMeilleure.State;

namespace ARMeilleure.Decoders
{
    class OpCode32AluUx : OpCode32AluReg, IOpCode32AluUx
    {
        public int Rotate { get; }
        public int RotateBits => Rotate * 8;
        public bool Add => Rn != RegisterAlias.Aarch32Pc;

        public new static OpCode Create(InstDescriptor inst, ulong address, int opCode, bool inITBlock) => new OpCode32AluUx(inst, address, opCode, inITBlock);

        public OpCode32AluUx(InstDescriptor inst, ulong address, int opCode, bool inITBlock) : base(inst, address, opCode, inITBlock)
        {
            Rotate = (opCode >> 10) & 0x3;
        }
    }
}
