using Ryujinx.Horizon.Kernel;

namespace Ryujinx.Horizon.Sm.Impl
{
    struct ServiceInfo
    {
        public ServiceName Name;
        public long OwnerProcessId;
        public int PortHandle;

        public void Free()
        {
            KernelStatic.Syscall.CloseHandle(PortHandle);

            Name = ServiceName.Invalid;
            OwnerProcessId = 0L;
            PortHandle = 0;
        }
    }
}
