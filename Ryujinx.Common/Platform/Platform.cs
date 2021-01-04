using System;

namespace Ryujinx.Common.Platform
{
    public class Platform
    {
        public virtual void ShowConsole() {}
        public virtual void HideConsole() {}
        public static Platform Instance { get; }

        static Platform()
        {
            if (OperatingSystem.IsWindows())
            {
                Instance = new WindowsPlatform();
            }
            else
            {
                Instance = new Platform();
            }
        }
    }
}