using Ryujinx.Graphics.Shader.Instructions;

namespace Ryujinx.Graphics.Shader.Decoders
{
    class OpCodeNop : OpCode
    {
        public OpCodeNop(InstEmitter emitter, ulong address, long opCode) : base(emitter, address, opCode) {}
    }
}
