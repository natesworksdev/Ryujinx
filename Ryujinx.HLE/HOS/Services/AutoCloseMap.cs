using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Kernel;
using System;

namespace Ryujinx.HLE.HOS.Services
{
    struct AutoCloseMap : IDisposable
    {
        private int _processHandle;
        public Result Result { get; }
        private ulong _mappedAddress;
        private ulong _baseAddress;
        private ulong _size;

        public AutoCloseMap(ulong mappedAddress, int processHandle, ulong baseAddress, ulong size)
        {
            _processHandle = processHandle;
            _mappedAddress = mappedAddress;
            _baseAddress = baseAddress;
            _size = size;
            Result = KernelStatic.Syscall.MapProcessMemory(_mappedAddress, _processHandle, _baseAddress, _size);
        }

        public void Dispose() => KernelStatic.Syscall.UnmapProcessMemory(_mappedAddress, _processHandle, _baseAddress, _size);
    }
}