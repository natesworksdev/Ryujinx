using Ryujinx.Horizon.Pctl.Ipc;
using Ryujinx.Horizon.Sdk.Sf.Hipc;
using Ryujinx.Horizon.Sdk.Sm;

namespace Ryujinx.Horizon.Pctl
{
    internal class PctlIpcServer
    {
        private const int MaxSessionsCount = 6;
        private const int SMaxSessionsCount = 8;
        private const int AMaxSessionsCount = 1;
        private const int RMaxSessionsCount = 1;

        private const int TotalMaxSessionsCount = MaxSessionsCount + SMaxSessionsCount + AMaxSessionsCount + RMaxSessionsCount;

        private const int PointerBufferSize = 0x80;
        private const int MaxDomains = 32;
        private const int MaxDomainObjects = 64;
        private const int MaxPortsCount = 4;

        private static readonly ManagerOptions _managerOptions = new(PointerBufferSize, MaxDomains, MaxDomainObjects, false);

        private SmApi _sm;
        private ServerManager _serverManager;

        public void Initialize()
        {
            HeapAllocator allocator = new();

            _sm = new SmApi();
            _sm.Initialize().AbortOnFailure();

            _serverManager = new ServerManager(allocator, _sm, MaxPortsCount, _managerOptions, TotalMaxSessionsCount);

            _serverManager.RegisterObjectForServer(new PctlServiceFactory(0x303), ServiceName.Encode("pctl"), MaxSessionsCount);
            _serverManager.RegisterObjectForServer(new PctlServiceFactory(0x838E), ServiceName.Encode("pctl:s"), SMaxSessionsCount);
            _serverManager.RegisterObjectForServer(new PctlServiceFactory(0x83BE), ServiceName.Encode("pctl:a"), AMaxSessionsCount);
            _serverManager.RegisterObjectForServer(new PctlServiceFactory(0x8040), ServiceName.Encode("pctl:r"), RMaxSessionsCount);
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
