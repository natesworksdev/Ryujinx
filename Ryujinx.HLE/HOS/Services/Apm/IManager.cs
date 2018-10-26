using Ryujinx.HLE.HOS.Ipc;
using System.Collections.Generic;

namespace Ryujinx.HLE.HOS.Services.Apm
{
    internal class IManager : IpcService
    {
        private Dictionary<int, ServiceProcessRequest> _mCommands;

        public override IReadOnlyDictionary<int, ServiceProcessRequest> Commands => _mCommands;

        public IManager()
        {
            _mCommands = new Dictionary<int, ServiceProcessRequest>()
            {
                { 0, OpenSession }
            };
        }

        public long OpenSession(ServiceCtx context)
        {
            MakeObject(context, new ISession());

            return 0;
        }
    }
}