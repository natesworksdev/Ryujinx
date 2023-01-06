using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Pctl.Detail.Ipc;
using Ryujinx.Horizon.Sdk.Sf;

namespace Ryujinx.Horizon.Pctl
{
    partial class ParentalControlServiceFactory : IParentalControlServiceFactory
    {
        [CmifCommand(0)]
        public Result CreateService(out IParentalControlService pctlSerivce, ulong arg1, [ClientProcessId] ulong pid)
        {
            pctlSerivce = new ParentalControlService();

            return Result.Success;
        }

        [CmifCommand(1)] // 4.0.0+
        public Result CreateServiceWithoutInitialize(out IParentalControlService pctlSerivce, ulong arg1, [ClientProcessId] ulong pid)
        {
            pctlSerivce = new ParentalControlService();

            return Result.Success;
        }
    }
}