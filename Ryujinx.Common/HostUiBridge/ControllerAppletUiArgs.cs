using Ryujinx.Common.Configuration.Hid;
using System.Collections.Generic;

namespace Ryujinx.Common.HostUiBridge
{
    public struct ControllerAppletUiArgs
    {
        public int PlayerCountMin;
        public int PlayerCountMax;
        public ControllerType SupportedStyles;
        public IEnumerable<PlayerIndex> SupportedPlayers;
        public bool IsDocked;
    }
}