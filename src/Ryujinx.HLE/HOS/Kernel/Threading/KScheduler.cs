using Ryujinx.Common;
using Ryujinx.HLE.HOS.Kernel.Process;
using System;
using System.Numerics;
using System.Threading;

namespace Ryujinx.HLE.HOS.Kernel.Threading
{
    partial class KScheduler: IDisposable
    {
        public const int PrioritiesCount = 64;
        public const int CpuCoresCount   = 4;

        private const int RoundRobinTimeQuantumMs = 10;

        private static readonly int[] PreemptionPriorities = new int[] { 59, 59, 59, 63 };

        private static readonly int[] _srcCoresHighestPrioThreads = new int[CpuCoresCount];

        private readonly KernelContext _context;
        private readonly int _coreId;

        public long LastContextSwitchTime { get; private set; }
        
        // TODO: Implement this properly.
        public long TotalIdleTimeTicks => 0;

        public KScheduler(KernelContext context, int coreId)
        {
            _context = context;
            _coreId = coreId;
        }
        
        public void Dispose()
        {
        }
    }
}
