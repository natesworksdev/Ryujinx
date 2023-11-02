using System;

namespace Ryujinx.Common.Microsleep
{
    public static class MicrosleepHelper
    {
        public static IMicrosleepEvent CreateEvent()
        {
            if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS() || OperatingSystem.IsIOS() || OperatingSystem.IsAndroid())
            {
                return new NanosleepEvent();
            }
            else
            {
                return new SleepEvent();
            }
        }
    }
}
