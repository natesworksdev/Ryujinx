using Microsoft.Management.Infrastructure;
using Ryujinx.Common.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;

namespace Ryujinx.Common.SystemInfo
{
    [SupportedOSPlatform("windows")]
    partial class WindowsSystemInfo : SystemInfo
    {
        internal WindowsSystemInfo()
        {
            CpuName = $"{GetCpuidCpuName() ?? GetCpuNameMMI()} ; {GetPhysicalCoreCount()} physical ; {LogicalCoreCount} logical"; // WMI is very slow
            (RamTotal, RamAvailable) = GetMemoryStatsMMI();
        }

        private static string  GetCpuNameMMI()
        {
            var cpuObjs = GetMMIObjects(@"root\cimv2", "SELECT * FROM Win32_Processor");

            if (cpuObjs != null)
            {
                foreach (var cpuObj in cpuObjs)
                {
                    return cpuObj.CimInstanceProperties["Name"].Value.ToString().Trim();
                }
            }
            
            return Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER")?.Trim();
        }
        
        private static (ulong TotalPhys, ulong AvailPhys) GetMemoryStatsMMI()
        {
            var memObjs = GetMMIObjects(@"root\cimv2", "SELECT * FROM Win32_ComputerSystem");
            var memObjs2 = GetMMIObjects(@"root\cimv2", "SELECT * FROM Win32_OperatingSystem");

            ulong TotalPhys = 0;
            ulong AvailPhys = 0;

            if (memObjs != null)
            {
                foreach (var memObj in memObjs)
                {
                    TotalPhys = (ulong)memObj.CimInstanceProperties["TotalPhysicalMemory"].Value;
                }
            }

            if (memObjs2 != null)
            {
                foreach (var memObj2 in memObjs2)
                {
                    AvailPhys = (ulong)memObj2.CimInstanceProperties["FreePhysicalMemory"].Value*1000;
                }
            }

            return (TotalPhys, AvailPhys);
        }

        private static IEnumerable<CimInstance> GetMMIObjects(string namespaceName, string query)
        {
            try
            {
                using (CimSession session = CimSession.Create(null))
                {
                    return session.QueryInstances(namespaceName, "WQL", query).ToList();
                }
            }
            catch (CimException ex)
            {
                Logger.Error?.Print(LogClass.Application, $"MMI isn't available : {ex.Message}");
            }

            return Enumerable.Empty<CimInstance>();
        }
    }
}
