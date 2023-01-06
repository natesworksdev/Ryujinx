using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Pctl.Detail.Ipc;
using Ryujinx.Horizon.Sdk.Sf;

namespace Ryujinx.Horizon.Pctl
{
    partial class ParentalControlServiceFactory : IParentalControlServiceFactory
    {
        private int _permissionFlag;

        [CmifCommand(0)]
        public Result CreateService(out IParentalControlService pctlSerivce, ulong arg1, [ClientProcessId] ulong pid)
        {
            pctlSerivce = new ParentalControlService(pid, true, _permissionFlag);

            return Result.Success;
        }

        [CmifCommand(1)] // 4.0.0+
        public Result CreateServiceWithoutInitialize(out IParentalControlService pctlSerivce, ulong arg1, [ClientProcessId] ulong pid)
        {
            pctlSerivce = new ParentalControlService(pid, false, _permissionFlag);

            return Result.Success;
        }
    }
}