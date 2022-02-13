using Ryujinx.Memory;

namespace Ryujinx.HLE.Debugger
{
    public interface IDebuggableProcess
    {
        void DebugStopAllThreads();
        ulong[] DebugGetThreadUids();
        Ryujinx.Cpu.IExecutionContext DebugGetThreadContext(ulong threadUid);
        IVirtualMemoryManager CpuMemory { get; }
        void InvalidateCacheRegion(ulong address, ulong size);
    }
}
