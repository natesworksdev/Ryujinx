using Ryujinx.HLE.HOS.Ipc;
using System.Collections.Generic;

namespace Ryujinx.HLE.HOS.Services.Ssl
{
    class SslContext : IpcService
    {
        private Dictionary<int, ServiceProcessRequest> _mCommands;

        public override IReadOnlyDictionary<int, ServiceProcessRequest> Commands => _mCommands;

        public SslContext()
        {
            _mCommands = new Dictionary<int, ServiceProcessRequest>()
            {
                //...
            };
        }
    }
}