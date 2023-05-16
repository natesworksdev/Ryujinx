using Ryujinx.Horizon.Common;
using Ryujinx.Memory;
using System;
using System.Threading;

namespace Ryujinx.Horizon
{
    public static class HorizonStatic
    {
        private static AsyncLocal<HorizonOptions> _options = new AsyncLocal<HorizonOptions>();

        private static AsyncLocal<ISyscallApi> _syscall = new AsyncLocal<ISyscallApi>();

        private static AsyncLocal<IVirtualMemoryManager> _addressSpace = new AsyncLocal<IVirtualMemoryManager>();

        private static AsyncLocal<IThreadContext> _threadContext = new AsyncLocal<IThreadContext>();

        private static AsyncLocal<int> _threadHandle = new AsyncLocal<int>();

        public static HorizonOptions        Options             => _options.Value;
        public static ISyscallApi           Syscall             => _syscall.Value;
        public static IVirtualMemoryManager AddressSpace        => _addressSpace.Value;
        public static IThreadContext        ThreadContext       => _threadContext.Value;
        public static int                   CurrentThreadHandle => _threadHandle.Value;

        public static void Register(
            HorizonOptions        options,
            ISyscallApi           syscallApi,
            IVirtualMemoryManager addressSpace,
            IThreadContext        threadContext,
            int                   threadHandle)
        {
            _options.Value       = options;
            _syscall.Value       = syscallApi;
            _addressSpace.Value  = addressSpace;
            _threadContext.Value = threadContext;
            _threadHandle.Value  = threadHandle;
        }
    }
}
