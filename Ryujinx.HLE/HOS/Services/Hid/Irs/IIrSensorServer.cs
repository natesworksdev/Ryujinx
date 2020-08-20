using Ryujinx.Common.Logging;
using Ryujinx.HLE.HOS.Ipc;
using Ryujinx.HLE.HOS.Services.Hid.HidServer;

namespace Ryujinx.HLE.HOS.Services.Hid.Irs
{
    [Service("irs")]
    class IIrSensorServer : IpcService
    {
        public IIrSensorServer(ServiceCtx context) { }

        [Command(302)]
        // ActivateIrsensor(nn::applet::AppletResourceUserId, pid)
        public ResultCode ActivateIrsensor(ServiceCtx context)
        {
            long appletResourceUserId = context.RequestData.ReadInt64();

            Logger.Stub?.PrintStub(LogClass.ServiceIrs, new { appletResourceUserId });

            return ResultCode.Success;
        }

        [Command(303)]
        // DeactivateIrsensor(nn::applet::AppletResourceUserId, pid)
        public ResultCode DeactivateIrsensor(ServiceCtx context)
        {
            long appletResourceUserId = context.RequestData.ReadInt64();

            Logger.Stub?.PrintStub(LogClass.ServiceIrs, new { appletResourceUserId });

            return ResultCode.Success;
        }

        [Command(304)]
        // GetIrsensorSharedMemoryHandle(nn::applet::AppletResourceUserId, pid) -> handle<copy>
        public ResultCode GetIrsensorSharedMemoryHandle(ServiceCtx context)
        {
            context.Response.HandleDesc = IpcHandleDesc.MakeCopy(context.Device.System.ServiceServer.HidServer.IrSharedMemoryHandle);

            return ResultCode.Success;
        }

        [Command(311)]
        // GetNpadIrCameraHandle(u32) -> nn::irsensor::IrCameraHandle
        public ResultCode GetNpadIrCameraHandle(ServiceCtx context)
        {
            NpadIdType npadIdType = (NpadIdType)context.RequestData.ReadUInt32();

            if (npadIdType >  NpadIdType.Player8 && 
                npadIdType != NpadIdType.Unknown && 
                npadIdType != NpadIdType.Handheld)
            {
                return ResultCode.NpadIdOutOfRange;
            }

            PlayerIndex irCameraHandle = HidUtils.GetIndexFromNpadIdType(npadIdType);

            context.ResponseData.Write((int)irCameraHandle);

            // NOTE: If the irCameraHandle pointer is null this error is returned, Doesn't occur in our case. 
            //       return ResultCode.HandlePointerIsNull;

            return ResultCode.Success;
        }

        [Command(319)] // 4.0.0+
        // ActivateIrsensorWithFunctionLevel(nn::applet::AppletResourceUserId, nn::irsensor::PackedFunctionLevel, pid)
        public ResultCode ActivateIrsensorWithFunctionLevel(ServiceCtx context)
        {
            long appletResourceUserId = context.RequestData.ReadInt64();
            long packedFunctionLevel  = context.RequestData.ReadInt64();

            Logger.Stub?.PrintStub(LogClass.ServiceIrs, new { appletResourceUserId, packedFunctionLevel });

            return ResultCode.Success;
        }
    }
}