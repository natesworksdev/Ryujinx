using Ryujinx.HLE.Logging;
using Ryujinx.HLE.OsHle.Ipc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;

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
                { 4, CreateRequest        },
                { 12, GetCurrentIpAddress }
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

        public long GetCurrentIpAddress(ServiceCtx Context)
        {
            string HostName = Dns.GetHostName();
            IPHostEntry HostEntry = Dns.GetHostEntry(HostName);
            IPAddress[] Address = HostEntry.AddressList;
            IPAddress IP = Address.Where(x => x.AddressFamily == AddressFamily.InterNetwork).FirstOrDefault();
            uint LocalIP = BitConverter.ToUInt32(IP.GetAddressBytes());
            Context.ResponseData.Write(LocalIP);
            return 0;
        }
    }
}
