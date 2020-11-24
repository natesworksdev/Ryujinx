namespace Ryujinx.Horizon.Sdk.Sf.Hipc
{
    enum HipcBufferFlags : byte
    {
        In = 1 << 0,
        Out = 1 << 1,
        HipcMapAlias = 1 << 2,
        HipcPointer = 1 << 3,
        FixedSize = 1 << 4,
        HipcAutoSelect = 1 << 5,
        HipcMapTransferAllowsNonSecure = 1 << 6,
        HipcMapTransferAllowsNonDevice = 1 << 7
    }
}
