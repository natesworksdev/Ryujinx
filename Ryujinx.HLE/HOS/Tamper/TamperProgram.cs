using Ryujinx.HLE.HOS.Tamper.Operations;
using Ryujinx.Memory;

namespace Ryujinx.HLE.HOS.Tamper
{
    public class TamperProgram
    {
        public Parameter<IVirtualMemoryManager> Memory { get; private set; }
        public IOperation EntryPoint { get; private set; }

        public TamperProgram(Parameter<IVirtualMemoryManager> memory, IOperation entryPoint)
        {
            Memory = memory;
            EntryPoint = entryPoint;
        }
    }
}
