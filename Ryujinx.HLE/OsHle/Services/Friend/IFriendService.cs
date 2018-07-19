using Ryujinx.HLE.Logging;
using Ryujinx.HLE.OsHle.Ipc;
using System.Collections.Generic;

namespace Ryujinx.HLE.OsHle.Services.Friend
{
    class IFriendService : IpcService
    {
        private Dictionary<int, ServiceProcessRequest> m_Commands;

        public override IReadOnlyDictionary<int, ServiceProcessRequest> Commands => m_Commands;

        public IFriendService()
        {
            m_Commands = new Dictionary<int, ServiceProcessRequest>()
            {
                { 10601, DeclareCloseOnlinePlaySession },
                { 10610, UpdateUserPresence            }
            };
        }

        public long DeclareCloseOnlinePlaySession(ServiceCtx Context)
        {
            Context.Ns.Log.PrintStub(LogClass.ServiceFriend, "Stubbed.");

            return 0;
        }

        public long UpdateUserPresence(ServiceCtx Context)
        {
            Context.Ns.Log.PrintStub(LogClass.ServiceFriend, "Stubbed.");

            return 0;
        }
    }
}