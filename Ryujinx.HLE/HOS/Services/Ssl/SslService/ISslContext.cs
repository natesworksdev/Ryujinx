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
            var CertificateFormat = context.RequestData.ReadInt32();
            var unknown = context.RequestData.ReadBytes(5);
            UInt64 response = 1;

            context.ResponseData.Write(response);

            Logger.PrintStub(LogClass.ServiceSsl, new { CertificateFormat, unknown });

            return ResultCode.Success;
        }

    }
}