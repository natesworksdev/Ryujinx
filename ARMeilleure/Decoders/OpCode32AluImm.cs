using ARMeilleure.Common;

namespace ARMeilleure.Decoders
{
    class OpCode32AluImm : OpCode32Alu
    {
        public int Immediate { get; }

        public bool IsRotated { get; }

        public new static OpCode Create(InstDescriptor inst, ulong address, int opCode, bool inITBlock) => new OpCode32AluImm(inst, address, opCode, inITBlock);

        public OpCode32AluImm(InstDescriptor inst, ulong address, int opCode, bool inITBlock) : base(inst, address, opCode, inITBlock)
        {
            int value = (opCode >> 0) & 0xff;
            int shift = (opCode >> 8) & 0xf;

            Immediate = BitUtils.RotateRight(value, shift * 2, 32);

            IsRotated = shift != 0;
        }
    }
}