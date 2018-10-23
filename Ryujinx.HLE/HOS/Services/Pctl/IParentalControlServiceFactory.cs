using Ryujinx.HLE.HOS.Ipc;
using System.Collections.Generic;

namespace Ryujinx.HLE.HOS.Services.Pctl
{
    class ParentalControlServiceFactory : IpcService
    {
        private Dictionary<int, ServiceProcessRequest> _mCommands;

        public override IReadOnlyDictionary<int, ServiceProcessRequest> Commands => _mCommands;

        public ParentalControlServiceFactory()
        {
            _mCommands = new Dictionary<int, ServiceProcessRequest>()
            {
                { 0, CreateService                  },
                { 1, CreateServiceWithoutInitialize }
            };
        }

        public long CreateService(ServiceCtx context)
        {
            MakeObject(context, new ParentalControlService());

            return 0;
        }

        public long CreateServiceWithoutInitialize(ServiceCtx context)
        {
            MakeObject(context, new ParentalControlService(false));

            return 0;
        }
    }
}