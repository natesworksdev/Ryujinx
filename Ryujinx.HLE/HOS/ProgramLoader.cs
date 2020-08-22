using ARMeilleure.Translation.PTC;
using Ryujinx.Common;
using Ryujinx.Common.Logging;
using Ryujinx.Cpu;
using Ryujinx.HLE.Loaders.Executables;
using Ryujinx.HLE.Loaders.Npdm;
using Ryujinx.Horizon.Kernel;
using Ryujinx.Horizon.Kernel.Common;
using Ryujinx.Horizon.Kernel.Memory;
using Ryujinx.Horizon.Kernel.Svc;

namespace Ryujinx.HLE.HOS
{
    static class ProgramLoader
    {
        private const bool AslrEnabled = true;

        private const int PageSize       = 0x1000;
        private const int ArgsHeaderSize = 8;
        private const int ArgsDataSize   = 0x9000;
        private const int ArgsTotalSize  = ArgsHeaderSize + ArgsDataSize;

        public static bool LoadKip(Switch device, KipExecutable kip)
        {
            // TODO.
            return false;

            /* int endOffset = kip.DataOffset + kip.Data.Length;

            if (kip.BssSize != 0)
            {
                endOffset = kip.BssOffset + kip.BssSize;
            }

            int codeSize = BitUtils.AlignUp(kip.TextOffset + endOffset, KMemoryManager.PageSize);

            int codePagesCount = codeSize / KMemoryManager.PageSize;

            ulong codeBaseAddress = kip.Is64BitAddressSpace ? 0x8000000UL : 0x200000UL;

            ulong codeAddress = codeBaseAddress + (ulong)kip.TextOffset;

            ProcessCreationFlags flags = 0;

            if (AslrEnabled)
            {
                // TODO: Randomization.

                flags |= ProcessCreationFlags.EnableAslr;
            }

            if (kip.Is64BitAddressSpace)
            {
                flags |= ProcessCreationFlags.AddressSpace64Bit;
            }

            if (kip.Is64Bit)
            {
                flags |= ProcessCreationFlags.Is64Bit;
            }

            ProcessCreationInfo creationInfo = new ProcessCreationInfo(
                kip.Name,
                kip.Version,
                kip.ProgramId,
                codeAddress,
                codePagesCount,
                flags,
                0,
                0);

            MemoryRegion memoryRegion = kip.UsesSecureMemory
                ? MemoryRegion.Service
                : MemoryRegion.Application;

            KMemoryRegionManager region = device.System.KernelContext.MemoryRegions[(int)memoryRegion];

            KernelResult result = region.AllocatePages((ulong)codePagesCount, false, out KPageList pageList);

            if (result != KernelResult.Success)
            {
                Logger.Error?.Print(LogClass.Loader, $"Process initialization returned error \"{result}\".");

                return false;
            }

            KProcess process = new KProcess(device.System.KernelContext);

            var processContextFactory = new ArmProcessContextFactory(device);

            result = process.InitializeKip(
                creationInfo,
                kip.Capabilities,
                pageList,
                device.System.KernelContext.ResourceLimit,
                memoryRegion,
                processContextFactory);

            if (result != KernelResult.Success)
            {
                Logger.Error?.Print(LogClass.Loader, $"Process initialization returned error \"{result}\".");

                return false;
            }

            result = LoadIntoMemory(process, kip, codeBaseAddress);

            if (result != KernelResult.Success)
            {
                Logger.Error?.Print(LogClass.Loader, $"Process initialization returned error \"{result}\".");

                return false;
            }

            process.DefaultCpuCore = kip.IdealCoreId;

            result = process.Start(kip.Priority, (ulong)kip.StackSize);

            if (result != KernelResult.Success)
            {
                Logger.Error?.Print(LogClass.Loader, $"Process start returned error \"{result}\".");

                return false;
            }

            device.System.KernelContext.Processes.TryAdd(process.Pid, process);

            return true; */
        }

