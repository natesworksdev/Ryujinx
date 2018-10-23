using Ryujinx.Common.Logging;
using Ryujinx.HLE.HOS.Ipc;
using System.Collections.Generic;

namespace Ryujinx.HLE.HOS.Services.Pctl
{
    class ParentalControlService : IpcService
    {
        private Dictionary<int, ServiceProcessRequest> _mCommands;

        public override IReadOnlyDictionary<int, ServiceProcessRequest> Commands => _mCommands;

        private bool _initialized = false;

        private bool _needInitialize;

        public ParentalControlService(bool needInitialize = true)
        {
            _mCommands = new Dictionary<int, ServiceProcessRequest>()
            {
                { 1, Initialize }
            };

            _needInitialize = needInitialize;
        }

        public long Initialize(ServiceCtx context)
        {
            if (_needInitialize && !_initialized)
            {
                _initialized = true;
            }
            else
            {
                Logger.PrintWarning(LogClass.ServicePctl, "Service is already initialized!");
            }

            return 0;
        }
    }
}