using Ryujinx.Common.Logging;
using Ryujinx.HLE.HOS.Ipc;
using System.Collections.Generic;

namespace Ryujinx.HLE.HOS.Services.Prepo
{
    class PrepoService : IpcService
    {
        private Dictionary<int, ServiceProcessRequest> _mCommands;

        public override IReadOnlyDictionary<int, ServiceProcessRequest> Commands => _mCommands;

        public PrepoService()
        {
            _mCommands = new Dictionary<int, ServiceProcessRequest>()
            {
                { 10101, SaveReportWithUser }
            };
        }

        public static long SaveReportWithUser(ServiceCtx context)
        {
            Logger.PrintStub(LogClass.ServicePrepo, "Stubbed.");

            return 0;
        }
    }
}