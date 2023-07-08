using Ryujinx.Common.Logging;
using Ryujinx.HLE.HOS.Services.Ovln.Sender;

namespace Ryujinx.HLE.HOS.Services.Ovln
{
    [Service("ovln:snd")]
    class ISenderService : IpcService
    {
        public ISenderService(ServiceCtx context) { }

        [CommandCmif(0)]
        // OpenSender(unknown<0x18>) -> object<nn::ovln::ISender>
        public ResultCode OpenSender(ServiceCtx context)
        {
            MakeObject(context, new ISender(context));

            Logger.Stub?.PrintStub(LogClass.ServiceOvln);

            return ResultCode.Success;
        }
    }
}