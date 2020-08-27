using Ryujinx.Common.Logging;
using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Kernel.Common;
using System.Runtime.CompilerServices;

namespace Ryujinx.Horizon.Kernel.Svc
{
    public partial class Syscall
    {
        private readonly KernelContextInternal _context;

        internal Syscall(KernelContextInternal context)
        {
            _context = context;
        }

        private Result CheckResult(Result result, [CallerMemberName] string svcName = null)
        {
            _context.Scheduler.GetCurrentThread().HandlePostSyscall();

            // Filter out some errors that are expected to occur under normal operation,
            // this avoids false warnings.
            if (result.IsFailure &&
                result != KernelResult.TimedOut &&
                result != KernelResult.Cancelled &&
                result != KernelResult.PortRemoteClosed &&
                result != KernelResult.InvalidState)
            {
                Logger.Warning?.Print(LogClass.KernelSvc, $"{svcName} returned error {result}.");
            }
            else
            {
                Logger.Debug?.Print(LogClass.KernelSvc, $"{svcName} returned result {result}.");
            }

            return result;
        }
    }
}
