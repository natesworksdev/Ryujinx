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

        private const int PctlTotalMaxSessionsCount =
            PctlMaxSessionsCount + PctlSMaxSessionsCount + PctlAMaxSessionsCount + PctlRMaxSessionsCount;

        private const int PointerBufferSize = 0x80;
        // TODO: Use actual values these are from LogManager
        private const int MaxDomains = 31;
        private const int MaxDomainObjects = 61;

        private const int MaxPortsCount = 4;

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
                new ServerManager(allocator, _sm, MaxPortsCount, _pctlManagerOptions, PctlTotalMaxSessionsCount);

            _pctlSericeObject = new ParentalControlServiceFactory(0x303);
            _pctlSSericeObject = new ParentalControlServiceFactory(0x838E);
            _pctlASericeObject = new ParentalControlServiceFactory(0x83BE);
            _pctlRSericeObject = new ParentalControlServiceFactory(0x8040);

            _pctlServerManager.RegisterObjectForServer(_pctlSericeObject, _pctlServiceName, PctlMaxSessionsCount);
            _pctlServerManager.RegisterObjectForServer(_pctlSSericeObject, _pctlSServiceName, PctlSMaxSessionsCount);
            _pctlServerManager.RegisterObjectForServer(_pctlASericeObject, _pctlAServiceName, PctlAMaxSessionsCount);
            _pctlServerManager.RegisterObjectForServer(_pctlRSericeObject, _pctlRServiceName, PctlRMaxSessionsCount);
        }

        public void ServiceRequests()
        {
            _pctlServerManager.ServiceRequests();
        }

        public void Shutdown()
        {
            _pctlServerManager.Dispose();
        }
    }
}