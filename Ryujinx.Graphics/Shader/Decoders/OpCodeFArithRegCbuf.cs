using Ryujinx.Graphics.Shader.Instructions;

namespace Ryujinx.Graphics.Shader.Decoders
{
    class OpCodeFArithRegCbuf : OpCodeFArithReg, IOpCodeRegCbuf
    {
        public int Offset { get; }
        public int Slot   { get; }

        public OpCodeFArithRegCbuf(InstEmitter emitter, ulong address, long opCode) : base(emitter, address, opCode)
        {
            Offset = opCode.Extract(20, 14);
            Slot   = opCode.Extract(34, 5);

            Rb = new Register(opCode.Extract(39, 8), RegisterType.Gpr);
        }
    }
}