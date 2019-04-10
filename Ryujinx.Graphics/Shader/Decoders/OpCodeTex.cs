using Ryujinx.Graphics.Shader.Instructions;

namespace Ryujinx.Graphics.Shader.Decoders
{
    class OpCodeTex : OpCodeTexture
    {
        public bool HasDepthCompare { get; }

        public OpCodeTex(InstEmitter emitter, ulong address, long opCode) : base(emitter, address, opCode)
        {
            HasDepthCompare = opCode.Extract(50);
        }
    }
}