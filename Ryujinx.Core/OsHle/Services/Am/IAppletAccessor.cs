using Ryujinx.Core.Logging;
using Ryujinx.Core.OsHle.Ipc;
using System.Collections.Generic;

namespace Ryujinx.Core.OsHle.Services.Am
{
    class IAppletAccessor : IpcService
    {
        private Dictionary<int, ServiceProcessRequest> m_Commands;

        public override IReadOnlyDictionary<int, ServiceProcessRequest> Commands => m_Commands;

        public IAppletAccessor()
        {
            m_Commands = new Dictionary<int, ServiceProcessRequest>()
            {
                { 0,  GetAppletStateChangedEvent },
                { 1,  IsCompleted                },
                { 10, Start                      },
                { 20, RequestExit                },
                { 25, Terminate                  },
                { 30, GetResult                  }
            };
        }

        public long GetAppletStateChangedEvent(ServiceCtx Context)
        {
            Context.Ns.Log.PrintStub(LogClass.ServiceAm, "Stubbed.");

            return 0;
        }

        public long IsCompleted(ServiceCtx Context)
        {  
            Context.Ns.Log.PrintStub(LogClass.ServiceAm, "Stubbed.");

            return 0;
        }

        public long Start(ServiceCtx Context)
        {  
            Context.Ns.Log.PrintStub(LogClass.ServiceAm, "Stubbed.");

            return 0;
        }

        public long RequestExit(ServiceCtx Context)
        {  
            Context.Ns.Log.PrintStub(LogClass.ServiceAm, "Stubbed.");

            return 0;
        }

        public long Terminate(ServiceCtx Context)
        {  
            Context.Ns.Log.PrintStub(LogClass.ServiceAm, "Stubbed.");

            return 0;
        }

        public long GetResult(ServiceCtx Context)
        {  
            Context.Ns.Log.PrintStub(LogClass.ServiceAm, "Stubbed.");

            return 0;
        }
    }
}
