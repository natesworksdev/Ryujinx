using System;
using System.Collections.Generic;
using System.Text;

namespace Ryujinx.HLE.Input
{
    public class HidEmulatedDevices
    {
        public enum HostDevice
        {
            None,
            Keyboard,
            GamePad_0,
            GamePad_1,
            GamePad_2,
            GamePad_3,
            GamePad_4,
            GamePad_5,
            GamePad_6,
            GamePad_7,
            GamePad_8,
        };

        public static Dictionary<HidControllerId, HostDevice> Devices;
    }
}
