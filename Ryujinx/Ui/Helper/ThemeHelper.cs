using Gtk;
using Ryujinx.Common.Logging;
using Ryujinx.Ui.Common.Configuration;
using System.IO;

namespace Ryujinx.Ui.Helper
{
    static class ThemeHelper
    {
        public static void ApplyTheme()
        {
            if (!ConfigurationState.Shared.Ui.EnableCustomTheme)
            {
                return;
            }

            if (File.Exists(ConfigurationState.Shared.Ui.CustomThemePath) && (Path.GetExtension(ConfigurationState.Shared.Ui.CustomThemePath) == ".css"))
            {
                CssProvider cssProvider = new CssProvider();

                cssProvider.LoadFromPath(ConfigurationState.Shared.Ui.CustomThemePath);

                StyleContext.AddProviderForScreen(Gdk.Screen.Default, cssProvider, 800);
            }
            else
            {
                Logger.Warning?.Print(LogClass.Application, $"The \"custom_theme_path\" section in \"Config.json\" contains an invalid path: \"{ConfigurationState.Shared.Ui.CustomThemePath}\".");

                ConfigurationState.Shared.Ui.CustomThemePath.Value   = "";
                ConfigurationState.Shared.Ui.EnableCustomTheme.Value = false;
                ConfigurationState.Shared.ToFileFormat().SaveConfig(Program.ConfigurationPath);
            }
        }
    }
}