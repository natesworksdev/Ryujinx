using Ryujinx.Common.Logging;
using Ryujinx.HLE.HOS.Ipc;
using Ryujinx.HLE.HOS.Kernel.Common;
using Ryujinx.HLE.HOS.Kernel.Threading;
using System;
using System.Collections.Generic;

namespace Ryujinx.HLE.HOS.Services.Friend
{
    class INotificationService : IpcService
    {
        private Dictionary<int, ServiceProcessRequest> _commands;

        private KEvent _newNotificationEvent;

        public override IReadOnlyDictionary<int, ServiceProcessRequest> Commands => _commands;

        public INotificationService(Horizon system)
        {
            _commands = new Dictionary<int, ServiceProcessRequest>()
            {
                { 0, GetSystemEvent },
                { 1, Clear          },
                { 2, Pop            }
            };

            _newNotificationEvent = new KEvent(system);
        }

        // GetSystemEvent() -> KObject
        private long GetSystemEvent(ServiceCtx context)
        {
            Logger.PrintStub(LogClass.ServiceFriend);

            if (context.Process.HandleTable.GenerateHandle(_newNotificationEvent.ReadableEvent, out int handle) != KernelResult.Success)
            {
                throw new InvalidOperationException("Out of handles!");
            }

            context.Response.HandleDesc = IpcHandleDesc.MakeCopy(handle);

            return 0;
        }

        // Clear()
        private long Clear(ServiceCtx context)
        {
            Logger.PrintStub(LogClass.ServiceFriend);

            return 0;
        }

        // Pop() -> nn::friends::detail::ipc::SizedNotificationInfo (u128)
        private long Pop(ServiceCtx context)
        {
            Logger.PrintStub(LogClass.ServiceFriend);

            context.ResponseData.Write(0L);
            context.ResponseData.Write(0L);

            return 0;
        }
    }
}
