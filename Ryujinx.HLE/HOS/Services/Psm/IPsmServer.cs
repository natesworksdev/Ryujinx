using Ryujinx.Common.Logging;
using Ryujinx.HLE.HOS.Ipc;
using System.Collections.Generic;

namespace Ryujinx.HLE.HOS.Services.Psm
{
    class PsmServer : IpcService
    {
        enum ChargerType : int
        {
            None,
            ChargerOrDock,
            UsbC
        }

        private Dictionary<int, ServiceProcessRequest> _mCommands;

        public override IReadOnlyDictionary<int, ServiceProcessRequest> Commands => _mCommands;

        public PsmServer()
        {
            _mCommands = new Dictionary<int, ServiceProcessRequest>()
            {
                { 0, GetBatteryChargePercentage },
                { 1, GetChargerType             },
                { 7, OpenSession                }
            };
        }

        // GetBatteryChargePercentage() -> u32
        public static long GetBatteryChargePercentage(ServiceCtx context)
        {
            int chargePercentage = 100;

            context.ResponseData.Write(chargePercentage);

            Logger.PrintStub(LogClass.ServicePsm, $"Stubbed. ChargePercentage: {chargePercentage}");

            return 0;
        }

        // GetChargerType() -> u32
        public static long GetChargerType(ServiceCtx context)
        {
            context.ResponseData.Write((int)ChargerType.ChargerOrDock);

            Logger.PrintStub(LogClass.ServicePsm, $"Stubbed. ChargerType: {ChargerType.ChargerOrDock}");

            return 0;
        }

        // OpenSession() -> IPsmSession
        public long OpenSession(ServiceCtx context)
        {
            MakeObject(context, new PsmSession(context.Device.System));

            return 0;
        }
    }
}