using Ryujinx.Horizon.Kernel;
using Ryujinx.Horizon.Sdk.Sf.Hipc;
using Ryujinx.Horizon.Sm.Impl;

namespace Ryujinx.Horizon.Sm
{
    public class SmMain
    {
        private readonly ServerManager _serverManager = new ServerManager();
        private readonly ServiceManager _serviceManager = new ServiceManager();

        public void Main()
        {
            KernelStatic.Syscall.ManageNamedPort("sm:", 64, out int smHandle).AbortOnFailure();

            _serverManager.RegisterServer<UserService>(smHandle, new UserService(_serviceManager));
            _serviceManager.RegisterServiceForSelf(out int smmHandle, ServiceName.Encode("sm:m"), 1).AbortOnFailure();
            _serverManager.RegisterServer<ManagerService>(smmHandle);
            _serverManager.ServiceRequests();
        }
    }
}
