using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Sf;
using System.Collections.Generic;

namespace Ryujinx.Horizon.Sdk.Pctl.Detail.Ipc
{
    class ParentalControlServiceFactory : IParentalControlServiceFactory
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

        public IReadOnlyDictionary<int, CommandHandler> GetCommandHandlers()
        {
            throw new System.NotImplementedException();
        }
    }
}