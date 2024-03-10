using Ryujinx.Common;
using Ryujinx.Common.Logging;
using Ryujinx.HLE.HOS.Ipc;
using Ryujinx.HLE.HOS.Services.Hid.HidServer;
using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Hid.Npad;
using System;

namespace Ryujinx.HLE.HOS.Services.Hid.Irs
{
    [Service("irs")]
    class IIrSensorServer : IpcService
    {
        private int _irsensorSharedMemoryHandle = 0;

        public IIrSensorServer(ServiceCtx context) { }

        [CommandCmif(304)]
        // GetIrsensorSharedMemoryHandle(nn::applet::AppletResourceUserId, pid) -> handle<copy>
        public ResultCode GetIrsensorSharedMemoryHandle(ServiceCtx context)
        {
            // NOTE: Shared memory should use the appletResourceUserId.
            // ulong appletResourceUserId = context.RequestData.ReadUInt64();

            if (_irsensorSharedMemoryHandle == 0)
            {
                if (context.Process.HandleTable.GenerateHandle(context.Device.System.IirsSharedMem, out _irsensorSharedMemoryHandle) != Result.Success)
                {
                    throw new InvalidOperationException("Out of handles!");
                }
            }

            context.Response.HandleDesc = IpcHandleDesc.MakeCopy(_irsensorSharedMemoryHandle);

            return ResultCode.Success;
        }

        [CommandCmif(309)]
        // GetImageTransferProcessorState(pid, nn::irsensor::IrCameraHandle, nn::applet::AppletResourceUserId)
        public ResultCode GetImageTransferProcessorState(ServiceCtx context)
        {
            IrCameraHandle irCameraHandle = context.RequestData.ReadStruct<IrCameraHandle>();
            ulong appletResourceUserId = context.RequestData.ReadUInt64();

            // ulong imageTransferBufferAddress = context.Request.ReceiveBuff[0].Position;
            ulong imageTransferBufferSize = context.Request.ReceiveBuff[0].Size;

            if (imageTransferBufferSize == 0)
            {
                return ResultCode.InvalidBufferSize;
            }

            CheckCameraHandle(irCameraHandle);

            Logger.Stub?.PrintStub(LogClass.ServiceIrs, new { appletResourceUserId, irCameraHandle.PlayerNumber, irCameraHandle.DeviceType });

            // TODO: Uses the buffer to copy the JoyCon IR data (by using a JoyCon driver) and update the following struct.
            context.ResponseData.WriteStruct(new ImageTransferProcessorState()
            {
                SamplingNumber = 0,
                AmbientNoiseLevel = 0,
            });

            return ResultCode.Success;
        }

        [CommandCmif(310)]
        // RunTeraPluginProcessor(pid, nn::irsensor::IrCameraHandle, nn::applet::AppletResourceUserId, PackedTeraPluginProcessorConfig)
        public ResultCode RunTeraPluginProcessor(ServiceCtx context)
        {
            IrCameraHandle irCameraHandle = context.RequestData.ReadStruct<IrCameraHandle>();
            ulong appletResourceUserId = context.RequestData.ReadUInt64();
            var packedTeraPluginProcessorConfig = context.RequestData.ReadStruct<PackedTeraPluginProcessorConfig>();

            CheckCameraHandle(irCameraHandle);

            Logger.Stub?.PrintStub(LogClass.ServiceIrs, new { appletResourceUserId, irCameraHandle.PlayerNumber, irCameraHandle.DeviceType, packedTeraPluginProcessorConfig.RequiredMcuVersion });

            return ResultCode.Success;
        }

        [CommandCmif(311)]
        // GetNpadIrCameraHandle(u32) -> nn::irsensor::IrCameraHandle
        public ResultCode GetNpadIrCameraHandle(ServiceCtx context)
        {
            NpadIdType npadIdType = (NpadIdType)context.RequestData.ReadUInt32();

            if (npadIdType > NpadIdType.Player8 &&
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

        [CommandCmif(318)] // 4.0.0+
        // StopImageProcessorAsync(nn::irsensor::IrCameraHandle, nn::applet::AppletResourceUserId, pid)
        public ResultCode StopImageProcessorAsync(ServiceCtx context)
        {
            int irCameraHandle = context.RequestData.ReadInt32();
            long appletResourceUserId = context.RequestData.ReadInt64();

            Logger.Stub?.PrintStub(LogClass.ServiceIrs, new { appletResourceUserId, irCameraHandle });

            return ResultCode.Success;
        }

        [CommandCmif(319)] // 4.0.0+
        // ActivateIrsensorWithFunctionLevel(nn::applet::AppletResourceUserId, nn::irsensor::PackedFunctionLevel, pid)
        public ResultCode ActivateIrsensorWithFunctionLevel(ServiceCtx context)
        {
            long appletResourceUserId = context.RequestData.ReadInt64();
            long packedFunctionLevel = context.RequestData.ReadInt64();

            Logger.Stub?.PrintStub(LogClass.ServiceIrs, new { appletResourceUserId, packedFunctionLevel });

            return ResultCode.Success;
        }

        private ResultCode CheckCameraHandle(IrCameraHandle irCameraHandle)
        {
            if (irCameraHandle.DeviceType == 1 || (PlayerIndex)irCameraHandle.PlayerNumber >= PlayerIndex.Unknown)
            {
                return ResultCode.InvalidCameraHandle;
            }

            return ResultCode.Success;
        }
    }
}
