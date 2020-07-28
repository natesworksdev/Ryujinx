using Newtonsoft.Json.Linq;
using Ryujinx.Common.Logging;
using Ryujinx.Ui;
using System;
using System.Net;
using System.Runtime.InteropServices;

namespace Ryujinx
{
    public class UpdateParser
    {
        internal static bool Running;

        private static string _jobId;
        private static string _buildVer;
        private static string _platformExt;

        private static string _masterUrl = "https://ci.appveyor.com/api/projects/gdkchan/ryujinx/branch/master";
        private static string _buildUrl;

        public async static void BeginParse(MainWindow mainWindow, bool showVersionUpToDate)
        {
            if (Running) return;
            Running = true;
            mainWindow.UpdateMenuItem.Sensitive = false;

            // Detect current platform
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                _platformExt = "osx_x64.zip";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _platformExt = "win_x64.zip";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                _platformExt = "linux_x64.tar.gz";
            }

            // Get latest version number from Appveyor
            try
            {
                using (WebClient jsonClient = new WebClient())
                {
                    string fetchedJson = await jsonClient.DownloadStringTaskAsync(_masterUrl);
                    JObject jsonRoot   = JObject.Parse(fetchedJson);
                    JToken buildToken  = jsonRoot["build"];

                    _jobId    = (string)buildToken["jobs"][0]["jobId"];
                    _buildVer = (string)buildToken["version"];
                    _buildUrl = "https://ci.appveyor.com/api/buildjobs/" + _jobId + "/artifacts/ryujinx-" + _buildVer + "-" + _platformExt;
                }
            }
            catch (Exception exception)
            {
                Logger.PrintError(LogClass.Application, exception.Message);
                GtkDialog.CreateErrorDialog($"An error has occured when trying to get release information from GitHub.");

                return;
            }

            // Get Version from app.config to compare versions
            Version newVersion     = Version.Parse("0.0");
            Version currentVersion = Version.Parse("0.0");

            try
            {
                newVersion     = Version.Parse(_buildVer);
                currentVersion = Version.Parse(Program.Version);
            }
            catch
            {
                Logger.PrintWarning(LogClass.Application, "Failed to convert current Ryujinx version.");
            }

            if (newVersion < currentVersion)
            {
                if (showVersionUpToDate)
                {
                    GtkDialog.CreateInfoDialog("Ryujinx - Updater", "You are already using the most updated version of Ryujinx!", "");
                }

                Running = false;
                mainWindow.UpdateMenuItem.Sensitive = true;
                return;
            }

            // Show a message asking the user if they want to update
            UpdateDialog updateDialog = new UpdateDialog(mainWindow, newVersion, _buildUrl);
            updateDialog.Show();
        }
    }
}