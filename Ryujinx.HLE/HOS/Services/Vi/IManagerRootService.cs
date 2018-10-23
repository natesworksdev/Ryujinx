using Ryujinx.HLE.HOS.Ipc;
using System.Collections.Generic;

namespace Ryujinx.HLE.HOS.Services.Vi
{
    class ManagerRootService : IpcService
    {
        private Dictionary<int, ServiceProcessRequest> _mCommands;

        public override IReadOnlyDictionary<int, ServiceProcessRequest> Commands => _mCommands;

        public ManagerRootService()
        {
            _mCommands = new Dictionary<int, ServiceProcessRequest>()
            {
                { 2, GetDisplayService }
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