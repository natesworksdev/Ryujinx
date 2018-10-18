using Ryujinx.Common.Logging;
using Ryujinx.HLE.HOS.Ipc;
using System.Collections.Generic;

namespace Ryujinx.HLE.HOS.Services.Mm
{
    class IRequest : IpcService
    {
        private Dictionary<int, ServiceProcessRequest> m_Commands;

        public override IReadOnlyDictionary<int, ServiceProcessRequest> Commands => m_Commands;

        public IRequest()
        {
            m_Commands = new Dictionary<int, ServiceProcessRequest>()
            {
                { 0, InitializeOld },
                { 4, Initialize    },
                { 6, SetAndWait    },
                { 7, Get           }
            };
        }

        // InitializeOld(u32, u32, u32)
        public long InitializeOld(ServiceCtx Context)
        {
            int Unknown0 = Context.RequestData.ReadInt32();
            int Unknown1 = Context.RequestData.ReadInt32();
            int Unknown2 = Context.RequestData.ReadInt32();

            Logger.PrintStub(LogClass.ServiceMm, "Stubbed.");

            return 0;
        }

        // Initialize()
        public long Initialize(ServiceCtx Context)
        {
            Logger.PrintStub(LogClass.ServiceMm, "Stubbed.");

            return 0;
        }

        // SetAndWait(u32, u32, u32)
        public long SetAndWait(ServiceCtx Context)
        {
            int Unknown0 = Context.RequestData.ReadInt32();
            int Unknown1 = Context.RequestData.ReadInt32();
            int Unknown2 = Context.RequestData.ReadInt32();

            Logger.PrintStub(LogClass.ServiceMm, "Stubbed.");

            return 0;
        }

        // Get(u32) -> u32
        public long Get(ServiceCtx Context)
        {
            int Unknown0 = Context.RequestData.ReadInt32();

            Logger.PrintStub(LogClass.ServiceMm, "Stubbed.");

            Context.ResponseData.Write(0);

            return 0;
        }
    }
}