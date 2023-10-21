using Ryujinx.Horizon.Sdk.Sf.Hipc;
using Ryujinx.Horizon.Sdk.Sm;

namespace Ryujinx.Horizon.Am
{
    internal class AmIpcServer
    {
        // TODO: Get actual values from RE
        private const int MaxSessionsCount = 10;
        private const int TotalMaxSessionsCount = MaxSessionsCount * 4;

        private const int PointerBufferSize = 0;
        private const int MaxDomains = 0;
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

            _serverManager.RegisterServer(0, ServiceName.Encode("appletAE"), MaxSessionsCount);
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
