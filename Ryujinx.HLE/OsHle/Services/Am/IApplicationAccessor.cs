using Ryujinx.HLE.Logging;
using Ryujinx.HLE.OsHle.Ipc;
using System.Collections.Generic;

namespace Ryujinx.HLE.OsHle.Services.Am
{
    class IApplicationAccessor : IpcService
    {
        private Dictionary<int, ServiceProcessRequest> m_Commands;

        public override IReadOnlyDictionary<int, ServiceProcessRequest> Commands => m_Commands;

        public IApplicationAccessor()
        {
            m_Commands = new Dictionary<int, ServiceProcessRequest>()
            {
                { 112, GetCurrentLibraryApplet },
                { 121, PushLaunchParameter     }
            };
        }
        
        public long GetCurrentLibraryApplet(ServiceCtx Context)
        {
            MakeObject(Context, new IAppletAccessor());

            return 0;
        }
        
        public long PushLaunchParameter(ServiceCtx Context)
        {  
            Context.Ns.Log.PrintStub(LogClass.ServiceAm, "Stubbed.");

            return 0;
        }
    }
}
