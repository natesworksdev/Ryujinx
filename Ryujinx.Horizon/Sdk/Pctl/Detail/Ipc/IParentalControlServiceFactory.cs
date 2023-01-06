using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Sf;

namespace Ryujinx.Horizon.Sdk.Pctl.Detail.Ipc
{
    interface IParentalControlServiceFactory : IServiceObject
    {
        Result CreateService(out IParentalControlService arg0, ulong arg1, ulong pid);
        Result CreateServiceWithoutInitialize(out IParentalControlService arg0, ulong arg1, ulong pid);
    }
}
