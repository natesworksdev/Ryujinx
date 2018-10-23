using Ryujinx.HLE.HOS.Ipc;
using System.Collections.Generic;

namespace Ryujinx.HLE.HOS.Services.Am
{
    class DebugFunctions : IpcService
    {
        private Dictionary<int, ServiceProcessRequest> _mCommands;

        public override IReadOnlyDictionary<int, ServiceProcessRequest> Commands => _mCommands;

        public DebugFunctions()
        {
            _mCommands = new Dictionary<int, ServiceProcessRequest>()
            {
                //...
            };
        }
    }
}