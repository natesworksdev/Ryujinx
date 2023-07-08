using Ryujinx.Common.Logging;
using Ryujinx.Horizon.Common;

namespace Ryujinx.HLE.HOS.Services.Vi.RootService.ApplicationDisplayService
{
    class ISystemDisplayService : IpcService
    {
        private IApplicationDisplayService _applicationDisplayService;

        public ISystemDisplayService(IApplicationDisplayService applicationDisplayService)
        {
            _applicationDisplayService = applicationDisplayService;
        }

        [CommandCmif(2205)]
        // SetLayerZ(u64, u64)
        public ResultCode SetLayerZ(ServiceCtx context)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceVi);

            return ResultCode.Success;
        }

        [CommandCmif(2207)]
        // SetLayerVisibility(b8, u64)
        public ResultCode SetLayerVisibility(ServiceCtx context)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceVi);

            return ResultCode.Success;
        }

        [CommandCmif(2312)] // 1.0.0-6.2.0
        // CreateStrayLayer(u32, u64) -> (u64, u64, buffer<bytes, 6>)
        public ResultCode CreateStrayLayer(ServiceCtx context)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceVi);

            return _applicationDisplayService.CreateStrayLayer(context);
        }

        [CommandCmif(3000)]
        // ListDisplayModes(u64) -> (u64, buffer<nn::vi::DisplayModeInfo, 6>)
        public ResultCode ListDisplayModes(ServiceCtx context)
        {
            ulong displayId = context.RequestData.ReadUInt64();
            ulong bufferAddress = context.Request.ReceiveBuff[0].Position;

            (ulong width, ulong height) = AndroidSurfaceComposerClient.GetDisplayInfo(context, displayId);

            context.Memory.Write(bufferAddress, (uint)width);
            context.Memory.Write(bufferAddress + 4, (uint)height);
            context.Memory.Write(bufferAddress + 8, 60.0f);
            context.Memory.Write(bufferAddress + 12, 0);

            context.ResponseData.Write(1);

            Logger.Stub?.PrintStub(LogClass.ServiceVi);

            return ResultCode.Success;
        }

        [CommandCmif(3200)]
        // GetDisplayMode(u64) -> nn::vi::DisplayModeInfo
        public ResultCode GetDisplayMode(ServiceCtx context)
        {
            ulong displayId = context.RequestData.ReadUInt64();

            (ulong width, ulong height) = AndroidSurfaceComposerClient.GetDisplayInfo(context, displayId);

            context.ResponseData.Write((uint)width);
            context.ResponseData.Write((uint)height);
            context.ResponseData.Write(60.0f);
            context.ResponseData.Write(0);

            Logger.Stub?.PrintStub(LogClass.ServiceVi);

            return ResultCode.Success;
        }
    }
}