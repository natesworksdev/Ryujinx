using Ryujinx.Horizon.Sdk.Sf.Hipc;
using Ryujinx.Horizon.Sdk.Sm;

namespace Ryujinx.Horizon.Pctl
{
    class PctlIpcServer
    {
        private const int PctlMaxSessionsCount = 6;
        private const int PctlSMaxSessionsCount = 8;
        private const int PctlAMaxSessionsCount = 1;
        private const int PctlRMaxSessionsCount = 1;

        // TODO: Use actual values these are from LogManager
        private const int PointerBufferSize = 0x400;
        private const int MaxDomains = 31;
        private const int MaxDomainObjects = 61;

        private const int MaxPortsCount = 1;

        private static readonly ManagerOptions _pctlManagerOptions = new ManagerOptions(
                PointerBufferSize,
                MaxDomains,
                MaxDomainObjects,
                false);

        private static readonly ServiceName _pctlServiceName = ServiceName.Encode("pctl");
        private static readonly ServiceName _pctlSServiceName = ServiceName.Encode("pctl:s");
        private static readonly ServiceName _pctlAServiceName = ServiceName.Encode("pctl:a");
        private static readonly ServiceName _pctlRServiceName = ServiceName.Encode("pctl:r");

        private SmApi _sm;
        private ServerManager _pctlServerManager;
        private ServerManager _pctlSServerManager;
        private ServerManager _pctlAServerManager;
        private ServerManager _pctlRServerManager;

        private ParentalControlServiceFactory _pctlSericeObject;
        private ParentalControlServiceFactory _pctlSSericeObject;
        private ParentalControlServiceFactory _pctlASericeObject;
        private ParentalControlServiceFactory _pctlRSericeObject;

        public void Initialize()
        {
            HeapAllocator allocator = new HeapAllocator();

            _sm = new SmApi();
            _sm.Initialize().AbortOnFailure();

            _pctlServerManager =
                new ServerManager(allocator, _sm, MaxPortsCount, _pctlManagerOptions, PctlMaxSessionsCount);
            _pctlSServerManager =
                new ServerManager(allocator, _sm, MaxPortsCount, _pctlManagerOptions, PctlSMaxSessionsCount);
            _pctlAServerManager =
                new ServerManager(allocator, _sm, MaxPortsCount, _pctlManagerOptions, PctlAMaxSessionsCount);
            _pctlRServerManager =
                new ServerManager(allocator, _sm, MaxPortsCount, _pctlManagerOptions, PctlRMaxSessionsCount);

            _pctlSericeObject = new ParentalControlServiceFactory(0x303);
            _pctlSSericeObject = new ParentalControlServiceFactory(0x838E);
            _pctlASericeObject = new ParentalControlServiceFactory(0x83BE);
            _pctlRSericeObject = new ParentalControlServiceFactory(0x8040);

            _pctlServerManager.RegisterObjectForServer(_pctlSericeObject, _pctlServiceName, PctlMaxSessionsCount);
            _pctlSServerManager.RegisterObjectForServer(_pctlSSericeObject, _pctlSServiceName, PctlSMaxSessionsCount);
            _pctlAServerManager.RegisterObjectForServer(_pctlASericeObject, _pctlAServiceName, PctlAMaxSessionsCount);
            _pctlRServerManager.RegisterObjectForServer(_pctlRSericeObject, _pctlRServiceName, PctlRMaxSessionsCount);
        }

        public void ServiceRequests()
        {
            _pctlServerManager.ServiceRequests();
            _pctlSServerManager.ServiceRequests();
            _pctlAServerManager.ServiceRequests();
            _pctlRServerManager.ServiceRequests();
        }

        public void Shutdown()
        {
            _pctlServerManager.Dispose();
            _pctlSServerManager.Dispose();
            _pctlAServerManager.Dispose();
            _pctlRServerManager.Dispose();
        }
    }
}