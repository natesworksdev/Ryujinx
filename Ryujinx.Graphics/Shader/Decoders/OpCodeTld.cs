using Ryujinx.Graphics.Shader.Instructions;

namespace Ryujinx.Graphics.Shader.Decoders
{
    class OpCodeTld : OpCodeTexture
    {
        public bool IsMultisample { get; }

        public OpCodeTld(InstEmitter emitter, ulong address, long opCode) : base(emitter, address, opCode)
        {
            IsMultisample = opCode.Extract(50);

            LodMode = (TextureLodMode)(opCode.Extract(55, 1) + TextureLodMode.LodZero);
        }
    }
}