using Ryujinx.Common.Logging;
using Ryujinx.HLE.HOS.Ipc;
using System.Collections.Generic;

namespace Ryujinx.HLE.HOS.Services.Pcv
{
    class IPcvService : IpcService
    {
        private Dictionary<int, ServiceProcessRequest> _commands;

        public override IReadOnlyDictionary<int, ServiceProcessRequest> Commands => _commands;

        public IPcvService(bool needInitialize = true)
        {
            _commands = new Dictionary<int, ServiceProcessRequest>
            {
                { 0, SetPowerEnabled },
                { 1, SetClockEnabled },
                { 2, SetClockRate    },
                { 7, SetReset        }
            };
        }

        private long SetPowerEnabled(ServiceCtx context)
        {
            Logger.PrintStub(LogClass.ServicePcv, "Stubbed.");

            return 0;
        }

        private long SetClockEnabled(ServiceCtx context)
        {
            Logger.PrintStub(LogClass.ServicePcv, "Stubbed.");

            return 0;
        }

        private long SetClockRate(ServiceCtx context)
        {
            Logger.PrintStub(LogClass.ServicePcv, "Stubbed.");

            return 0;
        }

        private long SetReset(ServiceCtx context)
        {
            Logger.PrintStub(LogClass.ServicePcv, "Stubbed.");

            return 0;
        }
    }
}