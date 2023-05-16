using Ryujinx.HLE.HOS.Kernel.Common;
using Ryujinx.Horizon.Common;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ryujinx.HLE.HOS.Kernel.Threading
{
    class KSynchronization
    {
        private KernelContext _context;

        public KSynchronization(KernelContext context)
        {
            _context = context;
        }

        public async Task<(Result, int)> WaitFor(KSynchronizationObject[] syncObjs, long timeout)
        {
            var handleIndex = 0;

            // Check if objects are already signaled before waiting.
            for (int index = 0; index < syncObjs.Length; index++)
            {
                if (!syncObjs[index].IsSignaled())
                {
                    continue;
                }

                handleIndex = index;

                return (Result.Success, handleIndex);
            }

            if (timeout == 0)
            {
                return (KernelResult.TimedOut, handleIndex);
            }

            KThread currentThread = KernelStatic.GetCurrentThread();

            if (currentThread.TerminationRequested)
            {
                return (KernelResult.ThreadTerminating, 0);
            }
      
            // Timeout ns to ms
            int timeoutMs = (int)(timeout / 1_000_000);
            
            async Task YieldAsTask()
            {
                await Task.Yield();
            }
            
            // Get tasks of sync objs
            var tasks = new Task[syncObjs.Length + 2];
            for (int index = 0; index < syncObjs.Length; index++)
            {
                tasks[index] = syncObjs[index].WaitSignaled();
            }
            // Add timeout & cancel tasks
            // TODO: clean this up
            tasks[syncObjs.Length] = timeoutMs == 0 ? YieldAsTask() : Task.Delay(timeoutMs);
            tasks[syncObjs.Length + 1] = currentThread.WaitSyncCancel();
            
            // Async wait for any task to complete or timeout
            var task = await Task.WhenAny(tasks);
            handleIndex = Array.IndexOf(tasks, task);
            // If timeout task completed first, return timeout
            if (handleIndex == syncObjs.Length)
            {
                return (KernelResult.TimedOut, -1);
            } else if (handleIndex == syncObjs.Length + 1)
            {
                currentThread.ResetCancel();
                return (KernelResult.Cancelled, -1);
            }
            return (Result.Success, handleIndex);
        }
    }
}
