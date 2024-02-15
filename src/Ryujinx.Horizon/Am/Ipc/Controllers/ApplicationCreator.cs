using Ryujinx.Common.Logging;
using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Am.Controllers;
using Ryujinx.Horizon.Sdk.Sf;
using System;
using ApplicationId = Ryujinx.Horizon.Sdk.Ncm.ApplicationId;

namespace Ryujinx.Horizon.Am.Ipc.Controllers
{
    partial class ApplicationCreator : IApplicationCreator
    {
        [CmifCommand(0)]
        public Result CreateApplication(out IApplicationAccessor accessor, ApplicationId applicationId)
        {
            accessor = new ApplicationAccessor(applicationId);
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(1)]
        public Result PopLaunchRequestedApplication(out IApplicationAccessor accessor)
        {
            throw new NotImplementedException();
        }

        [CmifCommand(10)]
        public Result CreateSystemApplication(out IApplicationAccessor accessor, ApplicationId applicationId)
        {
            accessor = new ApplicationAccessor(applicationId);
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(100)]
        public Result PopFloatingApplicationForDevelopment(out IApplicationAccessor accessor)
        {
            throw new NotImplementedException();
        }
    }
}
