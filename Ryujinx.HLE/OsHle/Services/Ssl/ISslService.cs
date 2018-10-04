using Ryujinx.HLE.Logging;
using Ryujinx.HLE.OsHle.Ipc;
using System.Collections.Generic;

namespace Ryujinx.HLE.OsHle.Services.Ssl
{
    class ISslService : IpcService
    {
        private Dictionary<int, ServiceProcessRequest> m_Commands;

        public override IReadOnlyDictionary<int, ServiceProcessRequest> Commands => m_Commands;

        public ISslService()
        {
            m_Commands = new Dictionary<int, ServiceProcessRequest>()
            {
                { 0, CreateContext       },
                { 5, SetInterfaceVersion }
            };
        }
		
        // CreateContext(nn::ssl::sf::SslVersion, u64, pid) -> object<nn::ssl::sf::ISslContext>
        public long CreateContext(ServiceCtx Context)
        {
            int  Version = Context.RequestData.ReadInt32();
            long Unknown = Context.RequestData.ReadInt64();

            Context.Device.Log.PrintStub(LogClass.ServiceSsl, $"Stubbed. Version: {Version} - Unknown: {Unknown}");

            MakeObject(Context, new ISslContext());

            return 0;
        }

        // SetInterfaceVersion(u32)
        public long SetInterfaceVersion(ServiceCtx Context)
        {
            int Version = Context.RequestData.ReadInt32();

            Context.Ns.Log.PrintStub(LogClass.ServiceSsl, $"Stubbed. Version: {Version}");

            return 0;
        }
    }
}
