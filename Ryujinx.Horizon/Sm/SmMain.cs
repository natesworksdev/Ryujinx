using Ryujinx.Horizon.Sdk.Sf.Hipc;
using Ryujinx.Horizon.Sdk.Sm;
using Ryujinx.Horizon.Sm.Impl;

namespace Ryujinx.Horizon.Sm
{
    public class SmMain
    {
        private enum PortIndex
        {
            User,
            Manager
        }

        private const int SmMaxSessionsCount = 64;
        private const int SmmMaxSessionsCount = 1;

        private const int MaxPortsCount = 2;

        private readonly ServerManager  _serverManager  = new(null, null, MaxPortsCount, ManagerOptions.Default, 0);
        private readonly ServiceManager _serviceManager = new();

        public void Main()
        {
            HorizonStatic.Syscall.ManageNamedPort(out int smHandle, "sm:", SmMaxSessionsCount).AbortOnFailure();

            _serverManager.RegisterServer((int)PortIndex.User, smHandle);
            _serviceManager.RegisterServiceForSelf(out int smmHandle, ServiceName.Encode("sm:m"), SmmMaxSessionsCount).AbortOnFailure();
            _serverManager.RegisterServer((int)PortIndex.Manager, smmHandle);
            _serverManager.ServiceRequests();
        }
    }
}