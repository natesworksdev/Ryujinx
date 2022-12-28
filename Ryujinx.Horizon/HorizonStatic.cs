using Ryujinx.Horizon.Common;
using Ryujinx.Memory;
using System;

namespace Ryujinx.Horizon
{
    static class HorizonStatic
    {
        [ThreadStatic]
        private static HorizonOptions _options;

        [ThreadStatic]
        private static ISyscallApi _syscall;

        [ThreadStatic]
        private static IVirtualMemoryManager _addressSpace;

        [ThreadStatic]
        private static IThreadContext _threadContext;

        public static HorizonOptions Options => _options;
        public static ISyscallApi Syscall => _syscall;
        public static IVirtualMemoryManager AddressSpace => _addressSpace;
        public static IThreadContext ThreadContext => _threadContext;

        public static void Register(
            HorizonOptions options,
            ISyscallApi syscallApi,
            IVirtualMemoryManager addressSpace,
            IThreadContext threadContext)
        {
            _options = options;
            _syscall = syscallApi;
            _addressSpace = addressSpace;
            _threadContext = threadContext;
        }
    }
}
