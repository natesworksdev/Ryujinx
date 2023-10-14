using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Arp;
using Ryujinx.Horizon.Sdk.Arp.Detail;
using Ryujinx.Horizon.Sdk.OsTypes;
using Ryujinx.Horizon.Sdk.Sf;

namespace Ryujinx.Horizon.Arp.Ipc
{
    partial class Writer : IWriter, IServiceObject
    {
        private readonly ApplicationInstanceManager _applicationInstanceManager;

        public Writer(ApplicationInstanceManager applicationInstanceManager)
        {
            _applicationInstanceManager = applicationInstanceManager;
        }

        [CmifCommand(0)]
        public Result AcquireRegistrar(out IRegistrar registrar)
        {
            // NOTE: Returns AllocationFailed if it can't create the object, doesn't occur in our case.

            if (_applicationInstanceManager.Entries[0] != null)
            {
                if (_applicationInstanceManager.Entries[1] != null)
                {
                    registrar = null;

                    return ArpResult.NoFreeInstance;
                }
                else
                {
                    _applicationInstanceManager.Entries[1] = new ApplicationInstance();

                    registrar = new Registrar(_applicationInstanceManager.Entries[1]);
                }
            }
            else
            {
                _applicationInstanceManager.Entries[0] = new ApplicationInstance();

                registrar = new Registrar(_applicationInstanceManager.Entries[0]);
            }

            return Result.Success;
        }

        [CmifCommand(1)]
        public Result UnregisterApplicationInstance(ulong applicationInstanceId)
        {
            // NOTE: Returns InvalidPointer if applicationInstanceId is null, doesn't occur in our case.

            if (_applicationInstanceManager.Entries[applicationInstanceId] != null)
            {
                _applicationInstanceManager.Entries[applicationInstanceId] = null;
            }

            Os.SignalSystemEvent(ref _applicationInstanceManager.SystemEvent);

            return Result.Success;
        }

        [CmifCommand(2)]
        public Result AcquireApplicationProcessPropertyUpdater(out IUpdater updater, ulong applicationInstanceId)
        {
            // NOTE: Returns InvalidPointer if applicationInstanceId is null, doesn't occur in our case.
            //       Returns AllocationFailed if it can't create the object, doesn't occur in our case.

            updater = new Updater(_applicationInstanceManager, applicationInstanceId);

            return Result.Success;
        }

        [CmifCommand(3)]
        public Result AcquireApplicationCertificateUpdater(out IUpdater updater, ulong applicationInstanceId)
        {
            // NOTE: Returns InvalidPointer if applicationInstanceId is null, doesn't occur in our case.
            //       Returns AllocationFailed if it can't create the object, doesn't occur in our case.

            updater = new Updater(_applicationInstanceManager, applicationInstanceId);

            return Result.Success;
        }
    }
}
