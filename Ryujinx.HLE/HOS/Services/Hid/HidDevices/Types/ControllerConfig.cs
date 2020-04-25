using Ryujinx.Common.Hid;

namespace Ryujinx.HLE.HOS.Services.Hid
{
    public struct ControllerConfig
    {
        public PlayerIndex    Player;
        public ControllerType Type;
    }
}