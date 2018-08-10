using System;
using System.Collections.Generic;
using System.Text;

namespace Ryujinx.HLE.Input
{
    public class HidEmulatedDevices
    {
        public struct EmulatedDevices
        {
            public int Handheld;
            public int Player1;
            public int Player2;
            public int Player3;
            public int Player4;
            public int Player5;
            public int Player6;
            public int Player7;
            public int Player8;
            public int PlayerUnknown;
        };

        public static EmulatedDevices Devices;
    }
}
