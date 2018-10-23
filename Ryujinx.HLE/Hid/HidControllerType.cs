using System;

namespace Ryujinx.HLE.Input
{
    [Flags]
    public enum HidControllerType
    {
        ControllerTypeProController = (1 << 0),
        ControllerTypeHandheld      = (1 << 1),
        ControllerTypeJoyconPair    = (1 << 2),
        ControllerTypeJoyconLeft    = (1 << 3),
        ControllerTypeJoyconRight   = (1 << 4)
    }
}