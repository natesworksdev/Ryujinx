using Ryujinx.HLE.HOS.Services.Hid;
using System.Collections.Generic;

namespace Ryujinx.HLE.HOS.Applets
{
    public class ControllerAppletUiArgs
    {
        public int PlayerCountMin;
        public int PlayerCountMax;
        public ControllerType SupportedStyles;
        public IEnumerable<PlayerIndex> SupportedPlayers;
        public bool IsDocked;
        public bool IsSinglePlayer;
        public uint[] IdentificationColors;
        public string[] ExplainTexts;
    }
}