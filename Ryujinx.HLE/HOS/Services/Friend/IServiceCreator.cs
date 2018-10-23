using Ryujinx.HLE.HOS.Ipc;
using System.Collections.Generic;

namespace Ryujinx.HLE.HOS.Services.Friend
{
    class ServiceCreator : IpcService
    {
        private Dictionary<int, ServiceProcessRequest> _mCommands;

        public override IReadOnlyDictionary<int, ServiceProcessRequest> Commands => _mCommands;

        public ServiceCreator()
        {
            _mCommands = new Dictionary<int, ServiceProcessRequest>()
            {
                { 0, CreateFriendService }
            };
        }

        public static long CreateFriendService(ServiceCtx context)
        {
            MakeObject(context, new FriendService());

            return 0;
        }
    }
}