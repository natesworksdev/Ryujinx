using Ryujinx.Common.Logging;
using System;

namespace Ryujinx.Common.GraphicsDriver
{
    public static class DriverUtilities
    {
        public static void ToggleOGLThreading(bool enabled)
        {
            Environment.SetEnvironmentVariable("mesa_glthread", enabled.ToString().ToLower());
            Environment.SetEnvironmentVariable("__GL_THREADED_OPTIMIZATIONS", enabled ? "1" : "0");

            try
            {
                NVDriverHelper.SetThreadedOptimization(enabled);
            }
            catch
            {
                Logger.Warning?.Print(LogClass.Application, "Failed to set threaded optimizations. NVAPI may be unavailable.");
            }
        }

        public static void ToggleDxgiSwapchain(bool enabled)
        {
            try
            {
                NVDriverHelper.SetDxgiSwapchain(enabled);
            }
            catch
            {
                Logger.Warning?.Print(LogClass.Application, "Failed to set Vulkan/OpenGL present method. NVAPI may be unavailable.");
            }
        }
    }
}
