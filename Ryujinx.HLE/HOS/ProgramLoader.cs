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
            long ArgsStart = 0;
            int  ArgsSize  = 0;
            long CodeStart = 0x8000000;
            int  CodeSize  = 0;

            long[] NsoBase = new long[StaticObjects.Length];

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

                NsoBase[Index] = CodeStart + CodeSize;

                CodeSize += NsoSize;

                if (Arguments != null && ArgsSize == 0)
                {
                    ArgsStart = CodeSize;

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

            KernelResult Result = Process.Initialize(
                CreationInfo,
                MetaData.ACI0.KernelAccessControl.Capabilities,
                System.ResourceLimit,
                MemoryRegion.Application);

            if (Result != KernelResult.Success)
            {
                Logger.PrintError(LogClass.KernelSvc, $"Process initialization returned error \"{Result}\".");

                return false;
            }

            for (int Index = 0; Index < StaticObjects.Length; Index++)
            {
                IExecutable StaticObject = StaticObjects[Index];

                long TextStart = NsoBase[Index] + StaticObject.TextOffset;
                long ROStart   = NsoBase[Index] + StaticObject.ROOffset;
                long DataStart = NsoBase[Index] + StaticObject.DataOffset;

                long BssStart = DataStart + StaticObject.Data.Length;

                long BssEnd = BitUtils.AlignUp(BssStart + StaticObject.BssSize, KMemoryManager.PageSize);

                Process.CpuMemory.WriteBytes(TextStart, StaticObject.Text);
                Process.CpuMemory.WriteBytes(ROStart,   StaticObject.RO);
                Process.CpuMemory.WriteBytes(DataStart, StaticObject.Data);

                 MemoryHelper.FillWithZeros(Process.CpuMemory, BssStart, (int)(BssEnd - BssStart));

                Process.MemoryManager.SetProcessMemoryPermission(TextStart, ROStart   - TextStart, MemoryPermission.ReadAndExecute);
                Process.MemoryManager.SetProcessMemoryPermission(ROStart,   DataStart - ROStart,   MemoryPermission.Read);
                Process.MemoryManager.SetProcessMemoryPermission(DataStart, BssEnd    - DataStart, MemoryPermission.ReadAndWrite);
            }

            Result = Process.Start(MetaData.MainThreadPriority, MetaData.MainThreadStackSize);

            if (Result != KernelResult.Success)
            {
                Logger.PrintError(LogClass.KernelSvc, $"Process start returned error \"{Result}\".");

                return false;
            }

            return true;
        }
    }
}