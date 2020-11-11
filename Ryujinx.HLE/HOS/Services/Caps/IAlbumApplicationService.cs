using Ryujinx.Common.Logging;

namespace Ryujinx.HLE.HOS.Services.Caps
{
    [Service("caps:u")]
    class IAlbumApplicationService : IpcService
    {
        public IAlbumApplicationService(ServiceCtx context) { }

        [Command(32)] // 7.0.0+
        // SetShimLibraryVersion(pid, u64, nn::applet::AppletResourceUserId)
        public ResultCode SetShimLibraryVersion(ServiceCtx context)
        {
            ulong shimLibraryVersion   = context.RequestData.ReadUInt64();
            ulong appletResourceUserId = context.RequestData.ReadUInt64();

            Logger.Stub?.Print(LogClass.ServiceCaps, new { shimLibraryVersion, appletResourceUserId });

            return ResultCode.Success;
        }
    }
}