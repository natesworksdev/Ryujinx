using Ryujinx.HLE.HOS.Ipc;
using System.Collections.Generic;

namespace Ryujinx.HLE.HOS.Services.Hid
{
    internal class IActiveApplicationDeviceList : IpcService
    {
        private Dictionary<int, ServiceProcessRequest> _mCommands;

        public override IReadOnlyDictionary<int, ServiceProcessRequest> Commands => _mCommands;

        public IActiveApplicationDeviceList()
        {
            _mCommands = new Dictionary<int, ServiceProcessRequest>()
            {
                { 0, ActivateVibrationDevice }
            };
        }

        public long ActivateVibrationDevice(ServiceCtx context)
        {
            int vibrationDeviceHandle = context.RequestData.ReadInt32();

            return 0;
        }
    }
}