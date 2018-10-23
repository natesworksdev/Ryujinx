using Ryujinx.HLE.HOS.Ipc;
using System.Collections.Generic;

namespace Ryujinx.HLE.HOS.Services.Am
{
    class ApplicationProxyService : IpcService
    {
        private Dictionary<int, ServiceProcessRequest> _mCommands;

        public override IReadOnlyDictionary<int, ServiceProcessRequest> Commands => _mCommands;

        public ApplicationProxyService()
        {
            _mCommands = new Dictionary<int, ServiceProcessRequest>()
            {
                { 0, OpenApplicationProxy }
            };
        }

        public long OpenApplicationProxy(ServiceCtx context)
        {
            MakeObject(context, new ApplicationProxy());

            return 0;
        }
    }
}