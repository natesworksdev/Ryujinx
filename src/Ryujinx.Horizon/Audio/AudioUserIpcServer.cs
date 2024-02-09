using Ryujinx.Horizon.Sdk.Audio.Detail;
using Ryujinx.Horizon.Sdk.Codec.Detail;
using Ryujinx.Horizon.Sdk.Sf.Hipc;
using Ryujinx.Horizon.Sdk.Sm;

namespace Ryujinx.Horizon.Audio
{
    class AudioUserIpcServer
    {
        private const int MaxSessionsCount = 30;

        private const int PointerBufferSize = 0x8000; // TODO: Correct value.
        // TODO: Only HWOPUS supports domains, but we use then for everything since the server manager is currently shared with the other audio services.
        private const int MaxDomains = 32; // TODO: Correct value.
        private const int MaxDomainObjects = 64; // TODO: Correct value.
        private const int MaxPortsCount = 1;

        private static readonly ManagerOptions _options = new(PointerBufferSize, MaxDomains, MaxDomainObjects, false);

        private SmApi _sm;
        private ServerManager _serverManager;
        private AudioManagers _managers;

        public void Initialize()
        {
            HeapAllocator allocator = new();

            _sm = new SmApi();
            _sm.Initialize().AbortOnFailure();

            _serverManager = new ServerManager(allocator, _sm, MaxPortsCount, _options, MaxSessionsCount);
            _managers = new AudioManagers(HorizonStatic.Options.AudioDeviceDriver, HorizonStatic.Options.TickSource);

            AudioRendererManager audioRendererManager = new(_managers.AudioRendererManager, _managers.AudioDeviceSessionRegistry);
            AudioOutManager audioOutManager = new(_managers.AudioOutputManager);
            AudioInManager audioInManager = new(_managers.AudioInputManager);

            _serverManager.RegisterObjectForServer(audioRendererManager, ServiceName.Encode("audren:u"), MaxSessionsCount);
            _serverManager.RegisterObjectForServer(audioOutManager, ServiceName.Encode("audout:u"), MaxSessionsCount);
            _serverManager.RegisterObjectForServer(audioInManager, ServiceName.Encode("audin:u"), MaxSessionsCount);

            // The service uses a separate IPC thread for HWOPUS, we might want to do that too eventually.
            HardwareOpusDecoderManager hardwareOpusDecoderManager = new();

            _serverManager.RegisterObjectForServer(hardwareOpusDecoderManager, ServiceName.Encode("hwopus"), MaxSessionsCount);
        }

        public void ServiceRequests()
        {
            _serverManager.ServiceRequests();
        }

        public void Shutdown()
        {
            _serverManager.Dispose();
            _managers.Dispose();
            _sm.Dispose();
        }
    }
}
