using ChocolArm64.Instruction;
using ChocolArm64.State;

namespace ChocolArm64.Decoder
{
    class AOpCodeAlu : AOpCode, IaOpCodeAlu
    {
        public int Rd { get; protected set; }
        public int Rn { get; private   set; }

        public ADataOp DataOp { get; private set; }

        public AOpCodeAlu(AInst inst, long position, int opCode) : base(inst, position, opCode)
        {
            Rd     =           (opCode >>  0) & 0x1f;
            Rn     =           (opCode >>  5) & 0x1f;
            DataOp = (ADataOp)((opCode >> 24) & 0x3);

            RegisterSize = (opCode >> 31) != 0
                ? ARegisterSize.Int64
                : ARegisterSize.Int32;
        }
    }
}