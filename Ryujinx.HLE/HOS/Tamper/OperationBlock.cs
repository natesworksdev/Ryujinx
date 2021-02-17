using Ryujinx.HLE.HOS.Tamper.Atmosphere.Operations;
using System.Collections.Generic;

namespace Ryujinx.HLE.HOS.Tamper
{
    struct OperationBlock
    {
        public byte[] BaseInstruction { get; }
        public List<IOperation> Operations { get; }

        public OperationBlock(byte[] baseInstruction)
        {
            BaseInstruction = baseInstruction;
            Operations = new List<IOperation>();
        }
    }
}
