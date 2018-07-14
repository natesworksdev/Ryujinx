using Ryujinx.HLE.Logging;
using Ryujinx.HLE.OsHle.Ipc;
using System.Collections.Generic;

namespace Ryujinx.HLE.OsHle.Services.Nifm
{
    class IGeneralService : IpcService
    {
        private Dictionary<int, ServiceProcessRequest> m_Commands;

        public override IReadOnlyDictionary<int, ServiceProcessRequest> Commands => m_Commands;

        public IGeneralService()
        {
            m_Commands = new Dictionary<int, ServiceProcessRequest>()
            {
                { 4,  CreateRequest            },
                { 5,  GetCurrentNetworkProfile },
                { 12, GetCurrentIpAddress      }
            };
        }

        //CreateRequest(i32)
        public long CreateRequest(ServiceCtx Context)
        {
            int Unknown = Context.RequestData.ReadInt32();

            MakeObject(Context, new IRequest());

            Context.Ns.Log.PrintStub(LogClass.ServiceNifm, "Stubbed.");

            return 0;
        }

        public long GetCurrentNetworkProfile(ServiceCtx Context)
        {
            (long Position, long Size) = Context.Request.GetBufferType0x21();

            Context.Ns.Log.PrintStub(LogClass.ServiceNifm, "Stubbed.");

            for (int Index = 0; Index < Size; Index++)
            {
                Context.Memory.WriteByte(Position + Index, 0);
            }

            return 0;
        }

        public long GetCurrentIpAddress(ServiceCtx Context)
        {
            Context.Ns.Log.PrintStub(LogClass.ServiceNifm, "Stubbed.");

            Context.ResponseData.Write(0);

            return 0;
        }
    }
}