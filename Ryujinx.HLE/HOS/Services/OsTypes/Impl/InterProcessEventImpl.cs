using Ryujinx.Horizon.Kernel;
using Ryujinx.Horizon.Kernel.Common;
using System;

namespace Ryujinx.HLE.HOS.Services.OsTypes.Impl
{
    class InterProcessEventImpl
    {
        public static KernelResult Create(out int writableHandle, out int readableHandle)
        {
            KernelResult result = KernelStatic.Syscall.CreateEvent(out writableHandle, out readableHandle);

            if (result == KernelResult.OutOfResource)
            {
                // TODO: Return OS out of resource error.
            }
            else if (result != KernelResult.Success)
            {
                // TODO: Abort.
            }

            return KernelResult.Success;
        }

        public static void Close(int handle)
        {
            if (handle != 0)
            {
                KernelStatic.Syscall.CloseHandle(handle);

                // TODO: Abort on error.
            }
        }

        public static void Signal(int handle)
        {
            KernelStatic.Syscall.SignalEvent(handle);

            // TODO: Abort on error.
        }

        public static void Clear(int handle)
        {
            KernelStatic.Syscall.ClearEvent(handle);

            // TODO: Abort on error.
        }

        public static void Wait(int handle, bool autoClear)
        {
            Span<int> handles = stackalloc int[1];

            handles[0] = handle;

            while (true)
            {
                KernelResult result = KernelStatic.Syscall.WaitSynchronization(out _, handles, -1L);

                if (result == KernelResult.Success)
                {
                    if (autoClear)
                    {
                        result = KernelStatic.Syscall.ResetSignal(handle);

                        if (result == KernelResult.InvalidState)
                        {
                            continue;
                        }

                        // TODO: Abort here on failure.
                    }

                    return;
                }

                // TODO: Abort here if result is not "Cancelled".
            }
        }

        public static bool TryWait(int handle, bool autoClear)
        {
            if (autoClear)
            {
                return KernelStatic.Syscall.ResetSignal(handle) == KernelResult.Success;
            }

            Span<int> handles = stackalloc int[1];

            handles[0] = handle;

            while (true)
            {
                KernelResult result = KernelStatic.Syscall.WaitSynchronization(out _, handles, 0);

                if (result == KernelResult.Success)
                {
                    return true;
                }
                else if (result == KernelResult.TimedOut)
                {
                    return false;
                }

                // TODO: Abort here if result is not "Cancelled".
            }
        }

        public static bool TimedWait(int handle, bool autoClear, TimeSpan timeout)
        {
            Span<int> handles = stackalloc int[1];

            handles[0] = handle;

            long timeoutNs = timeout.Milliseconds * 1000000L;

            while (true)
            {
                KernelResult result = KernelStatic.Syscall.WaitSynchronization(out _, handles, timeoutNs);

                if (result == KernelResult.Success)
                {
                    if (autoClear)
                    {
                        result = KernelStatic.Syscall.ResetSignal(handle);

                        if (result == KernelResult.InvalidState)
                        {
                            continue;
                        }

                        // TODO: Abort here on failure.
                    }

                    return true;
                }
                else if (result == KernelResult.TimedOut)
                {
                    return false;
                }

                // TODO: Abort here if result is not "Cancelled".
            }
        }
    }
}
