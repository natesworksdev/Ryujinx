using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Sf;
using Ryujinx.Horizon.Sm.Impl;

namespace Ryujinx.Horizon.Sm
{
    class UserService : IServiceObject
    {
        private readonly ServiceManager _serviceManager;

        private long _clientProcessId;
        private bool _initialized;

        public UserService()
        {
        }

        public UserService(ServiceManager serviceManager)
        {
            _serviceManager = serviceManager;
        }

        [Command(0)]
        public Result Initialize([ClientProcessId] long clientProcessId)
        {
            _clientProcessId = clientProcessId;
            _initialized = true;
            return Result.Success;
        }

        [Command(1)]
        public Result GetService([MoveHandle] out int handle, ServiceName name)
        {
            if (!_initialized)
            {
                handle = 0;
                return SmResult.InvalidClient;
            }

            return _serviceManager.GetService(out handle, _clientProcessId, name);
        }

        [Command(2)]
        public Result RegisterService([MoveHandle] out int handle, ServiceName name, int maxSessions, bool isLight)
        {
            if (!_initialized)
            {
                handle = 0;
                return SmResult.InvalidClient;
            }

            return _serviceManager.RegisterService(out handle, _clientProcessId, name, maxSessions, isLight);
        }

        [Command(3)]
        public Result UnregisterService(ServiceName name)
        {
            if (!_initialized)
            {
                return SmResult.InvalidClient;
            }

            return _serviceManager.UnregisterService(_clientProcessId, name);
        }
    }
}
