using Ryujinx.HLE.Logging;
using Ryujinx.HLE.OsHle.Ipc;
using System.Collections.Generic;

namespace Ryujinx.HLE.OsHle.Services.Acc
{
    class IProfile : IpcService
    {
        private Dictionary<int, ServiceProcessRequest> m_Commands;

        public override IReadOnlyDictionary<int, ServiceProcessRequest> Commands => m_Commands;

        private byte[] profile_image = { };

        public IProfile()
        {
            m_Commands = new Dictionary<int, ServiceProcessRequest>()
            {
                { 1, GetBase },
                { 10, GetImageSize },
                { 11, LoadImage }
            };
        }

        public long GetBase(ServiceCtx Context)
        {
            Context.Ns.Log.PrintStub(LogClass.ServiceAcc, "Stubbed.");

            Context.ResponseData.Write(0L);
            Context.ResponseData.Write(0L);
            Context.ResponseData.Write(0L);
            Context.ResponseData.Write(0L);
            Context.ResponseData.Write(0L);
            Context.ResponseData.Write(0L);
            Context.ResponseData.Write(0L);

            return 0;
        }

        public long GetImageSize(ServiceCtx Context)
        {
            Context.Ns.Log.PrintStub(LogClass.ServiceAcc, "Stubbed.");

            Context.ResponseData.Write(profile_image.Length);

            return 0;
        }

        public long LoadImage(ServiceCtx Context)
        {
            Context.Ns.Log.PrintStub(LogClass.ServiceAcc, "Stubbed.");

            (long Position, long Size) = Context.Request.GetBufferType0x22();

            Context.ResponseData.Write(profile_image.Length);
            Context.Memory.WriteBytes(Position, profile_image);

            return 0;
        }
    }
}