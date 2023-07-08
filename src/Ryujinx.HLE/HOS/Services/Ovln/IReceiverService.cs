using Ryujinx.Common.Logging;
using Ryujinx.HLE.HOS.Services.Ovln.Receiver;

namespace Ryujinx.HLE.HOS.Services.Ovln
{
    [Service("ovln:rcv")]
    class IReceiverService : IpcService
    {
        public IReceiverService(ServiceCtx context) { }

        [CommandCmif(0)]
        // OpenReceiver() -> object<nn::ovln::IReceiver>
        public ResultCode OpenSender(ServiceCtx context)
        {
            MakeObject(context, new IReceiver(context));

            Logger.Stub?.PrintStub(LogClass.ServiceOvln);

            return ResultCode.Success;
        }
    }
}