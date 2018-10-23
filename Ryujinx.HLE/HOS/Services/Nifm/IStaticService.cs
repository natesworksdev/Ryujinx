using Ryujinx.HLE.HOS.Ipc;
using System.Collections.Generic;

namespace Ryujinx.HLE.HOS.Services.Nifm
{
    class StaticService : IpcService
    {
        private Dictionary<int, ServiceProcessRequest> _mCommands;

        public override IReadOnlyDictionary<int, ServiceProcessRequest> Commands => _mCommands;

        public StaticService()
        {
            _mCommands = new Dictionary<int, ServiceProcessRequest>()
            {
                { 4, CreateGeneralServiceOld },
                { 5, CreateGeneralService    }
            };
        }

        public long CreateGeneralServiceOld(ServiceCtx context)
        {
            MakeObject(context, new GeneralService());

            return 0;
        }

        public long CreateGeneralService(ServiceCtx context)
        {
            MakeObject(context, new GeneralService());

            return 0;
        }
    }
}