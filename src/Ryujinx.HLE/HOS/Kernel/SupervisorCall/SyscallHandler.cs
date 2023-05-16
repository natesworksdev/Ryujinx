using Ryujinx.Cpu;
using Ryujinx.HLE.HOS.Kernel.Threading;
using System.Threading.Tasks;

namespace Ryujinx.HLE.HOS.Kernel.SupervisorCall
{
    partial class SyscallHandler
    {
        private readonly KernelContext _context;

        public SyscallHandler(KernelContext context)
        {
            _context = context;
        }

        public async Task SvcCall(IExecutionContext context, ulong address, int id)
        {
            KThread currentThread = KernelStatic.GetCurrentThread();

            if (currentThread.Owner != null &&
                currentThread.GetUserDisableCount() != 0 &&
                currentThread.Owner.PinnedThreads[currentThread.CurrentCore] == null)
            {
                _context.CriticalSection.Enter();

                currentThread.Owner.PinThread(currentThread);

                currentThread.SetUserInterruptFlag();

                _context.CriticalSection.Leave();
            }

            if (context.IsAarch32)
            {
                await SyscallDispatch.Dispatch32(_context.Syscall, context, id);
            }
            else
            {
                await SyscallDispatch.Dispatch64(_context.Syscall, context, id);
            }

            currentThread.HandlePostSyscall();
        }
    }
}