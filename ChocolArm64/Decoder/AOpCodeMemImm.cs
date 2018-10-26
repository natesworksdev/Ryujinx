using ChocolArm64.Instruction;

namespace ChocolArm64.Decoder
{
    internal class AOpCodeMemImm : AOpCodeMem
    {
        public    long Imm      { get; protected set; }
        public    bool WBack    { get; protected set; }
        public    bool PostIdx  { get; protected set; }
        protected bool Unscaled { get; private   set; }

        private enum MemOp
        {
            Unscaled     = 0,
            PostIndexed  = 1,
            Unprivileged = 2,
            PreIndexed   = 3,
            Unsigned
        }

        public AOpCodeMemImm(AInst inst, long position, int opCode) : base(inst, position, opCode)
        {
            Extend64 = ((opCode >> 22) & 3) == 2;
            WBack    = ((opCode >> 24) & 1) == 0;

            //The type is not valid for the Unsigned Immediate 12-bits encoding,
            //because the bits 11:10 are used for the larger Immediate offset.
            MemOp type = WBack ? (MemOp)((opCode >> 10) & 3) : MemOp.Unsigned;

            PostIdx  = type == MemOp.PostIndexed;
            Unscaled = type == MemOp.Unscaled ||
                       type == MemOp.Unprivileged;

            //Unscaled and Unprivileged doesn't write back,
            //but they do use the 9-bits Signed Immediate.
            if (Unscaled)
            {
                WBack = false;
            }

            if (WBack || Unscaled)
            {
                Imm = (opCode << 43) >> 55;
            }
            else
            {
                Imm = ((opCode >> 10) & 0xfff) << Size;
            }
        }
    }
}