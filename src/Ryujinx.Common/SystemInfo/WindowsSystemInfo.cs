using Ryujinx.Common.Logging;
using System;
using System.Management;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Ryujinx.Common.SystemInfo
{
    [SupportedOSPlatform("windows")]
    partial class WindowsSystemInfo : SystemInfo
    {
        internal WindowsSystemInfo()
        {
            CpuName = $"{GetCpuidCpuName() ?? GetCpuNameWmi()} ; {GetPhysicalCoreCount()} physical ; {LogicalCoreCount} logical";
            (RamTotal, RamAvailable) = GetMemoryStats();
        }

        private static string GetCpuNameWmi()
        {
            ManagementObjectCollection cpuObjs = GetWmiObjects("root\\CIMV2", "SELECT Name FROM Win32_Processor");

            if (cpuObjs != null)
            {
                foreach (var cpuObj in cpuObjs)
                {
                    return cpuObj["Name"].ToString().Trim();
                }
            }

            return Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER").Trim();
        }

        private static new int GetPhysicalCoreCount()
        {
            uint buffSize = 0;
            GetLogicalProcessorInformation(IntPtr.Zero, ref buffSize);
            IntPtr buffer = Marshal.AllocHGlobal((int)buffSize);
            bool success = GetLogicalProcessorInformation(buffer, ref buffSize);
            if (!success)
            {
                Marshal.FreeHGlobal(buffer);
                return LogicalCoreCount;
            }

            int physicalCores = 0;
            long pos = buffer.ToInt64();
            int size = Marshal.SizeOf(typeof(SystemLogicalProcessorInformation));
            for (long offset = 0; offset + size <= buffSize; offset += size)
            {
                IntPtr current = new IntPtr(pos + offset);
                SystemLogicalProcessorInformation info = Marshal.PtrToStructure<SystemLogicalProcessorInformation>(current);

                if (info.Relationship == LogicalProcessorRelationship.RelationProcessorCore)
                {
                    physicalCores++;
                }
            }

            Marshal.FreeHGlobal(buffer);

            return physicalCores;
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

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetLogicalProcessorInformation(IntPtr buffer, ref uint returnLength);

        [StructLayout(LayoutKind.Sequential)]
        public struct SystemLogicalProcessorInformation
        {
            public UIntPtr ProcessorMask;
            public LogicalProcessorRelationship Relationship;
            public ProcessorInformationUnion ProcessorInformation;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct ProcessorInformationUnion
        {
            [FieldOffset(8)]
            private UInt64 Reserved2;
        }

        public enum LogicalProcessorRelationship
        {
            RelationProcessorCore,
        }

        private static ManagementObjectCollection GetWmiObjects(string scope, string query)
        {
            try
            {
                return new ManagementObjectSearcher(scope, query).Get();
            }
            catch (PlatformNotSupportedException ex)
            {
                Logger.Error?.Print(LogClass.Application, $"WMI isn't available : {ex.Message}");
            }
            catch (COMException ex)
            {
                Logger.Error?.Print(LogClass.Application, $"WMI isn't available : {ex.Message}");
            }

            return null;
        }
    }
}