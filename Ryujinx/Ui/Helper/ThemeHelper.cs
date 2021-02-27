using Gtk;
using Ryujinx.Common.Logging;
using Ryujinx.Configuration;
using System.IO;

namespace Ryujinx.Ui.Helper
{
    static class ThemeHelper
    {
        public static void ApplyTheme()
        {
            if (!GlobalConfigurationState.Instance.Ui.EnableCustomTheme)
            {
                return;
            }

            if (File.Exists(GlobalConfigurationState.Instance.Ui.CustomThemePath) && (Path.GetExtension(GlobalConfigurationState.Instance.Ui.CustomThemePath) == ".css"))
            {
                CssProvider cssProvider = new CssProvider();

                cssProvider.LoadFromPath(GlobalConfigurationState.Instance.Ui.CustomThemePath);

                StyleContext.AddProviderForScreen(Gdk.Screen.Default, cssProvider, 800);
            }
            else
            {
                Logger.Warning?.Print(LogClass.Application, $"The \"custom_theme_path\" section in \"Config.json\" contains an invalid path: \"{GlobalConfigurationState.Instance.Ui.CustomThemePath}\".");

                GlobalConfigurationState.Instance.Ui.CustomThemePath.Value   = "";
                GlobalConfigurationState.Instance.Ui.EnableCustomTheme.Value = false;
                GlobalConfigurationState.Instance.ToFileFormat().SaveConfig(Program.ConfigurationPath);
            }
        }
    }
}