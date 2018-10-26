using Ryujinx.Common.Logging;
using Ryujinx.HLE.HOS.Ipc;
using Ryujinx.HLE.HOS.Kernel;
using System;
using System.Collections.Generic;

namespace Ryujinx.HLE.HOS.Services.Am
{
    internal class IHomeMenuFunctions : IpcService
    {
        private Dictionary<int, ServiceProcessRequest> _mCommands;

        public override IReadOnlyDictionary<int, ServiceProcessRequest> Commands => _mCommands;

        private KEvent _channelEvent;

        public IHomeMenuFunctions(Horizon system)
        {
            _mCommands = new Dictionary<int, ServiceProcessRequest>()
            {
                { 10, RequestToGetForeground        },
                { 21, GetPopFromGeneralChannelEvent }
            };

            //ToDo: Signal this Event somewhere in future.
            _channelEvent = new KEvent(system);
        }

        public long RequestToGetForeground(ServiceCtx context)
        {
            Logger.PrintStub(LogClass.ServiceAm, "Stubbed.");

            return 0;
        }

        public long GetPopFromGeneralChannelEvent(ServiceCtx context)
        {
            if (context.Process.HandleTable.GenerateHandle(_channelEvent.ReadableEvent, out int handle) != KernelResult.Success)
            {
                throw new InvalidOperationException("Out of handles!");
            }

            context.Response.HandleDesc = IpcHandleDesc.MakeCopy(handle);

            Logger.PrintStub(LogClass.ServiceAm, "Stubbed.");

            return 0;
        }
    }
}
