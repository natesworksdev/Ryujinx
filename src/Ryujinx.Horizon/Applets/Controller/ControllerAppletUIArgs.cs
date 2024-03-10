using System.Collections.Generic;

namespace Ryujinx.Horizon.Applets.Controller
{
    public struct ControllerAppletUIArgs
    {
        public int PlayerCountMin;
        public int PlayerCountMax;
        public ControllerType SupportedStyles;
        public IEnumerable<PlayerIndex> SupportedPlayers;
        public bool IsDocked;
    }
}
