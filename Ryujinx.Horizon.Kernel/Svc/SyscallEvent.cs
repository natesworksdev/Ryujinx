using Ryujinx.Common;
using Ryujinx.Common.Logging;
using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Kernel.Common;
using Ryujinx.Horizon.Kernel.Ipc;
using Ryujinx.Horizon.Kernel.Memory;
using Ryujinx.Horizon.Kernel.Process;
using Ryujinx.Horizon.Kernel.Threading;
using Ryujinx.Memory;
using System;
using System.Runtime.CompilerServices;

namespace Ryujinx.Horizon.Kernel.Svc
{
    public partial class Syscall
    {
        public Result SignalEvent(int handle)
        {
            KProcess process = _context.Scheduler.GetCurrentProcess();

            KWritableEvent writableEvent = process.HandleTable.GetObject<KWritableEvent>(handle);

            Result result;

            if (writableEvent != null)
            {
                writableEvent.Signal();

                result = Result.Success;
            }
            else
            {
                result = KernelResult.InvalidHandle;
            }

            return CheckResult(result);
        }

        public Result ClearEvent(int handle)
        {
            Result result;

            KProcess process = _context.Scheduler.GetCurrentProcess();

            KWritableEvent writableEvent = process.HandleTable.GetObject<KWritableEvent>(handle);

            if (writableEvent == null)
            {
                KReadableEvent readableEvent = process.HandleTable.GetObject<KReadableEvent>(handle);

                result = readableEvent?.Clear() ?? KernelResult.InvalidHandle;
            }
            else
            {
                result = writableEvent.Clear();
            }

            return CheckResult(result);
        }

        public Result ResetSignal(int handle)
        {
            KProcess currentProcess = _context.Scheduler.GetCurrentProcess();

            KReadableEvent readableEvent = currentProcess.HandleTable.GetObject<KReadableEvent>(handle);

            Result result;

            if (readableEvent != null)
            {
                result = readableEvent.ClearIfSignaled();
            }
            else
            {
                KProcess process = currentProcess.HandleTable.GetKProcess(handle);

                if (process != null)
                {
                    result = process.ClearIfNotExited();
                }
                else
                {
                    result = KernelResult.InvalidHandle;
                }
            }

            return CheckResult(result);
        }

        public Result CreateEvent(out int wEventHandle, out int rEventHandle)
        {
            KEvent Event = new KEvent(_context);

            KProcess process = _context.Scheduler.GetCurrentProcess();

            Result result = process.HandleTable.GenerateHandle(Event.WritableEvent, out wEventHandle);

            if (result == Result.Success)
            {
                result = process.HandleTable.GenerateHandle(Event.ReadableEvent, out rEventHandle);

                if (result != Result.Success)
                {
                    process.HandleTable.CloseHandle(wEventHandle);
                }
            }
            else
            {
                rEventHandle = 0;
            }

            return CheckResult(result);
        }
    }
}
