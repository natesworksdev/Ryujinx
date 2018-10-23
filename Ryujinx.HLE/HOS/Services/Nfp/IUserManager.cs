using Ryujinx.HLE.HOS.Ipc;
using System.Collections.Generic;

namespace Ryujinx.HLE.HOS.Services.Nfp
{
    class UserManager : IpcService
    {
        private Dictionary<int, ServiceProcessRequest> _mCommands;

        public override IReadOnlyDictionary<int, ServiceProcessRequest> Commands => _mCommands;

        public UserManager()
        {
            _mCommands = new Dictionary<int, ServiceProcessRequest>()
            {
                { 0, GetUserInterface }
            };
        }

        public long GetUserInterface(ServiceCtx context)
        {
            MakeObject(context, new User(context.Device.System));

            return 0;
        }
    }
}