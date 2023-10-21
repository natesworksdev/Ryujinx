using System;

namespace Ryujinx.HLE.HOS.Services.Ns
{
    [Service("ns:cm")]
    class IContentManagementInterface : IpcService
    {
        public IContentManagementInterface(ServiceCtx context) { }

        [CommandCmif(47)]
        public ResultCode GetTotalSpaceSize(ServiceCtx context)
        {
            byte storageId = context.RequestData.ReadByte();

            context.ResponseData.Write(Int64.MaxValue);

            return ResultCode.Success;
        }

        [CommandCmif(48)]
        public ResultCode GetFreeSpaceSize(ServiceCtx context)
        {
            byte storageId = context.RequestData.ReadByte();

            context.ResponseData.Write(Int64.MaxValue);

            return ResultCode.Success;
        }
    }
}
