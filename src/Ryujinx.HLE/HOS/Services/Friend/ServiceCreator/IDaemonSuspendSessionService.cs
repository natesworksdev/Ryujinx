namespace Ryujinx.HLE.HOS.Services.Friend.ServiceCreator
{
    class IDaemonSuspendSessionService : IpcService
    {
#pragma warning disable IDE0052
        private readonly FriendServicePermissionLevel PermissionLevel;
#pragma warning restore IDE0052

        public IDaemonSuspendSessionService(FriendServicePermissionLevel permissionLevel)
        {
            PermissionLevel = permissionLevel;
        }
    }
}