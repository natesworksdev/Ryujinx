using Ryujinx.HLE.HOS.Ipc;
using System.Collections.Generic;

namespace Ryujinx.HLE.HOS.Services.Nifm
{
    internal class IStaticService : IpcService
    {
        private Dictionary<int, ServiceProcessRequest> _mCommands;

        public override IReadOnlyDictionary<int, ServiceProcessRequest> Commands => _mCommands;

        public IStaticService()
        {
            _mCommands = new Dictionary<int, ServiceProcessRequest>()
            {
                { 4, CreateGeneralServiceOld },
                { 5, CreateGeneralService    }
            };
        }

        public long CreateGeneralServiceOld(ServiceCtx context)
        {
            MakeObject(context, new IGeneralService());

            return 0;
        }

        public long CreateGeneralService(ServiceCtx context)
        {
            MakeObject(context, new IGeneralService());

            return 0;
        }
    }
}