using Ryujinx.Common.Logging;
using Ryujinx.HLE.HOS.Kernel.Process;

namespace Ryujinx.HLE.HOS.Tamper
{
    class TamperedKProcess : ITamperedProcess
    {
        private KProcess _process;

        public ProcessState State => _process.State;

        public TamperedKProcess(KProcess process)
        {
            this._process = process;
        }

        public T ReadMemory<T>(ulong va) where T : unmanaged
        {
            return _process.CpuMemory.Read<T>(va);
        }

        public void WriteMemory<T>(ulong va, T value) where T : unmanaged
        {
            _process.CpuMemory.Write(va, value);
        }

        public void PauseProcess()
        {
            Logger.Warning?.Print(LogClass.TamperMachine, "Process pausing is not supported!");
        }

        public void ResumeProcess()
        {
            Logger.Warning?.Print(LogClass.TamperMachine, "Process resuming is not supported!");
        }
    }
}