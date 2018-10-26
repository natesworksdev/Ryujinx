using Ryujinx.Common.Logging;
using Ryujinx.HLE.HOS.Ipc;
using System.Collections.Generic;

namespace Ryujinx.HLE.HOS.Services.Irs
{
    internal class IIrSensorServer : IpcService
    {
        private Dictionary<int, ServiceProcessRequest> _mCommands;

        public override IReadOnlyDictionary<int, ServiceProcessRequest> Commands => _mCommands;

        private bool _activated;

        public IIrSensorServer()
        {
            _mCommands = new Dictionary<int, ServiceProcessRequest>()
            {
                { 302, ActivateIrsensor   },
                { 303, DeactivateIrsensor }
            };
        }

        // ActivateIrsensor(nn::applet::AppletResourceUserId, pid)
        public long ActivateIrsensor(ServiceCtx context)
        {
            long appletResourceUserId = context.RequestData.ReadInt64();

            Logger.PrintStub(LogClass.ServiceIrs, $"Stubbed. AppletResourceUserId: {appletResourceUserId}");

            return 0;
        }

        // DeactivateIrsensor(nn::applet::AppletResourceUserId, pid)
        public long DeactivateIrsensor(ServiceCtx context)
        {
            long appletResourceUserId = context.RequestData.ReadInt64();

            Logger.PrintStub(LogClass.ServiceIrs, $"Stubbed. AppletResourceUserId: {appletResourceUserId}");

            return 0;
        }
    }
}