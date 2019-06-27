using System;
using System.Collections.Generic;
using System.Text;

namespace Ryujinx.HLE.HOS.Services.Friend
{
    [Flags]
    enum FriendServicePermissionLevel
    {
        Admin        = -1,
        User         = 1,
        Overlay      = 3,
        Manager      = 7,
        System       = 9
    }
}
