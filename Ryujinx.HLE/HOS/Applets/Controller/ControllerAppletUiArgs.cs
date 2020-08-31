using Ryujinx.HLE.HOS.Services.Hid;
using System.Collections.Generic;

namespace Ryujinx.HLE.HOS.Applets
{
    public class ControllerAppletUiArgs
    {
        public int PlayerCountMin;
        public int PlayerCountMax;
        public ControllerType SupportedStyles;
        public bool IsDocked;
        public bool IsSinglePlayer;
        public bool PermitJoyDual;
        public uint[] IdentificationColors;
        public string[] ExplainTexts;
    }
}