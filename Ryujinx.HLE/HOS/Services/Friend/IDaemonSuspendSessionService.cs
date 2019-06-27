using Ryujinx.HLE.HOS.Ipc;
using System.Collections.Generic;

namespace Ryujinx.HLE.HOS.Services.Friend
{
    class IDaemonSuspendSessionService : IpcService
    {
        private Dictionary<int, ServiceProcessRequest> _commands;

        private FriendServicePermissionLevel PermissionLevel;

        public override IReadOnlyDictionary<int, ServiceProcessRequest> Commands => _commands;

        public IDaemonSuspendSessionService(FriendServicePermissionLevel permissionLevel)
        {
            _commands = new Dictionary<int, ServiceProcessRequest>
            {
                // ...
            };

            PermissionLevel = permissionLevel;
        }
    }
}