        public static bool LoadNsos(
            Switch device,
            Npdm metaData,
            byte[] arguments = null,
            params IExecutable[] executables)
        {
            ulong argsStart = 0;
            int   argsSize  = 0;
            ulong codeStart = metaData.Is64Bit ? 0x8000000UL : 0x200000UL;
            int   codeSize  = 0;

            ulong[] nsoBase = new ulong[executables.Length];

            for (int index = 0; index < executables.Length; index++)
            {
                IExecutable staticObject = executables[index];

                int textEnd = staticObject.TextOffset + staticObject.Text.Length;
                int roEnd   = staticObject.RoOffset   + staticObject.Ro.Length;
                int dataEnd = staticObject.DataOffset + staticObject.Data.Length + staticObject.BssSize;

                int nsoSize = textEnd;

                if ((uint)nsoSize < (uint)roEnd)
                {
                    nsoSize = roEnd;
                }

                if ((uint)nsoSize < (uint)dataEnd)
                {
                    nsoSize = dataEnd;
                }

                nsoSize = BitUtils.AlignUp(nsoSize, PageSize);

                nsoBase[index] = codeStart + (ulong)codeSize;

                codeSize += nsoSize;

                if (arguments != null && argsSize == 0)
                {
                    argsStart = (ulong)codeSize;

                    argsSize = BitUtils.AlignDown(arguments.Length * 2 + ArgsTotalSize - 1, PageSize);

                    codeSize += argsSize;
                }
            }

            PtcProfiler.StaticCodeStart = codeStart;
            PtcProfiler.StaticCodeSize  = codeSize;

            int codePagesCount = codeSize / PageSize;

            int personalMmHeapPagesCount = metaData.PersonalMmHeapSize / PageSize;

            KernelResult result = KernelStatic.Syscall.CreateResourceLimit(out int resourceLimitHandle);

            result |= KernelStatic.Syscall.SetResourceLimitLimitValue(resourceLimitHandle, LimitableResource.Memory, 0xcd500000L);
            result |= KernelStatic.Syscall.SetResourceLimitLimitValue(resourceLimitHandle, LimitableResource.Thread, 608);
            result |= KernelStatic.Syscall.SetResourceLimitLimitValue(resourceLimitHandle, LimitableResource.Event, 700);
            result |= KernelStatic.Syscall.SetResourceLimitLimitValue(resourceLimitHandle, LimitableResource.TransferMemory, 128);
            result |= KernelStatic.Syscall.SetResourceLimitLimitValue(resourceLimitHandle, LimitableResource.Session, 894);

            if (result != KernelResult.Success)
            {
                Logger.Error?.Print(LogClass.Loader, $"Process initialization failed setting resource limit values.");

                return false;
            }

            ProcessCreationFlags memoryRegion = (ProcessCreationFlags)(((metaData.Acid.Flags >> 2) & 0xf) << (int)ProcessCreationFlags.PoolPartitionShift);

            ProcessCreationInfo creationInfo = new ProcessCreationInfo(
                metaData.TitleName,
                metaData.Version,
                metaData.Aci0.TitleId,
                codeStart,
                codePagesCount,
                (ProcessCreationFlags)metaData.ProcessFlags | ProcessCreationFlags.IsApplication | memoryRegion,
                resourceLimitHandle,
                personalMmHeapPagesCount);

            var processContextFactory = new ArmProcessContextFactory(device);

            result = KernelStatic.Syscall.CreateProcess(
                creationInfo,
                metaData.Aci0.KernelAccessControl.Capabilities,
                out int processHandle,
                processContextFactory);

            if (result != KernelResult.Success)
            {
                Logger.Error?.Print(LogClass.Loader, $"Process initialization returned error \"{result}\".");

                return false;
            }

            for (int index = 0; index < executables.Length; index++)
            {
                Logger.Info?.Print(LogClass.Loader, $"Loading image {index} at 0x{nsoBase[index]:x16}...");

                result = LoadIntoMemory(processHandle, executables[index], nsoBase[index]);

                if (result != KernelResult.Success)
                {
                    Logger.Error?.Print(LogClass.Loader, $"Process initialization returned error \"{result}\".");

                    return false;
                }
            }

            result = KernelStatic.Syscall.StartProcess(
                processHandle,
                metaData.MainThreadPriority,
                metaData.DefaultCpuId,
                (ulong)metaData.MainThreadStackSize);

            if (result != KernelResult.Success)
            {
                Logger.Error?.Print(LogClass.Loader, $"Process start returned error \"{result}\".");

                return false;
            }

            return true;
        }

        private static KernelResult LoadIntoMemory(int processHandle, IExecutable image, ulong baseAddress)
        {
            ulong textStart = baseAddress + (ulong)image.TextOffset;
            ulong roStart   = baseAddress + (ulong)image.RoOffset;
            ulong dataStart = baseAddress + (ulong)image.DataOffset;
            ulong bssStart  = baseAddress + (ulong)image.BssOffset;

            ulong end = dataStart + (ulong)image.Data.Length;

            if (image.BssSize != 0)
            {
                end = bssStart + (ulong)image.BssSize;
            }

            var memory = KernelStatic.GetAddressSpace(processHandle);

            memory.Write(textStart, image.Text);
            memory.Write(roStart,   image.Ro);
            memory.Write(dataStart, image.Data);

            MemoryHelper.FillWithZeros(memory, (long)bssStart, image.BssSize);

            KernelResult SetProcessMemoryPermission(ulong address, ulong size, KMemoryPermission permission)
            {
                if (size == 0)
                {
                    return KernelResult.Success;
                }

                size = BitUtils.AlignUp(size, PageSize);

                return KernelStatic.Syscall.SetProcessMemoryPermission(processHandle, address, size, permission);
            }

            KernelResult result = SetProcessMemoryPermission(textStart, (ulong)image.Text.Length, KMemoryPermission.ReadAndExecute);

            if (result != KernelResult.Success)
            {
                return result;
            }

            result = SetProcessMemoryPermission(roStart, (ulong)image.Ro.Length, KMemoryPermission.Read);

            if (result != KernelResult.Success)
            {
                return result;
            }

            return SetProcessMemoryPermission(dataStart, end - dataStart, KMemoryPermission.ReadAndWrite);
        }
    }
}