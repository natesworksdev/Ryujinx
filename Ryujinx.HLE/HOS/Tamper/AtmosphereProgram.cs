using Ryujinx.HLE.HOS.Kernel.Process;
using Ryujinx.HLE.HOS.Services.Hid;
using Ryujinx.HLE.HOS.Tamper.Operations;
using Ryujinx.Memory;
using System;

namespace Ryujinx.HLE.HOS.Tamper
{
    public class AtmosphereProgram : ITamperProgram
    {
        private Parameter<long> _pressedKeys;
        private IOperation _entryPoint;

        public ITamperedProcess Process { get; }

        public AtmosphereProgram(ITamperedProcess process, Parameter<long> pressedKeys, IOperation entryPoint)
        {
            Process = process;
            _pressedKeys = pressedKeys;
            _entryPoint = entryPoint;
        }

        public void Execute(ControllerKeys pressedKeys)
        {
            _pressedKeys.Value = (long)pressedKeys;
            _entryPoint.Execute();
        }
    }
}
