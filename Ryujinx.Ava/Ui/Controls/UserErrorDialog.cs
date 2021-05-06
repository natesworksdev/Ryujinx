using Ryujinx.Ava.Common;
using Ryujinx.Ava.Common.Locale;
using Ryujinx.Ava.Ui.Windows;

namespace Ryujinx.Ava.Ui.Controls
{
    internal class UserErrorDialog
    {
        private const string SetupGuideUrl =
            "https://github.com/Ryujinx/Ryujinx/wiki/Ryujinx-Setup-&-Configuration-Guide";

        private static string GetErrorCode(UserError error)
        {
            return $"RYU-{(uint)error:X4}";
        }

        private static string GetErrorTitle(UserError error)
        {
            return error switch
            {
                UserError.NoKeys => "Keys not found",
                UserError.NoFirmware => "Firmware not found",
                UserError.FirmwareParsingFailed => "Firmware parsing error",
                UserError.ApplicationNotFound => "Application not found",
                UserError.Unknown => "Unknown error",
                _ => "Undefined error"
            };
        }

        private static string GetErrorDescription(UserError error)
        {
            return error switch
            {
                UserError.NoKeys => "Ryujinx was unable to find your 'prod.keys' file",
                UserError.NoFirmware => "Ryujinx was unable to find any firmwares installed",
                UserError.FirmwareParsingFailed =>
                    "Ryujinx was unable to parse the provided firmware. This is usually caused by outdated keys.",
                UserError.ApplicationNotFound => "Ryujinx couldn't find a valid application at the given path.",
                UserError.Unknown => "An unknown error occured!",
                _ => "An undefined error occured! This shouldn't happen, please contact a dev!"
            };
        }

        private static bool IsCoveredBySetupGuide(UserError error)
        {
            return error switch
            {
                UserError.NoKeys or
                    UserError.NoFirmware or
                    UserError.FirmwareParsingFailed => true,
                _ => false
            };
        }

        private static string GetSetupGuideUrl(UserError error)
        {
            if (!IsCoveredBySetupGuide(error))
            {
                return null;
            }

            return error switch
            {
                UserError.NoKeys => SetupGuideUrl + "#initial-setup---placement-of-prodkeys",
                UserError.NoFirmware => SetupGuideUrl + "#initial-setup-continued---installation-of-firmware",
                _ => SetupGuideUrl
            };
        }

        public static async void ShowUserErrorDialog(UserError error, StyleableWindow owner)
        {
            string errorCode = GetErrorCode(error);

            bool isInSetupGuide = IsCoveredBySetupGuide(error);

            string setupButtonLabel = isInSetupGuide ? "Open the Setup Guide" : "";

            var result = await ContentDialogHelper.CreateInfoDialog(owner,
                string.Format(LocaleManager.Instance["DialogUserErrorDialogMessage"], errorCode, GetErrorTitle(error)),
                GetErrorDescription(error) + (isInSetupGuide
                    ? LocaleManager.Instance["DialogUserErrorDialogInfoMessage"]
                    : ""), setupButtonLabel, LocaleManager.Instance["InputDialogOk"],
                string.Format(LocaleManager.Instance["DialogUserErrorDialogTitle"], errorCode));

            if(result == UserResult.Ok)
            {
                OpenHelper.OpenUrl(GetSetupGuideUrl(error));
            }
        }
    }
}