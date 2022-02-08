using Ryujinx.Memory;

namespace Ryujinx.HLE.Debugger
{
    public interface IDebuggableProcess
    {
        public void DebugStopAllThreads();
        public ulong[] DebugGetThreadUids();
        public Ryujinx.Cpu.IExecutionContext DebugGetThreadContext(ulong threadUid);
        public IVirtualMemoryManager CpuMemory { get; }
    }
}
