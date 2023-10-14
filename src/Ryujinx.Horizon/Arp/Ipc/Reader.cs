using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Arp;
using Ryujinx.Horizon.Sdk.Arp.Detail;
using Ryujinx.Horizon.Sdk.Ns;
using Ryujinx.Horizon.Sdk.Sf;
using Ryujinx.Horizon.Sdk.Sf.Hipc;
using System;

namespace Ryujinx.Horizon.Arp.Ipc
{
    partial class Reader : IReader, IServiceObject
    {
        private readonly ApplicationInstanceManager _applicationInstanceManager;

        public Reader(ApplicationInstanceManager applicationInstanceManager)
        {
            _applicationInstanceManager = applicationInstanceManager;
        }

        [CmifCommand(0)]
        public Result GetApplicationLaunchProperty(out ApplicationLaunchProperty applicationLaunchProperty, ulong applicationInstanceId)
        {
            // NOTE: Returns InvalidArgument if applicationLaunchProperty is null, doesn't occur in our case.
            //       Returns InvalidPointer if applicationInstanceId is null, doesn't occur in our case.

            if (_applicationInstanceManager.Entries[applicationInstanceId] == null)
            {
                applicationLaunchProperty = new ApplicationLaunchProperty();

                return ArpResult.InvalidInstanceId;
            }

            applicationLaunchProperty = (ApplicationLaunchProperty)_applicationInstanceManager.Entries[applicationInstanceId].LaunchProperty;

            return Result.Success;
        }

        [CmifCommand(1)]
        public Result GetApplicationControlProperty([Buffer(HipcBufferFlags.Out | HipcBufferFlags.MapAlias | HipcBufferFlags.FixedSize, 0x4000)] out ApplicationControlProperty applicationControlProperty, ulong applicationInstanceId)
        {
            // NOTE: Returns InvalidArgument if applicationControlProperty is null, doesn't occur in our case.
            //       Returns InvalidPointer if applicationInstanceId is null, doesn't occur in our case.

            if (_applicationInstanceManager.Entries[applicationInstanceId] == null)
            {
                applicationControlProperty = new ApplicationControlProperty();

                return ArpResult.InvalidInstanceId;
            }

            applicationControlProperty = (ApplicationControlProperty)_applicationInstanceManager.Entries[applicationInstanceId].ControlProperty;

            return Result.Success;
        }

        [CmifCommand(2)]
        public Result GetApplicationProcessProperty(out ApplicationProcessProperty applicationProcessProperty, ulong applicationInstanceId)
        {
            // NOTE: Returns InvalidArgument if applicationProcessProperty is null, doesn't occur in our case.
            //       Returns InvalidPointer if applicationInstanceId is null, doesn't occur in our case.

            if (_applicationInstanceManager.Entries[applicationInstanceId] == null)
            {
                applicationProcessProperty = new ApplicationProcessProperty();

                return ArpResult.InvalidInstanceId;
            }

            applicationProcessProperty = (ApplicationProcessProperty)_applicationInstanceManager.Entries[applicationInstanceId].ProcessProperty;

            return Result.Success;
        }

        [CmifCommand(3)]
        public Result GetApplicationInstanceId(out ulong applicationInstanceId, ulong pid)
        {
            // NOTE: Returns InvalidArgument if applicationInstanceId is null, doesn't occur in our case.

            applicationInstanceId = 0;

            if (pid == 0)
            {
                return ArpResult.InvalidPid;
            }

            for (int i = 0; i < _applicationInstanceManager.Entries.Length; i++)
            {
                if (_applicationInstanceManager.Entries[i] != null && _applicationInstanceManager.Entries[i].Pid == pid)
                {
                    applicationInstanceId = (ulong)i;

                    return Result.Success;
                }
            }

            return ArpResult.InvalidPid;
        }

        [CmifCommand(4)]
        public Result GetApplicationInstanceUnregistrationNotifier(out IUnregistrationNotifier unregistrationNotifier)
        {
            // NOTE: Returns AllocationFailed if it can't create the object, doesn't occur in our case.

            unregistrationNotifier = new UnregistrationNotifier(_applicationInstanceManager);

            return Result.Success;
        }

        [CmifCommand(5)]
        public Result ListApplicationInstanceId(out int counter, [Buffer(HipcBufferFlags.Out | HipcBufferFlags.MapAlias)] Span<ulong> applicationInstanceIdList)
        {
            // NOTE: Returns InvalidArgument if counter is null, doesn't occur in our case.

            counter = 0;

            if (_applicationInstanceManager.Entries[0] != null)
            {
                applicationInstanceIdList[counter++] = 0;
            }

            if (_applicationInstanceManager.Entries[1] != null)
            {
                applicationInstanceIdList[counter++] = 1;
            }

            return Result.Success;
        }

        [CmifCommand(6)]
        public Result GetMicroApplicationInstanceId(out ulong microApplicationInstanceId, [ClientProcessId] ulong pid)
        {
            return GetApplicationInstanceId(out microApplicationInstanceId, pid);
        }

        [CmifCommand(7)]
        public Result GetApplicationCertificate([Buffer(HipcBufferFlags.Out | HipcBufferFlags.MapAlias | HipcBufferFlags.FixedSize, 0x528)] out ApplicationCertificate applicationCertificate, ulong applicationInstanceId)
        {
            // NOTE: Returns InvalidArgument if applicationCertificate is null, doesn't occur in our case.
            //       Returns InvalidPointer if applicationInstanceId is null, doesn't occur in our case.

            if (_applicationInstanceManager.Entries[applicationInstanceId] == null)
            {
                applicationCertificate = new ApplicationCertificate();

                return ArpResult.InvalidInstanceId;
            }

            applicationCertificate = (ApplicationCertificate)_applicationInstanceManager.Entries[applicationInstanceId].Certificate;

            return Result.Success;
        }
    }
}
