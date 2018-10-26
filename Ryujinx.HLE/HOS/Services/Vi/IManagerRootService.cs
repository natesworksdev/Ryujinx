using Ryujinx.HLE.HOS.Ipc;
using System.Collections.Generic;

namespace Ryujinx.HLE.HOS.Services.Vi
{
    internal class IManagerRootService : IpcService
    {
        private Dictionary<int, ServiceProcessRequest> _mCommands;

        public override IReadOnlyDictionary<int, ServiceProcessRequest> Commands => _mCommands;

        public IManagerRootService()
        {
            _mCommands = new Dictionary<int, ServiceProcessRequest>()
            {
                { 2, GetDisplayService }
            };
        }

        public long GetDisplayService(ServiceCtx context)
        {
            int serviceType = context.RequestData.ReadInt32();

            MakeObject(context, new IApplicationDisplayService());

            return 0;
        }
    }
}