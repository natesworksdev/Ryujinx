using Ryujinx.Memory;
using Ryujinx.HLE.HOS.Kernel.Threading;

namespace Ryujinx.HLE.Debugger
{
    internal interface IDebuggableProcess
    {
        void DebugStopAllThreads();
        ulong[] DebugGetThreadUids();
        public KThread DebugGetThread(ulong threadUid);
        IVirtualMemoryManager CpuMemory { get; }
        void InvalidateCacheRegion(ulong address, ulong size);
    }
}
