using Ryujinx.Common.Logging;
using Ryujinx.HLE.HOS.Ipc;
using Ryujinx.HLE.Utilities;
using System.Collections.Generic;

namespace Ryujinx.HLE.HOS.Services.Friend
{
    class IServiceCreator : IpcService
    {
        private Dictionary<int, ServiceProcessRequest> _commands;

        private Horizon _system;

        public override IReadOnlyDictionary<int, ServiceProcessRequest> Commands => _commands;

        public IServiceCreator(Horizon system)
        {
            _commands = new Dictionary<int, ServiceProcessRequest>
            {
                { 0, CreateFriendService       },
                { 1, CreateNotificationService }
            };

            this._system = system;
        }

        public static long CreateFriendService(ServiceCtx context)
        {
            MakeObject(context, new IFriendService());

            return 0;
        }

        private long CreateNotificationService(ServiceCtx context)
        {
            UInt128 uuid = new UInt128(
                context.RequestData.ReadInt64(),
                context.RequestData.ReadInt64());

            Logger.PrintStub(LogClass.ServiceFriend, new { uuid });

            MakeObject(context, new INotificationService(_system));

            return 0;
        }
    }
}