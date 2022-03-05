using ARMeilleure.Common;

namespace ARMeilleure.Decoders
{
    class OpCodeT32AluImm : OpCodeT32Alu, IOpCode32AluImm
    {
        public int Immediate { get; }

        public bool IsRotated { get; }

        public new static OpCode Create(InstDescriptor inst, ulong address, int opCode) => new OpCodeT32AluImm(inst, address, opCode);

        public OpCodeT32AluImm(InstDescriptor inst, ulong address, int opCode) : base(inst, address, opCode)
        {
            int imm8 = (opCode >> 0) & 0xff;
            int imm3 = (opCode >> 12) & 7;
            int imm1 = (opCode >> 26) & 1;

            int imm12 = imm8 | (imm3 << 8) | (imm1 << 11);

            if ((imm12 >> 10) == 0)
            {
                switch ((imm12 >> 8) & 3)
                {
                    case 0:
                        Immediate = imm8;
                        break;
                    case 1:
                        Immediate = imm8 | (imm8 << 16);
                        break;
                    case 2:
                        Immediate = (imm8 << 8) | (imm8 << 24);
                        break;
                    case 3:
                        Immediate = imm8 | (imm8 << 8) | (imm8 << 16) | (imm8 << 24);
                        break;
                }
                IsRotated = false;
            }
            else
            {
                int shift = imm12 >> 7;

                Immediate = BitUtils.RotateRight(0x80 | (imm12 & 0x7f), shift, 32);
                IsRotated = shift != 0;
            }
        }
    }
}