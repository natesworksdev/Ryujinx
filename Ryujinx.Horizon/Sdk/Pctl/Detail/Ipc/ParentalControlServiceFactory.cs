using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Sf;

namespace Ryujinx.Horizon.Sdk.Pctl.Detail.Ipc
{
    partial class ParentalControlServiceFactory : IParentalControlServiceFactory
    {
        [CmifCommand(0)]
        public Result CreateService(out IParentalControlService arg0, ulong arg1, [ClientProcessId] ulong pid)
        {
            arg0 = new ParentalControlService();

            return Result.Success;
        }

        [CmifCommand(1)]
        public Result CreateServiceWithoutInitialize(out IParentalControlService arg0, ulong arg1, [ClientProcessId] ulong pid)
        {
            arg0 = new ParentalControlService();

            return Result.Success;
        }
    }
}