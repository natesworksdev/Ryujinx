namespace Ryujinx.HLE.HOS.Services.Friend.ServiceCreator
{
    sealed class IDaemonSuspendSessionService : IpcService
    {
        private FriendServicePermissionLevel PermissionLevel;

        public IDaemonSuspendSessionService(FriendServicePermissionLevel permissionLevel)
        {
            PermissionLevel = permissionLevel;
        }
    }
}