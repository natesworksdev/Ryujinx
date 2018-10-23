using Ryujinx.HLE.HOS.Ipc;
using System.Collections.Generic;

namespace Ryujinx.HLE.HOS.Services.Am
{
    class DisplayController : IpcService
    {
        private Dictionary<int, ServiceProcessRequest> _mCommands;

        public override IReadOnlyDictionary<int, ServiceProcessRequest> Commands => _mCommands;

        public DisplayController()
        {
            _mCommands = new Dictionary<int, ServiceProcessRequest>()
            {
                //...
            };
        }
    }
}