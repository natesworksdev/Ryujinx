using Ryujinx.Graphics.Shader.Instructions;

namespace Ryujinx.Graphics.Shader.Decoders
{
    class OpCodeExit : OpCode
    {
        public OpCodeExit(InstEmitter emitter, ulong address, long opCode) : base(emitter, address, opCode)
        {

        }
    }
}