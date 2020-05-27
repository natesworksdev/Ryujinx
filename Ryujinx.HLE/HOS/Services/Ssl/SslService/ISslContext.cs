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
            var orOther = context.RequestData.ReadBytes(5);
            UInt64 nice = 1;

            context.ResponseData.Write(nice);

            Logger.PrintStub(LogClass.ServiceSsl, new { CertificateFormat, orOther });

            return ResultCode.Success;
        }

    }
}