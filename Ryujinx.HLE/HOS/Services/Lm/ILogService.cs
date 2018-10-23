using Ryujinx.HLE.HOS.Ipc;
using System.Collections.Generic;

namespace Ryujinx.HLE.HOS.Services.Lm
{
    class LogService : IpcService
    {
        private Dictionary<int, ServiceProcessRequest> _mCommands;

        public override IReadOnlyDictionary<int, ServiceProcessRequest> Commands => _mCommands;

        public LogService()
        {
            _mCommands = new Dictionary<int, ServiceProcessRequest>()
            {
                { 0, Initialize }
            };
        }

        public long Initialize(ServiceCtx context)
        {
            MakeObject(context, new Logger());

            return 0;
        }
    }
}