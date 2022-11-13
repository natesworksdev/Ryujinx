using Ryujinx.Common.Logging;

namespace Ryujinx.HLE.HOS.Services.Hid
{
    [Service("hidbus")]
    class IHidbusServer : IpcService
    {
        public IHidbusServer(ServiceCtx context) { }

        [CommandHipc(1)]
        // GetBusHandle(nn::hidbus::IHidbusServer, nn::applet::AppletResourceUserId) 5.0.0+ 
        public ResultCode GetBusHandle(ServiceCtx context)
        {
            NpadIdType npadIdType        = (NpadIdType)context.RequestData.ReadInt32();
            context.RequestData.BaseStream.Position += 4; // Padding
            BusType busType              = (BusType)context.RequestData.ReadInt64();
            long appletResourceUserId = context.RequestData.ReadInt64();

            Logger.Stub?.PrintStub(LogClass.ServiceHid, new {npadIdType, busType, appletResourceUserId});

            return ResultCode.Success;
        }
    }
}