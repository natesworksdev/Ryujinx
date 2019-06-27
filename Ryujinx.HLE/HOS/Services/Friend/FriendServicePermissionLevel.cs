using System;
using System.Collections.Generic;
using System.Text;

namespace Ryujinx.HLE.HOS.Services.Friend
{
    [Flags]
    enum FriendServicePermissionLevelMask
    {
        User    = 1,
        Overlay = 2,
        Manager = 4,
        System  = 8
    }

    enum FriendServicePermissionLevel
    {
        Admin        = -1,
        User         = FriendServicePermissionLevelMask.User,
        Overlay      = FriendServicePermissionLevelMask.User | FriendServicePermissionLevelMask.Overlay,
        Manager      = FriendServicePermissionLevelMask.User | FriendServicePermissionLevelMask.Overlay | FriendServicePermissionLevelMask.Manager,
        System       = FriendServicePermissionLevelMask.User | FriendServicePermissionLevelMask.System
    }
}
