using Ryujinx.Common.Logging;
using Ryujinx.HLE.HOS.Ipc;
using System.Collections.Generic;

namespace Ryujinx.HLE.HOS.Services.Ns
{
    class IApplicationManagerInterface : IpcService
    {
        private Dictionary<int, ServiceProcessRequest> _commands;

        public override IReadOnlyDictionary<int, ServiceProcessRequest> Commands => _commands;

        private bool _isInitialized;

        public IApplicationManagerInterface()
        {
            _commands = new Dictionary<int, ServiceProcessRequest>
            {
                { 400, GetApplicationControlData }
            };
        }

        // TODO: implement this at some point
        public long GetApplicationControlData(ServiceCtx context)
        {
            Logger.PrintStub(LogClass.ServiceNs, "Stubbed.");

            return 0xDEAD;
        }
    }
}
