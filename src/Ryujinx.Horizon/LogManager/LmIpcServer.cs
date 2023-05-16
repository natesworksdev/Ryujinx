using Ryujinx.Common.Logging;
using Ryujinx.Horizon.LogManager.Ipc;
using Ryujinx.Horizon.Sdk.Sf.Hipc;
using Ryujinx.Horizon.Sdk.Sm;
using System.Threading.Tasks;

namespace Ryujinx.Horizon.LogManager
{
    public class LmIpcServer: IService
    {
        private const int LogMaxSessionsCount = 42;

        private const int PointerBufferSize = 0x400;
        private const int MaxDomains        = 31;
        private const int MaxDomainObjects  = 61;
        private const int MaxPortsCount     = 1;

        private static readonly ManagerOptions _logManagerOptions = new(PointerBufferSize, MaxDomains, MaxDomainObjects, false);

        private SmApi         _sm;
        private ServerManager _serverManager;

        public async Task Initialize()
        {
            HeapAllocator allocator = new();

            Logger.Info?.Print(LogClass.Kernel, "Creating SmAPi");
            _sm = new SmApi();
            Logger.Info?.Print(LogClass.Kernel, "Initializing");
            (await _sm.Initialize()).AbortOnFailure();
            Logger.Info?.Print(LogClass.Kernel, "Post Initializing");

            _serverManager = new ServerManager(allocator, _sm, MaxPortsCount, _logManagerOptions, LogMaxSessionsCount);
            Logger.Info?.Print(LogClass.Kernel, "Registering");
            _serverManager.RegisterObjectForServer(new LogService(), ServiceName.Encode("lm"), LogMaxSessionsCount);
        }

        public async Task ServiceRequests()
        {
            await _serverManager.ServiceRequests();
        }

        public void Shutdown()
        {
            _serverManager.Dispose();
        }
    }
}