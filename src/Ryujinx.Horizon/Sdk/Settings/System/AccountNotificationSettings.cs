using Ryujinx.Horizon.Sdk.Account;
using System.Runtime.InteropServices;

namespace Ryujinx.Horizon.Sdk.Settings.System
{
    struct AccountNotificationSettings
    {
#pragma warning disable CS0649 // Field is never assigned to
        public Uid UserId;
        public uint Flags;
        public byte FriendPresenceOverlayPermission;
        public byte FriendInvitationOverlayPermission;
        public ushort Reserved;
#pragma warning restore CS0649
    }
}
