using System;
using Ryujinx.Ava.Common.Locale;
using Ryujinx.Ava.UI.Helpers;
using Ryujinx.Common.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace Ryujinx.Modules
{
    internal static partial class Updater
    {
        private static string _buildVer;

        private static async Task<Version> GetCurrentVersion()
        {
            try
            {
                //return Version.Parse(Program.Version);
                return Version.Parse("1.1.0"); // Temporary code, will revert back
            }
            catch
            {
                Logger.Error?.Print(LogClass.Application, "Failed to convert the current Ryujinx version!");
                await ContentDialogHelper.CreateWarningDialog(
                    LocaleManager.Instance[LocaleKeys.DialogUpdaterConvertFailedMessage],
                    LocaleManager.Instance[LocaleKeys.DialogUpdaterCancelUpdateMessage]);
                return null;
            }
        }
    }
}
