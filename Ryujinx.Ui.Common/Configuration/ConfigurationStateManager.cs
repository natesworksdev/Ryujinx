using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ryujinx.Ui.Common.Configuration
{
    public class ConfigurationStateManager
    {
        public static ConfigurationState.LoggerSection LoggerSection
        {
            get
            {
                return ConfigurationState.GameInstance.Logger ?? ConfigurationState.Instance.Logger;
            }
        }
        public static ConfigurationState.HidSection HidSection
        {
            get
            {
                return ConfigurationState.GameInstance.Hid ?? ConfigurationState.Instance.Hid;
            }
        }
        public static ConfigurationState.GraphicsSection GraphicsSection
        {
            get
            {
                return ConfigurationState.GameInstance.Graphics ?? ConfigurationState.Instance.Graphics;
            }
        }
    }
}
