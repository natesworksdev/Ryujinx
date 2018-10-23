using Ryujinx.Common.Logging;
using Ryujinx.HLE.HOS.Ipc;
using System.Collections.Generic;

namespace Ryujinx.HLE.HOS.Services.Ns
{
    class AddOnContentManager : IpcService
    {
        private Dictionary<int, ServiceProcessRequest> _mCommands;

        public override IReadOnlyDictionary<int, ServiceProcessRequest> Commands => _mCommands;

        public AddOnContentManager()
        {
            _mCommands = new Dictionary<int, ServiceProcessRequest>()
            {
                { 2, CountAddOnContent },
                { 3, ListAddOnContent  }
            };
        }

        public static long CountAddOnContent(ServiceCtx context)
        {
            context.ResponseData.Write(0);

            Logger.PrintStub(LogClass.ServiceNs, "Stubbed.");

            return 0;
        }

        public static long ListAddOnContent(ServiceCtx context)
        {
            Logger.PrintStub(LogClass.ServiceNs, "Stubbed.");

            //TODO: This is supposed to write a u32 array aswell.
            //It's unknown what it contains.
            context.ResponseData.Write(0);

            return 0;
        }
    }
}