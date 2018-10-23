using Ryujinx.HLE.HOS.Ipc;
using System.Collections.Generic;

namespace Ryujinx.HLE.HOS.Services.Am
{
    class AllSystemAppletProxiesService : IpcService
    {
        private Dictionary<int, ServiceProcessRequest> _mCommands;

        public override IReadOnlyDictionary<int, ServiceProcessRequest> Commands => _mCommands;

        public AllSystemAppletProxiesService()
        {
            _mCommands = new Dictionary<int, ServiceProcessRequest>()
            {
                { 100, OpenSystemAppletProxy }
            };
        }

        public long OpenSystemAppletProxy(ServiceCtx context)
        {
            MakeObject(context, new SystemAppletProxy());

            return 0;
        }
    }
}