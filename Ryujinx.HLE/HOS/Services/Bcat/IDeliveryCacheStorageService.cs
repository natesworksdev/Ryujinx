using Ryujinx.HLE.HOS.Ipc;
using System.Collections.Generic;

namespace Ryujinx.HLE.HOS.Services.Bcat
{
    internal class IDeliveryCacheStorageService : IpcService
    {
        private Dictionary<int, ServiceProcessRequest> _commands;

        public override IReadOnlyDictionary<int, ServiceProcessRequest> Commands => _commands;

        public IDeliveryCacheStorageService()
        {
            _commands = new Dictionary<int, ServiceProcessRequest>
            {
                //...
            };
        }

    }
}
