using System;
using System.Runtime.Versioning;
using WmiLight;
using Ryujinx.Common.Logging;
using System.Collections.Generic;
using System.Linq;

namespace Ryujinx.Common.SystemInfo
{
    [SupportedOSPlatform("windows")]
    class WindowsSystemInfo : SystemInfo
    {
        internal WindowsSystemInfo()
        {
            try
            {
                using (WmiConnection connection = new())
                {
                    (string cpuName, PhysicalCores) = GetCpuStatsLight(connection);
                    CpuName = $"{cpuName} ; {PhysicalCores} physical ; {LogicalCoreCount} logical";
                    (RamTotal, RamAvailable) = GetMemoryStatsWmiLight(connection);
                }
            }
            catch (Exception ex)
            {
                Logger.Error?.Print(LogClass.Application, $"WmiLight isn't available : {ex.Message}");
            }
        }


        private (string cpuName, int physicalCores) GetCpuStatsLight(WmiConnection connection)
        {
            string cpuName = Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER")?.Trim();
            int physicalCores = LogicalCoreCount;
            foreach (WmiObject cpuObj in GetWmiObjects(connection, "SELECT Name FROM Win32_Processor"))
            {
                cpuName = cpuObj["Name"].ToString().Trim();
            }

            foreach (WmiObject cpuObj in GetWmiObjects(connection, "SELECT NumberOfCores FROM Win32_Processor"))
            {
                physicalCores = Convert.ToInt32(cpuObj["NumberOfCores"]);
            }

            return (cpuName, physicalCores);
        }

        private (ulong TotalPhys, ulong AvailPhys) GetMemoryStatsWmiLight(WmiConnection connection)
        {
            ulong TotalPhys = 0;
            ulong AvailPhys = 0;

            foreach (WmiObject memObj in GetWmiObjects(connection, "SELECT TotalPhysicalMemory FROM Win32_ComputerSystem"))
            {
                TotalPhys = Convert.ToUInt64(memObj["TotalPhysicalMemory"]);
            }

            foreach (WmiObject memObj2 in GetWmiObjects(connection, "SELECT FreePhysicalMemory FROM Win32_OperatingSystem"))
            {
                AvailPhys = Convert.ToUInt64(memObj2["FreePhysicalMemory"]) * 1000;
            }

            return (TotalPhys, AvailPhys);
        }


        private IEnumerable<WmiObject> GetWmiObjects(WmiConnection connection, string query)
        {
            try
            {
                return connection.CreateQuery(query).ToList();
            }
            catch (Exception ex)
            {
                Logger.Error?.Print(LogClass.Application, $"WmiLight isn't available : {ex.Message}");
            }

            return Enumerable.Empty<WmiObject>();
        }

    }
}
