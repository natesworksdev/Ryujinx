using Ryujinx.HLE.HOS.Ipc;
using System.Collections.Generic;

namespace Ryujinx.HLE.HOS.Services.Am
{
    internal class IApplicationProxyService : IpcService
    {
        private Dictionary<int, ServiceProcessRequest> _mCommands;

        public override IReadOnlyDictionary<int, ServiceProcessRequest> Commands => _mCommands;

        public IApplicationProxyService()
        {
            _mCommands = new Dictionary<int, ServiceProcessRequest>()
            {
                { 0, OpenApplicationProxy }
            };
        }

        public long OpenApplicationProxy(ServiceCtx context)
        {
            MakeObject(context, new IApplicationProxy());

            return 0;
        }
    }
}