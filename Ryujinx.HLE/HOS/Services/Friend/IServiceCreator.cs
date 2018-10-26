using Ryujinx.HLE.HOS.Ipc;
using System.Collections.Generic;

namespace Ryujinx.HLE.HOS.Services.Friend
{
    internal class IServiceCreator : IpcService
    {
        private Dictionary<int, ServiceProcessRequest> _mCommands;

        public override IReadOnlyDictionary<int, ServiceProcessRequest> Commands => _mCommands;

        public IServiceCreator()
        {
            _mCommands = new Dictionary<int, ServiceProcessRequest>()
            {
                { 0, CreateFriendService }
            };
        }

        public static long CreateFriendService(ServiceCtx context)
        {
            MakeObject(context, new IFriendService());

            return 0;
        }
    }
}