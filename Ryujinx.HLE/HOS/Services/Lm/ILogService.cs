using Ryujinx.HLE.HOS.Ipc;
using System.Collections.Generic;

namespace Ryujinx.HLE.HOS.Services.Lm
{
    internal class ILogService : IpcService
    {
        private Dictionary<int, ServiceProcessRequest> _mCommands;

        public override IReadOnlyDictionary<int, ServiceProcessRequest> Commands => _mCommands;

        public ILogService()
        {
            _mCommands = new Dictionary<int, ServiceProcessRequest>()
            {
                { 0, Initialize }
            };
        }

        public long Initialize(ServiceCtx context)
        {
            MakeObject(context, new ILogger());

            return 0;
        }
    }
}