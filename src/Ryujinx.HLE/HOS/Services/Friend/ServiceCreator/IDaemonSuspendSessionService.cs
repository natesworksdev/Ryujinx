namespace Ryujinx.HLE.HOS.Services.Friend.ServiceCreator
{
    class IDaemonSuspendSessionService : IpcService
    {
        private readonly FriendServicePermissionLevel PermissionLevel;

        public IDaemonSuspendSessionService(FriendServicePermissionLevel permissionLevel)
        {
            PermissionLevel = permissionLevel;
        }
    }
}