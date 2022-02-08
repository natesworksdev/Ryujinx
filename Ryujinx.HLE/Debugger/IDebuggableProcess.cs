using Ryujinx.Memory;

namespace Ryujinx.HLE.Debugger
{
    public interface IDebuggableProcess
    {
        public void DebugStopAllThreads();
        public long[] DebugGetThreadUids();
        public ARMeilleure.State.ExecutionContext DebugGetThreadContext(long threadUid);
        public IVirtualMemoryManager CpuMemory { get; }
    }
}
