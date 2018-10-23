using Ryujinx.HLE.HOS.Ipc;
using System.Collections.Generic;

namespace Ryujinx.HLE.HOS.Services.Apm
{
    class Manager : IpcService
    {
        private Dictionary<int, ServiceProcessRequest> _mCommands;

        public override IReadOnlyDictionary<int, ServiceProcessRequest> Commands => _mCommands;

        public Manager()
        {
            _mCommands = new Dictionary<int, ServiceProcessRequest>()
            {
                { 0, OpenSession }
            };
        }

        public long OpenSession(ServiceCtx context)
        {
            MakeObject(context, new Session());

            return 0;
        }
    }
}