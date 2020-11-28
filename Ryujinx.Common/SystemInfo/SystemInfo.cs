using System;
using System.Runtime.InteropServices;

namespace Ryujinx.Common.SystemInfo
{
    public class SystemInfo
    {
        public virtual string OsDescription => $"{RuntimeInformation.OSDescription} ({RuntimeInformation.OSArchitecture})";
        public virtual string CpuName => "Unknown";
        public virtual ulong RamSize => 0;
        public string RamSizeInMB => (RamSize == 0) ? "Unknown" : $"{RamSize / 1024 / 1024} MB";

        public static SystemInfo Instance { get; }

        static SystemInfo()
        {
            if (OperatingSystem.IsWindows())
            {
                Instance = new WindowsSysteminfo();
            }
            else if (OperatingSystem.IsLinux())
            {
                Instance = new LinuxSysteminfo();
            }
            else if (OperatingSystem.IsMacOS())
            {
                Instance = new MacOSSysteminfo();
            }
            else
            {
                Instance = new SystemInfo();
            }
        }
    }
}