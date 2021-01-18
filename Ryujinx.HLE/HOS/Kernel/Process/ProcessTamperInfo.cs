using System.Collections.Generic;

namespace Ryujinx.HLE.HOS.Kernel.Process
{
    struct ProcessTamperInfo
    {
        public long Pid { get; }

        public IEnumerable<string> BuildIds { get; }
        public IEnumerable<ulong> CodeAddresses { get; }

        public ProcessTamperInfo(
            long pid,
            IEnumerable<string> buildIds,
            IEnumerable<ulong> codeAddresses)
        {
            Pid = pid;
            BuildIds = buildIds;
            CodeAddresses = codeAddresses;
        }
    }
}