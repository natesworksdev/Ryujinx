using System;
using System.Linq;
using System.Management;

namespace Ryujinx.Common.Utilities
{
    public static class AVUtils
    {
        public static string GetAVName()
        {
            if (!OperatingSystem.IsWindows())
            {
                return null;
            }

            ManagementObjectSearcher wmiData = new ManagementObjectSearcher(@"root\SecurityCenter2", "SELECT * FROM AntiVirusProduct");
            ManagementObjectCollection data = wmiData.Get();

            foreach (ManagementObject dataObj in data.Cast<ManagementObject>())
            {
                try
                {
                    string displayName = (string)dataObj["displayName"];
                    if (displayName != "Windows Defender")
                    {
                        return displayName;
                    }
                }
                catch (ManagementException)
                {
                    continue;
                }
            }

            return null;
        }
    }
}
