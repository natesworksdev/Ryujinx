using System;
using System.Runtime.Versioning;
using WmiLight;
using Ryujinx.Common.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Ryujinx.Common.SystemInfo
{
    [SupportedOSPlatform("windows")]
    partial class WindowsSystemInfo : SystemInfo
    {
        internal WindowsSystemInfo()
        {
            try
            {
                using (WmiConnection connection = new())
                {
                    (string cpuName, PhysicalCores) = GetCpuStatsLight(connection);
                    CpuName = $"{cpuName ?? GetCpuidCpuName()} ; {PhysicalCores} physical ; {LogicalCoreCount} logical";
                    (RamTotal, RamAvailable) = GetMemoryStats();
                }
            }
            catch (Exception ex)
            {
                Logger.Error?.Print(LogClass.Application, $"WmiLight isn't available : {ex.Message}");
            }
        }
        
        private static (ulong Total, ulong Available) GetMemoryStats()
        {
            MemoryStatusEx memStatus = new();
            if (GlobalMemoryStatusEx(ref memStatus))
            {
                return (memStatus.TotalPhys, memStatus.AvailPhys); // Bytes
            }

            Logger.Error?.Print(LogClass.Application, $"GlobalMemoryStatusEx failed. Error {Marshal.GetLastWin32Error():X}");

            return (0, 0);
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
        
        [StructLayout(LayoutKind.Sequential)]
        private struct MemoryStatusEx
        {
            public uint Length;
            public uint MemoryLoad;
            public ulong TotalPhys;
            public ulong AvailPhys;
            public ulong TotalPageFile;
            public ulong AvailPageFile;
            public ulong TotalVirtual;
            public ulong AvailVirtual;
            public ulong AvailExtendedVirtual;

            public MemoryStatusEx()
            {
                Length = (uint)Marshal.SizeOf<MemoryStatusEx>();
            }
        }

        [LibraryImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool GlobalMemoryStatusEx(ref MemoryStatusEx lpBuffer);
        
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
