using ARMeilleure.Instructions;

namespace ARMeilleure.Decoders
{
    class OpCode32SimdCvtFFixed : OpCode32Simd
    {
        public int Fbits { get; protected set; }

        public new static OpCode Create(InstDescriptor inst, ulong address, int opCode) => new OpCode32SimdCvtFFixed(inst, address, opCode);

        public OpCode32SimdCvtFFixed(InstDescriptor inst, ulong address, int opCode) : base(inst, address, opCode)
        {
            Opc = (opCode >> 8) & 0x1;

            Size = 2;
            Fbits = 64 - ((opCode >> 16) & 0x3f);

            if (((opCode >> 21) & 0x1) == 0)
            {
                Instruction = InstDescriptor.Undefined;
            }

            if (DecoderHelper.VectorArgumentsInvalid(Q, Vd, Vm))
            {
                Instruction = InstDescriptor.Undefined;
            }
        }
    }
}
