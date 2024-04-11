using Ryujinx.Common.GraphicsDriver.NVAPI;
using Ryujinx.Common.Logging;
using System;

namespace Ryujinx.Common.GraphicsDriver
{
    public static class DriverUtilities
    {
        public static void ToggleNvDriverSetting(NvapiSettingId id, bool enabled)
        {
            try
            {
                if (id == NvapiSettingId.OglThreadControlId)
                {
                    Environment.SetEnvironmentVariable("mesa_glthread", enabled.ToString().ToLower());
                    Environment.SetEnvironmentVariable("__GL_THREADED_OPTIMIZATIONS", enabled ? "1" : "0");
                }

                NVDriverHelper.SetControlOption(id, enabled);
            }
            catch
            {
                Logger.Warning?.Print(LogClass.Application, "Failed to set NVIDIA driver settings. NVAPI may be unavailable.");
            }
        }
    }
}
