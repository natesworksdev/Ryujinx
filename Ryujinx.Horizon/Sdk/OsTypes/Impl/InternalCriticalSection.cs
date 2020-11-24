using Ryujinx.Horizon.Kernel;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Ryujinx.Horizon.Sdk.OsTypes.Impl
{
    class InternalCriticalSection
    {
        private const int HasListenersMask = 0x40000000;

        private int _threadHandle;

        public void Enter()
        {
            int currentThreadHandle = 0;

            while (true)
            {
                int ownerHandle = Interlocked.CompareExchange(ref _threadHandle, currentThreadHandle, 0);

                if (ownerHandle == 0 || (ownerHandle & ~HasListenersMask) == currentThreadHandle)
                {
                    break;
                }

                if ((ownerHandle & HasListenersMask) != 0)
                {
                    KernelStatic.Syscall.ArbitrateLock(ownerHandle & ~HasListenersMask, 0, currentThreadHandle);
                }
                else
                {
                    int prevOwnerHandle = Interlocked.CompareExchange(ref _threadHandle, ownerHandle | HasListenersMask, ownerHandle);

                    if (prevOwnerHandle == ownerHandle)
                    {
                        KernelStatic.Syscall.ArbitrateLock(ownerHandle, 0, currentThreadHandle);
                    }
                }
            }
        }
    }
}
