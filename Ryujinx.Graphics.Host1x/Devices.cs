using Ryujinx.Graphics.Device;
using System.Collections.Generic;

namespace Ryujinx.Graphics.Host1x
{
    class Devices
    {
        private readonly Dictionary<ClassId, IDeviceState> _devices = new Dictionary<ClassId, IDeviceState>();

        public void RegisterDevice(ClassId classId, IDeviceState device)
        {
            _devices[classId] = device;
        }

        public IDeviceState GetDevice(ClassId classId)
        {
            return _devices.TryGetValue(classId, out IDeviceState device) ? device : null;
        }
    }
}
