using Ryujinx.Horizon.Common;
using Ryujinx.Memory;
using System;

namespace Ryujinx.Horizon
{
    static class HorizonStatic
    {
        [field: ThreadStatic]
        public static HorizonOptions Options { get; private set; }

        [field: ThreadStatic]
        public static ISyscallApi Syscall { get; private set; }

        [field: ThreadStatic]
        public static IVirtualMemoryManager AddressSpace { get; private set; }

        [field: ThreadStatic]
        public static IThreadContext ThreadContext { get; private set; }

        [field: ThreadStatic]
        public static int CurrentThreadHandle { get; private set; }

        public static void Register(
            HorizonOptions options,
            ISyscallApi syscallApi,
            IVirtualMemoryManager addressSpace,
            IThreadContext threadContext,
            int threadHandle)
        {
            Options = options;
            Syscall = syscallApi;
            AddressSpace = addressSpace;
            ThreadContext = threadContext;
            CurrentThreadHandle = threadHandle;
        }
    }
}