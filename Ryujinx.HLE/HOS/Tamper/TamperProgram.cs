using Ryujinx.HLE.HOS.Kernel.Process;
using Ryujinx.HLE.HOS.Tamper.Operations;
using Ryujinx.Memory;
using System;

namespace Ryujinx.HLE.HOS.Tamper
{
    internal class TamperProgram
    {
        public ITamperedProcess Process { get; }
        public Parameter<long> PressedKeys { get; }
        public IOperation EntryPoint { get; }

        public TamperProgram(ITamperedProcess process, Parameter<long> pressedKeys, IOperation entryPoint)
        {
            Process = process;
            PressedKeys = pressedKeys;
            EntryPoint = entryPoint;
        }
    }
}
