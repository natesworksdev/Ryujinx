using System;
using System.Collections.Generic;
using System.Text;
using Ryujinx.HLE.Input;

namespace Ryujinx.Ui.Input
{
    class EmulatedDevices
    {
        public static Dictionary<HidControllerId, HidHostDevice> Devices;
    }
}
