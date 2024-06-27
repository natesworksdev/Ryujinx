using System;
using System.Linq;
using System.Management;

namespace Ryujinx.Common.Utilities
{
    public static class AVUtils
    {
        public static bool IsRunningThirdPartyAV()
        {
            if (!OperatingSystem.IsWindows())
            {
                return false;
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
                        return true;
                    }
                }
                catch (ManagementException)
                {
                    continue;
                }
            }

            return false;
        }
    }
}
