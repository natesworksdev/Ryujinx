using Ryujinx.Common.Logging;
using System;

namespace Ryujinx.HLE.HOS.Services.Ssl.SslService
{
    class ISslContext : IpcService
    {
        public ISslContext(ServiceCtx context ) { }
        
        [Command(4)]
        public ResultCode ImportServerPki(ServiceCtx context)
        {
            int certificateFormat = context.RequestData.ReadInt32();
            long certificateDataPosition = context.Request.SendBuff[0].Position;
            long certificateDataSize     = context.Request.SendBuff[0].Size;
            UInt64 response = 1;

            context.ResponseData.Write(response);

            Logger.PrintStub(LogClass.ServiceSsl, new { CertificateFormat, unknown });

            return ResultCode.Success;
        }

    }
}
