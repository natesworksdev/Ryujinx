using Ryujinx.Horizon.Am.Ipc;
using Ryujinx.Horizon.Sdk.Sf.Hipc;
using Ryujinx.Horizon.Sdk.Sm;

namespace Ryujinx.Horizon.Am
{
    internal class AmIpcServer
    {
        private const int MaxSessionsCountAE = 9;
        private const int MaxSessionsCountOE = 1;
        private const int TotalMaxSessionsCount = MaxSessionsCountAE + MaxSessionsCountOE;

        private const int PointerBufferSize = 0;
        private const int MaxDomains = 10;
        private const int MaxDomainObjects = 64;
        private const int MaxPortsCount = 4;

        private static readonly ManagerOptions _options = new(PointerBufferSize, MaxDomains, MaxDomainObjects, false);

        private SmApi _sm;
        private ServerManager _serverManager;

        public void Initialize()
        {
            HeapAllocator allocator = new();

            _sm = new SmApi();
            _sm.Initialize().AbortOnFailure();

            _serverManager = new ServerManager(allocator, _sm, MaxPortsCount, _options, TotalMaxSessionsCount);

            _serverManager.RegisterObjectForServer(new ProxiesService(), ServiceName.Encode("appletAE"), MaxSessionsCountAE);
            _serverManager.RegisterObjectForServer(new ProxiesService(), ServiceName.Encode("appletOE"), MaxSessionsCountOE);
        }

        public void ServiceRequests()
        {
            _serverManager.ServiceRequests();
        }

        public void Shutdown()
        {
            _serverManager.Dispose();
        }
    }
}
