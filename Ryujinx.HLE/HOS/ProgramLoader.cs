using ChocolArm64.Memory;
using Ryujinx.Common;
using Ryujinx.Common.Logging;
using Ryujinx.HLE.HOS.Kernel;
using Ryujinx.HLE.Loaders.Executables;
using Ryujinx.HLE.Loaders.Npdm;

namespace Ryujinx.HLE.HOS
{
    class ProgramLoader
    {
        private const int ArgsHeaderSize = 8;
        private const int ArgsDataSize   = 0x9000;
        private const int ArgsTotalSize  = ArgsHeaderSize + ArgsDataSize;

        public static bool LoadStaticObjects(
            Horizon       System,
            Npdm          MetaData,
            IExecutable[] StaticObjects,
            byte[]        Arguments = null)
        {
            ulong ArgsStart = 0;
            int   ArgsSize  = 0;
            ulong CodeStart = 0x8000000;
            int   CodeSize  = 0;

            ulong[] NsoBase = new ulong[StaticObjects.Length];

            for (int Index = 0; Index < StaticObjects.Length; Index++)
            {
                IExecutable StaticObject = StaticObjects[Index];

                int TextEnd = StaticObject.TextOffset + StaticObject.Text.Length;
                int ROEnd   = StaticObject.ROOffset   + StaticObject.RO.Length;
                int DataEnd = StaticObject.DataOffset + StaticObject.Data.Length + StaticObject.BssSize;

                int NsoSize = TextEnd;

                if ((uint)NsoSize < (uint)ROEnd)
                {
                    NsoSize = ROEnd;
                }

                if ((uint)NsoSize < (uint)DataEnd)
                {
                    NsoSize = DataEnd;
                }

                NsoSize = BitUtils.AlignUp(NsoSize, KMemoryManager.PageSize);

                NsoBase[Index] = CodeStart + (ulong)CodeSize;

                CodeSize += NsoSize;

                if (Arguments != null && ArgsSize == 0)
                {
                    ArgsStart = (ulong)CodeSize;

                    ArgsSize = BitUtils.AlignDown(Arguments.Length * 2 + ArgsTotalSize - 1, KMemoryManager.PageSize);

                    CodeSize += ArgsSize;
                }
            }

            int CodePagesCount = CodeSize / KMemoryManager.PageSize;

            int PersonalMmHeapPagesCount = MetaData.PersonalMmHeapSize / KMemoryManager.PageSize;

            KProcess Process = new KProcess(System);

            ProcessCreationInfo CreationInfo = new ProcessCreationInfo(
                MetaData.TitleName,
                MetaData.ProcessCategory,
                MetaData.ACI0.TitleId,
                CodeStart,
                CodePagesCount,
                MetaData.MmuFlags,
                0,
                PersonalMmHeapPagesCount);

            KernelResult Result;

            KResourceLimit ResourceLimit = new KResourceLimit(System);

            long ApplicationRgSize = (long)System.MemoryRegions[(int)MemoryRegion.Application].Size;

            Result  = ResourceLimit.SetLimitValue(LimitableResource.Memory,         ApplicationRgSize);
            Result |= ResourceLimit.SetLimitValue(LimitableResource.Thread,         608);
            Result |= ResourceLimit.SetLimitValue(LimitableResource.Event,          700);
            Result |= ResourceLimit.SetLimitValue(LimitableResource.TransferMemory, 128);
            Result |= ResourceLimit.SetLimitValue(LimitableResource.Session,        894);

            if (Result != KernelResult.Success)
            {
                Logger.PrintError(LogClass.Loader, $"Process initialization failed setting resource limit values.");

                return false;
            }

            Result = Process.Initialize(
                CreationInfo,
                MetaData.ACI0.KernelAccessControl.Capabilities,
                ResourceLimit,
                MemoryRegion.Application);

            if (Result != KernelResult.Success)
            {
                Logger.PrintError(LogClass.Loader, $"Process initialization returned error \"{Result}\".");

                return false;
            }

            for (int Index = 0; Index < StaticObjects.Length; Index++)
            {
                IExecutable StaticObject = StaticObjects[Index];

                ulong TextStart = NsoBase[Index] + (ulong)StaticObject.TextOffset;
                ulong ROStart   = NsoBase[Index] + (ulong)StaticObject.ROOffset;
                ulong DataStart = NsoBase[Index] + (ulong)StaticObject.DataOffset;

                ulong BssStart = DataStart + (ulong)StaticObject.Data.Length;

                ulong BssEnd = BitUtils.AlignUp(BssStart + (ulong)StaticObject.BssSize, KMemoryManager.PageSize);

                Process.CpuMemory.WriteBytes((long)TextStart, StaticObject.Text);
                Process.CpuMemory.WriteBytes((long)ROStart,   StaticObject.RO);
                Process.CpuMemory.WriteBytes((long)DataStart, StaticObject.Data);

                MemoryHelper.FillWithZeros(Process.CpuMemory, (long)BssStart, (int)(BssEnd - BssStart));

                KMemoryManager MemMgr = Process.MemoryManager;

                Result  = MemMgr.SetProcessMemoryPermission(TextStart, ROStart   - TextStart, MemoryPermission.ReadAndExecute);
                Result |= MemMgr.SetProcessMemoryPermission(ROStart,   DataStart - ROStart,   MemoryPermission.Read);
                Result |= MemMgr.SetProcessMemoryPermission(DataStart, BssEnd    - DataStart, MemoryPermission.ReadAndWrite);

                if (Result != KernelResult.Success)
                {
                    Logger.PrintError(LogClass.Loader, $"Process initialization failed setting memory permissions.");

                    return false;
                }
            }

            Result = Process.Start(MetaData.MainThreadPriority, (ulong)MetaData.MainThreadStackSize);

            if (Result != KernelResult.Success)
            {
                Logger.PrintError(LogClass.Loader, $"Process start returned error \"{Result}\".");

                return false;
            }

            return true;
        }
    }
}