namespace Ryujinx.Common.GraphicsDriver.NVAPI
{
    enum NvapiSettingID : uint
    {
        OglThreadControlId = 0x20C1221E,
        OglCplPreferDxPresentId = 0x20D690F8,
    }
    enum OglThreadControl : uint
    {
        OglThreadControlDefault = 0,
        OglThreadControlEnable = 1,
        OglThreadControlDisable = 2,
    }

    // Only present in driver versions >= 526.47
    enum OglCplDxPresent : uint
    {
        OglCplPreferDxPresentDisable = 0,
        OglCplPreferDxPresentEnable = 1,
        OglCplPreferDxPresentDefault = 2,
    }
}
