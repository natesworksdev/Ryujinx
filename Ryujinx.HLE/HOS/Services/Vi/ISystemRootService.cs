using Ryujinx.HLE.HOS.Ipc;
using System.Collections.Generic;

namespace Ryujinx.HLE.HOS.Services.Vi
{
    class SystemRootService : IpcService
    {
        private Dictionary<int, ServiceProcessRequest> _mCommands;

        public override IReadOnlyDictionary<int, ServiceProcessRequest> Commands => _mCommands;

        public SystemRootService()
        {
            _mCommands = new Dictionary<int, ServiceProcessRequest>()
            {
                { 1, GetDisplayService }
            };
        }

        public long GetDisplayService(ServiceCtx context)
        {
            int serviceType = context.RequestData.ReadInt32();

            MakeObject(context, new ApplicationDisplayService());

            return 0;
        }
    }
}