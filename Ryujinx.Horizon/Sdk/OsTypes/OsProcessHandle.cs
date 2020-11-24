using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Kernel;

namespace Ryujinx.Horizon.Sdk.OsTypes
{
    public static partial class Os
    {
        private const int SelfProcessHandle = (0x1ffff << 15) | 1;

        public static int GetCurrentProcessHandle()
        {
            return SelfProcessHandle;
        }

        public static long GetCurrentProcessId()
        {
            return GetProcessId(GetCurrentProcessHandle());
        }

        private static long GetProcessId(int handle)
        {
            Result result = TryGetProcessId(handle, out long pid);

            result.AbortOnFailure();

            return pid;
        }

        private static Result TryGetProcessId(int handle, out long pid)
        {
            return KernelStatic.Syscall.GetProcessId(handle, out pid);
        }
    }
}
