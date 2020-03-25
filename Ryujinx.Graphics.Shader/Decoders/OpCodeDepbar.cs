using Ryujinx.Graphics.Shader.Instructions;

namespace Ryujinx.Graphics.Shader.Decoders
{
    class OpCodeDepbar : OpCode
    {
        public OpCodeDepbar(InstEmitter emitter, ulong address, long opCode) : base(emitter, address, opCode) {}
    }
}
