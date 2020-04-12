using System;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;

namespace Ryujinx.Common
{
    public class SystemInfo
    {
        public string OsDescription { get; private set; }
        public string CpuName       { get; private set; }
        public string RamSize       { get; private set; }

        public SystemInfo()
        {
            OsDescription = $"{RuntimeInformation.OSDescription} ({RuntimeInformation.OSArchitecture})";
            CpuName       = "Unknown";
            RamSize       = "Unknown";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                foreach (ManagementBaseObject mObject in new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_Processor").Get())
                {
                    CpuName = mObject["Name"].ToString();
                }

                foreach (ManagementBaseObject mObject in new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_OperatingSystem").Get())
                {
                    RamSize = $"{Math.Round(double.Parse(mObject["TotalVisibleMemorySize"].ToString()) / 1024, 0)} MB";
                }
            }

            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                CpuName = File.ReadAllLines("/proc/cpuinfo").Where(line => line.StartsWith("model name")).ToList()[0].Split(":")[1].Trim();
                RamSize = $"{Math.Round(double.Parse(File.ReadAllLines("/proc/meminfo")[0].Split(":")[1].Trim().Split(" ")[0]) / 1024, 0)} MB";
            }
        }
    }
}