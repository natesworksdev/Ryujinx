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
                { 1, FinalizeOld   },
                { 2, SetAndWaitOld },
                { 3, GetOld        },
                { 4, Initialize    },
                { 5, Finalize      },
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

            Logger.PrintStub(LogClass.ServiceMm, $"Stubbed. Unknown0: {Unknown0} - " +
                                                 $"Unknown1: {Unknown1} - Unknown2: {Unknown2}");

            return 0;
        }

        // FinalizeOld(u32)
        public long FinalizeOld(ServiceCtx Context)
        {
            Context.Device.Gpu.UninitializeVideoDecoder();

            Logger.PrintStub(LogClass.ServiceMm, "Stubbed.");

            return 0;
        }

        // SetAndWaitOld(u32, u32, u32)
        public long SetAndWaitOld(ServiceCtx Context)
        {
            int Unknown0 = Context.RequestData.ReadInt32();
            int Unknown1 = Context.RequestData.ReadInt32();
            int Unknown2 = Context.RequestData.ReadInt32();

            Logger.PrintStub(LogClass.ServiceMm, $"Stubbed. Unknown0: {Unknown0} - " +
                                                 $"Unknown1: {Unknown1} - Unknown2: {Unknown2}");
            return 0;
        }

        // GetOld(u32) -> u32
        public long GetOld(ServiceCtx Context)
        {
            int Unknown0 = Context.RequestData.ReadInt32();

            Logger.PrintStub(LogClass.ServiceMm, $"Stubbed. Unknown0: {Unknown0}");

            Context.ResponseData.Write(0);

            return 0;
        }

        // Initialize()
        public long Initialize(ServiceCtx Context)
        {
            Logger.PrintStub(LogClass.ServiceMm, "Stubbed.");

            return 0;
        }

        // Finalize(u32)
        public long Finalize(ServiceCtx Context)
        {
            Context.Device.Gpu.UninitializeVideoDecoder();

            Logger.PrintStub(LogClass.ServiceMm, "Stubbed.");

            return 0;
        }

        // SetAndWait(u32, u32, u32)
        public long SetAndWait(ServiceCtx Context)
        {
            int Unknown0 = Context.RequestData.ReadInt32();
            int Unknown1 = Context.RequestData.ReadInt32();
            int Unknown2 = Context.RequestData.ReadInt32();

            Logger.PrintStub(LogClass.ServiceMm, $"Stubbed. Unknown0: {Unknown0} - " +
                                                 $"Unknown1: {Unknown1} - Unknown2: {Unknown2}");

            return 0;
        }

        // Get(u32) -> u32
        public long Get(ServiceCtx Context)
        {
            int Unknown0 = Context.RequestData.ReadInt32();

            Logger.PrintStub(LogClass.ServiceMm, $"Stubbed. Unknown0: {Unknown0}");

            Context.ResponseData.Write(0);

            return 0;
        }
    }
}