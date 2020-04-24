using ARMeilleure.State;
using Ryujinx.HLE.HOS.Kernel.Process;
using Ryujinx.HLE.HOS.Kernel.Threading;
using System;

namespace Ryujinx.HLE.HOS.Kernel.SupervisorCall
{
    partial class SvcHandler
    {
        private readonly Switch   _device;
        private readonly KProcess _process;
        private readonly Horizon  _system;

        public SvcHandler(Switch device, KProcess process)
        {
            _device  = device;
            _process = process;
            _system  = device.System;
        }

        public void SvcCall(object sender, InstExceptionEventArgs e)
        {
            ExecutionContext context = (ExecutionContext)sender;

            var svcFunc = context.IsAarch32 ? SvcTable.SvcTable32[e.Id] : SvcTable.SvcTable64[e.Id];

            if (svcFunc == null)
            {
                throw new NotImplementedException($"SVC 0x{e.Id:X4} is not implemented.");
            }

            svcFunc(this, context);

            PostSvcHandler();
        }

        private void PostSvcHandler()
        {
            KThread currentThread = _system.Scheduler.GetCurrentThread();

            currentThread.HandlePostSyscall();
        }
    }
}