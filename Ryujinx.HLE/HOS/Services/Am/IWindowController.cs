using Ryujinx.Common.Logging;
using Ryujinx.HLE.HOS.Ipc;
using System.Collections.Generic;

namespace Ryujinx.HLE.HOS.Services.Am
{
    internal class IWindowController : IpcService
    {
        private Dictionary<int, ServiceProcessRequest> _mCommands;

        public override IReadOnlyDictionary<int, ServiceProcessRequest> Commands => _mCommands;

        public IWindowController()
        {
            _mCommands = new Dictionary<int, ServiceProcessRequest>()
            {
                { 1,  GetAppletResourceUserId },
                { 10, AcquireForegroundRights }
            };
        }

        public long GetAppletResourceUserId(ServiceCtx context)
        {
            Logger.PrintStub(LogClass.ServiceAm, "Stubbed.");

            context.ResponseData.Write(0L);

            return 0;
        }

        public long AcquireForegroundRights(ServiceCtx context)
        {
            Logger.PrintStub(LogClass.ServiceAm, "Stubbed.");

            return 0;
        }
    }
}