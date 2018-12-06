using Ryujinx.Common.Logging;
using Ryujinx.HLE.HOS.Ipc;
using System.Collections.Generic;

namespace Ryujinx.HLE.HOS.Services.Mii
{
    class IStaticService : IpcService
    {
        private Dictionary<int, ServiceProcessRequest> _commands;

        public override IReadOnlyDictionary<int, ServiceProcessRequest> Commands => _commands;

        public IStaticService()
        {
            _commands = new Dictionary<int, ServiceProcessRequest>()
            {
                { 0, GetDatabaseService }
            };
        }

        private long GetDatabaseService(ServiceCtx context)
        {
            uint unknown = context.RequestData.ReadUInt32();

            Logger.PrintStub(LogClass.ServiceMii, new { unknown });

            MakeObject(context, new IDatabaseService());

            return 0;
        }
    }
}