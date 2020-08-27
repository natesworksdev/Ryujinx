using System;

namespace Ryujinx.Horizon.Kernel.Svc
{
    partial class SyscallHandler
    {
        private readonly Syscall32 _syscall32;
        private readonly Syscall64 _syscall64;

        public SyscallHandler(KernelContextInternal context)
        {
            _syscall32 = new Syscall32(context.Syscall);
            _syscall64 = new Syscall64(context.Syscall);
        }

        public void CallSvc(IThreadContext context, int id)
        {
            if (context.Is32Bit)
            {
                var svcFunc = SyscallTable.SvcTable32[id];

                if (svcFunc == null)
                {
                    throw new NotImplementedException($"SVC 0x{id:X4} is not implemented.");
                }

                svcFunc(_syscall32, context);
            }
            else
            {
                var svcFunc = SyscallTable.SvcTable64[id];

                if (svcFunc == null)
                {
                    throw new NotImplementedException($"SVC 0x{id:X4} is not implemented.");
                }

                svcFunc(_syscall64, context);
            }
        }
    }
}