using Ryujinx.Horizon.Kernel.Process;
using System;
using System.Runtime.CompilerServices;

namespace Ryujinx.Horizon.Kernel.Common
{
    static class KernelTransfer
    {
        public static bool UserToKernelInt32(KernelContextInternal context, ulong address, out int value)
        {
            KProcess currentProcess = context.Scheduler.GetCurrentProcess();

            if (currentProcess.CpuMemory.IsMapped(address) &&
                currentProcess.CpuMemory.IsMapped(address + 3))
            {
                value = currentProcess.CpuMemory.Read<int>(address);

                return true;
            }

            value = 0;

            return false;
        }

        public static bool UserToKernelInt32Array(KernelContextInternal context, ulong address, Span<int> values)
        {
            KProcess currentProcess = context.Scheduler.GetCurrentProcess();

            for (int index = 0; index < values.Length; index++, address += 4)
            {
                if (currentProcess.CpuMemory.IsMapped(address) &&
                    currentProcess.CpuMemory.IsMapped(address + 3))
                {
                    values[index] = currentProcess.CpuMemory.Read<int>(address);
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        public static bool UserToKernelString(KernelContextInternal context, ulong address, ulong size, out string value)
        {
            KProcess currentProcess = context.Scheduler.GetCurrentProcess();

            value = string.Empty;

            for (ulong offset = 0; offset < size; offset++)
            {
                if (!currentProcess.CpuMemory.IsMapped(address + offset))
                {
                    value = null;
                    return false;
                }

                char chr = (char)currentProcess.CpuMemory.Read<byte>(address + offset);
                if (chr == '\0')
                {
                    break;
                }

                value += chr;
            }

            return true;
        }

        public static bool KernelToUserInt32(KernelContextInternal context, ulong address, int value)
        {
            return KernelToUser(context, address, value);
        }

        public static bool KernelToUserInt64(KernelContextInternal context, ulong address, long value)
        {
            return KernelToUser(context, address, value);
        }

        public static bool KernelToUser<T>(KernelContextInternal context, ulong address, T value) where T : unmanaged
        {
            KProcess currentProcess = context.Scheduler.GetCurrentProcess();

            if (currentProcess.CpuMemory.IsMapped(address) &&
                currentProcess.CpuMemory.IsMapped(address + (ulong)Unsafe.SizeOf<T>() - 1))
            {
                currentProcess.CpuMemory.Write(address, value);

                return true;
            }

            return false;
        }
    }
}