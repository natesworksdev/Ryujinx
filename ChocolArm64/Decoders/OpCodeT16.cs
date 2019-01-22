using ChocolArm64.Instructions;

namespace ChocolArm64.Decoders
{
    class OpCodeT16 : OpCode32
    {
        public OpCodeT16(Inst inst, long position, int opCode) : base(inst, position, opCode)
        {
            Cond = Cond.Al;

            OpCodeSizeInBytes = 2;
        }
    }
}