using Ryujinx.Common.Logging;
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Ryujinx.Common.System
{
    public static class ForceDpiAware
    {
        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        [DllImport("SDL2")]
        public static extern int SDL_GetDisplayDPI(
            int displayIndex,
            out float ddpi,
            out float hdpi,
            out float vdpi
        );

        [DllImport("SDL2")]
        public static extern int SDL_Init(uint flags);

        [DllImport("SDL2")]
        public static extern void SDL_Quit();
        private static readonly double _standardDpiScale = 96.0;

        /// <summary>
        /// Marks the application as DPI-Aware when running on the Windows operating system.
        /// </summary>
        public static void Windows()
        {
            // Make process DPI aware for proper window sizing on high-res screens.
            if (OperatingSystem.IsWindowsVersionAtLeast(6))
            {
                SetProcessDPIAware();
            }
        }

        public static double GetWindowScaleFactor()
        {
            double userDpiScale = 96.0;

            if (SDL_Init(32) == 0)
            {
                if (SDL_GetDisplayDPI(0, out var _, out var hdpi, out var _) == 0)
                {
                    userDpiScale = hdpi;
                }
                SDL_Quit();
            }

            return userDpiScale / _standardDpiScale;
        }
    }
}
