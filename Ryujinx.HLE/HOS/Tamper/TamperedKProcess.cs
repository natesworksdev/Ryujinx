using Ryujinx.Common.Logging;
using Ryujinx.HLE.Exceptions;
using Ryujinx.HLE.HOS.Kernel.Process;
using System.Runtime.CompilerServices;

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

        private void AssertMemoryRegion<T>(ulong va) where T : unmanaged
        {
            ulong size = (ulong)Unsafe.SizeOf<T>();
            if (!_process.CpuMemory.IsRangeMapped(va, size))
            {
                throw new TamperExecutionException($"Unmapped memory access of {size} bytes at 0x{va:X16}");
            }
        }

        public T ReadMemory<T>(ulong va) where T : unmanaged
        {
            AssertMemoryRegion<T>(va);
            return _process.CpuMemory.Read<T>(va);
        }

        public void WriteMemory<T>(ulong va, T value) where T : unmanaged
        {
            AssertMemoryRegion<T>(va);
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