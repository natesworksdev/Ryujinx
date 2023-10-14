using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Arp;
using Ryujinx.Horizon.Sdk.Arp.Detail;
using Ryujinx.Horizon.Sdk.Sf;
using Ryujinx.Horizon.Sdk.Sf.Hipc;
using System;

namespace Ryujinx.Horizon.Arp.Ipc
{
    partial class Updater : IUpdater, IServiceObject
    {
        private readonly ApplicationInstanceManager _applicationInstanceManager;
        private readonly ulong _applicationInstanceId;

        public Updater(ApplicationInstanceManager applicationInstanceManager, ulong applicationInstanceId)
        {
            _applicationInstanceManager = applicationInstanceManager;
            _applicationInstanceId = applicationInstanceId;
        }

        [CmifCommand(0)]
        public Result Issue()
        {
            throw new NotImplementedException();
        }

        [CmifCommand(1)]
        public Result SetApplicationProcessProperty(ulong pid, ApplicationProcessProperty applicationProcessProperty)
        {
            if (pid == 0)
            {
                return ArpResult.InvalidPid;
            }

            // NOTE: Returns InvalidPointer if _applicationInstanceId is null, doesn't occur in our case.

            _applicationInstanceManager.Entries[_applicationInstanceId].Pid = pid;
            _applicationInstanceManager.Entries[_applicationInstanceId].ProcessProperty = applicationProcessProperty;

            return Result.Success;
        }

        [CmifCommand(2)]
        public Result DeleteApplicationProcessProperty()
        {
            // NOTE: Returns InvalidPointer if _applicationInstanceId is null, doesn't occur in our case.

            _applicationInstanceManager.Entries[_applicationInstanceId].ProcessProperty = new ApplicationProcessProperty();

            return Result.Success;
        }

        [CmifCommand(3)]
        public Result SetApplicationCertificate([Buffer(HipcBufferFlags.In | HipcBufferFlags.AutoSelect)] ApplicationCertificate applicationCertificate)
        {
            // NOTE: Does nothing in original service.

            return ArpResult.DataAlreadyBound;
        }
    }
}
