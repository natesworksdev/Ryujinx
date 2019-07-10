using System;

namespace Ryujinx.HLE.HOS.Services.Bsd
{
    [Flags]
    enum BsdServicePermissionLevel
    {
        User,
        System
    }
}