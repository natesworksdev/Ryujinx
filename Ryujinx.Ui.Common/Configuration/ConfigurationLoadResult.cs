using System;

namespace Ryujinx.Ui.Common.Configuration
{
    [Flags]
    public enum ConfigurationLoadResult
    {
        Success = 0,
        NotLoaded = 1,
        MigratedFromPreVulkan = 1 << 8
    }
}