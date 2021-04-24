using System;
using ARMeilleure.Instructions;

namespace ARMeilleure.Decoders
{
    class OpCode32SimdCvtFFixed : OpCode32Simd
    {
        public bool M { get; protected set; }
        public bool D { get; protected set; }
        public int Fbits { get; protected set; }

        public new static OpCode Create(InstDescriptor inst, ulong address, int opCode) => new OpCode32SimdCvtFFixed(inst, address, opCode);

        public OpCode32SimdCvtFFixed(InstDescriptor inst, ulong address, int opCode) : base(inst, address, opCode)
        {
            Opc = (opCode >> 8) & 0x1;
            M = ((opCode >> 5) & 0x1) == 1;
            D = ((opCode >> 22) & 0x1) == 1;

            Size = Opc == 1 ? 2 : 0; // EmitVectorUnaryOpF32 needs op.Size to be 0 while EmitVectorUnaryOpS/Zx32 wants it to be  2 so that we can iterate through all vector elements. This works with both 128 and 64 bit vectors.
            Fbits = (opCode >> 16) & 0x3f;

            if (DecoderHelper.VectorArgumentsInvalid(Q, Vd, Vm))
            {
                Instruction = InstDescriptor.Undefined;
            }
        }
    }
}
