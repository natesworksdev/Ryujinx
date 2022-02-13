using Ryujinx.Memory;

namespace Ryujinx.HLE.Debugger
{
    public interface IDebuggableProcess
    {
        void DebugStopAllThreads();
        long[] DebugGetThreadUids();
        ARMeilleure.State.ExecutionContext DebugGetThreadContext(long threadUid);
        IVirtualMemoryManager CpuMemory { get; }
        void InvalidateCacheRegion(ulong address, ulong size);
    }
}