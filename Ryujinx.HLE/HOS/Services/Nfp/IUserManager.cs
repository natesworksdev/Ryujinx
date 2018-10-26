using Ryujinx.HLE.HOS.Ipc;
using System.Collections.Generic;

namespace Ryujinx.HLE.HOS.Services.Nfp
{
    internal class IUserManager : IpcService
    {
        private Dictionary<int, ServiceProcessRequest> _mCommands;

        public override IReadOnlyDictionary<int, ServiceProcessRequest> Commands => _mCommands;

        public IUserManager()
        {
            _mCommands = new Dictionary<int, ServiceProcessRequest>()
            {
                { 0, GetUserInterface }
            };
        }

        public long GetUserInterface(ServiceCtx context)
        {
            MakeObject(context, new IUser(context.Device.System));

            return 0;
        }
    }
}