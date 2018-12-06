using Ryujinx.Common.Logging;
using Ryujinx.HLE.HOS.Ipc;
using System.Collections.Generic;

namespace Ryujinx.HLE.HOS.Services.Bcat
{
    class IDeliveryCacheStorageService : IpcService
    {
        private Dictionary<int, ServiceProcessRequest> _commands;

        public override IReadOnlyDictionary<int, ServiceProcessRequest> Commands => _commands;

        public IDeliveryCacheStorageService()
        {
            _commands = new Dictionary<int, ServiceProcessRequest>
            {
                { 10, EnumerateDeliveryCacheDirectory }
            };
        }

        private long EnumerateDeliveryCacheDirectory(ServiceCtx context)
        {
            Logger.PrintStub(LogClass.ServiceBcat);

            // We have no cache directories, so return 0.
            context.ResponseData.Write(0);

            return 0;
        }
    }
}
