using System;
using System.Collections.Generic;
using System.Text;

namespace Ryujinx.Graphics.Shader.Decoders
{
    class OpCodeNop
    {
        public OpCodeNop(InstEmitter emitter, ulong address, long opCode) : base(emitter, address, opCode)
        {

        }
    }
}
