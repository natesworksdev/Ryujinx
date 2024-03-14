using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Ncm;
using Ryujinx.Horizon.Sdk.Sf;

namespace Ryujinx.Horizon.Sdk.Am.Controllers
{
    interface IApplicationCreator : IServiceObject
    {
        Result CreateApplication(out IApplicationAccessor accessor, ApplicationId applicationId);
        Result PopLaunchRequestedApplication(out IApplicationAccessor accessor);
        Result CreateSystemApplication(out IApplicationAccessor accessor, ApplicationId applicationId);
        Result PopFloatingApplicationForDevelopment(out IApplicationAccessor accessor);
    }
}
