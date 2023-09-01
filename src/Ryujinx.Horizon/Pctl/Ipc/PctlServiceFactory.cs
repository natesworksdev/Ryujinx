using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Pctl;
using Ryujinx.Horizon.Sdk.Sf;

namespace Ryujinx.Horizon.Pctl.Ipc
{
    partial class PctlServiceFactory : IPctlServiceFactory
    {
        private readonly int _permissionFlag;

        public PctlServiceFactory(int permissionFlag)
        {
            _permissionFlag = permissionFlag;
        }

        [CmifCommand(0)]
        public Result CreateService(out IPctlService pctlService, ulong arg1, [ClientProcessId] ulong pid)
        {
            pctlService = new PctlService(pid, true, _permissionFlag);

            return Result.Success;
        }

        [CmifCommand(1)] // 4.0.0+
        public Result CreateServiceWithoutInitialize(out IPctlService pctlService, ulong arg1, [ClientProcessId] ulong pid)
        {
            pctlService = new PctlService(pid, false, _permissionFlag);

            return Result.Success;
        }
    }
}
