using ARMeilleure.State;
using Ryujinx.HLE.HOS.Kernel.Process;
using Ryujinx.HLE.HOS.Kernel.Threading;
using System;

namespace Ryujinx.HLE.HOS.Kernel.SupervisorCall
{
    partial class SvcHandler
    {
        private Switch        _device;
        private KProcess      _process;
        private KernelContext _context;

        public SvcHandler(Switch device, KProcess process, KernelContext context)
        {
            _device  = device;
            _process = process;
            _context = context;
        }

        public void SvcCall(object sender, InstExceptionEventArgs e)
        {
            ExecutionContext context = (ExecutionContext)sender;

            Action<SvcHandler, ExecutionContext> svcFunc = context.IsAarch32 ? SvcTable.SvcTable32[e.Id] : SvcTable.SvcTable64[e.Id];

            if (svcFunc == null)
            {
                throw new NotImplementedException($"SVC 0x{e.Id:X4} is not implemented.");
            }

            svcFunc(this, context);

            PostSvcHandler();
        }

        private void PostSvcHandler()
        {
            KThread currentThread = _context.Scheduler.GetCurrentThread();

            currentThread.HandlePostSyscall();
        }
    }
}