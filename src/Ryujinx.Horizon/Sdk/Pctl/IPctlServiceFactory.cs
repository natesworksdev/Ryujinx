using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Sf;

namespace Ryujinx.Horizon.Sdk.Pctl
{
    internal interface IPctlServiceFactory : IServiceObject
    {
        Result CreateService(out IPctlService arg0, ulong arg1, ulong pid);
        Result CreateServiceWithoutInitialize(out IPctlService arg0, ulong arg1, ulong pid);
    }
}
