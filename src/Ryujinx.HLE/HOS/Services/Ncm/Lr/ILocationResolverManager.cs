using LibHac.Ncm;
using Ryujinx.HLE.HOS.Services.Ncm.Lr.LocationResolverManager;

namespace Ryujinx.HLE.HOS.Services.Ncm.Lr
{
    [Service("lr")]
    class ILocationResolverManager : IpcService
    {
#pragma warning disable IDE0060
        public ILocationResolverManager(ServiceCtx context) { }
#pragma warning restore IDE0060

        [CommandCmif(0)]
        // OpenLocationResolver()
        public ResultCode OpenLocationResolver(ServiceCtx context)
        {
            StorageId storageId = (StorageId)context.RequestData.ReadByte();

            MakeObject(context, new ILocationResolver(storageId));

            return ResultCode.Success;
        }
    }
}