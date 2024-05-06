using Ryujinx.Common.Logging;
using Ryujinx.Horizon.Bcat.Types;
using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Bcat;
using Ryujinx.Horizon.Sdk.Sf;

namespace Ryujinx.Horizon.Bcat.Ipc
{
    partial class BcatService : IBcatService
    {
        public BcatService(BcatServicePermissionLevel permissionLevel) { }

        [CmifCommand(10100)]
        public Result RequestSyncDeliveryCache(out IDeliveryCacheProgressService deliveryCacheProgressService)
        {
            deliveryCacheProgressService = new DeliveryCacheProgressService();

            return Result.Success;
        }

        [CmifCommand(10101)]
        public Result RequestSyncDeliveryCacheWithDirectoryName(out IDeliveryCacheProgressService deliveryCacheProgressService)
        {
            // Temporary fix for Endless Ocean Luminous (010067B017588000).
            // Just pretend the network request failed and pretend that everything is fine.
            deliveryCacheProgressService = new DeliveryCacheProgressService();

            return BcatResult.InternetRequestDenied;
        }
    }
}
