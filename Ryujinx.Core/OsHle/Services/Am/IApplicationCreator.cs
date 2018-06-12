using Ryujinx.HLE.OsHle.Ipc;
using System.Collections.Generic;

namespace Ryujinx.HLE.OsHle.Services.Am
{
    class IApplicationCreator : IpcService
    {
        private Dictionary<int, ServiceProcessRequest> m_Commands;

        public override IReadOnlyDictionary<int, ServiceProcessRequest> Commands => m_Commands;

        public IApplicationCreator()
        {
            m_Commands = new Dictionary<int, ServiceProcessRequest>()
            {
                { 0,   CreateApplication                    },
                { 1,   PopLaunchRequestedApplication        },
                { 10,  CreateSystemApplication              },
                { 100, PopFloatingApplicationForDevelopment }
            };
        }

        public long CreateApplication(ServiceCtx Context)
        {
            MakeObject(Context, new IApplicationAccessor());

            return 0;
        }

        public long PopLaunchRequestedApplication(ServiceCtx Context)
        {
            MakeObject(Context, new IApplicationAccessor());

            return 0;
        }

        public long CreateSystemApplication(ServiceCtx Context)
        {
            MakeObject(Context, new IApplicationAccessor());

            return 0;
        }

        public long PopFloatingApplicationForDevelopment(ServiceCtx Context)
        {
            MakeObject(Context, new IApplicationAccessor());

            return 0;
        }
    }
}
