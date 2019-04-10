using Ryujinx.Graphics.Shader.Instructions;

namespace Ryujinx.Graphics.Shader.Decoders
{
    class OpCodeAluImm2x10 : OpCodeAlu, IOpCodeImm
    {
        public int Immediate { get; }

        public OpCodeAluImm2x10(InstEmitter emitter, ulong address, long opCode) : base(emitter, address, opCode)
        {
            int immH0 = opCode.Extract(20, 9);
            int immH1 = opCode.Extract(30, 9);

            bool negateH0 = opCode.Extract(29);
            bool negateH1 = opCode.Extract(56);

            if (negateH0)
            {
                immH0 |= 1 << 9;
            }

            if (negateH1)
            {
                immH1 |= 1 << 9;
            }

            Immediate = immH1 << 22 | immH0 << 6;
        }
    }
}