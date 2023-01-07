namespace Ryujinx.Horizon.Prepo
{
    enum PrepoServicePermissionLevel
    {
        Admin   = -1,
        User    = 1,
        System  = 2,
        Manager = 6,
        Debug   = unchecked((int)0x80000006)
    }
}