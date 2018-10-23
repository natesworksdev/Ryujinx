using Ryujinx.HLE.HOS.Ipc;
using System.Collections.Generic;

namespace Ryujinx.HLE.HOS.Services.Ns
{
    class ServiceGetterInterface : IpcService
    {
        private Dictionary<int, ServiceProcessRequest> _mCommands;

        public override IReadOnlyDictionary<int, ServiceProcessRequest> Commands => _mCommands;

        public ServiceGetterInterface()
        {
            _mCommands = new Dictionary<int, ServiceProcessRequest>()
            {
                //...
            };
        }
    }
}