using Gtk;
using Newtonsoft.Json.Linq;
using Ryujinx.Common.Logging;
using Ryujinx.Ui;
using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;

namespace Ryujinx
{
    public class UpdateParser
    {
        private static string _jobId;
        private static string _buildVer;
        private static string _platformExt;

        private static string _masterUrl = "https://ci.appveyor.com/api/projects/gdkchan/ryujinx/branch/master";
        private static string _buildUrl;

        public static string RyuDir = Environment.CurrentDirectory;
        public static string localAppPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Ryujinx");
        public static WebClient Package = new WebClient();
        public static int PackageProgress;
        public static double Percentage;

        public static void BeginParse()
        {
            try
            {
                // Detect current platform

                if (RuntimeInformation.ProcessArchitecture == Architecture.X64)
                {
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
                }
                else
                {
                    Logger.PrintError(LogClass.Application, $"You are using an operating system architecture ({RuntimeInformation.ProcessArchitecture.ToString()}) not compatible with Ryujinx.");
                    GtkDialog.CreateErrorDialog($"You are using an operating system architecture ({RuntimeInformation.ProcessArchitecture.ToString()}) not compatible with Ryujinx.");

                    return;
                }

                // Begin the Appveyor parsing

                WebClient jsonClient = new WebClient();
                string fetchedJSON = jsonClient.DownloadString(_masterUrl);
                JObject jsonRoot = JObject.Parse(fetchedJSON);

                JToken _buildToken = jsonRoot["build"];

                _jobId = (string)_buildToken["jobs"][0]["jobId"];
                _buildVer = (string)_buildToken["version"];
                _buildUrl = "https://ci.appveyor.com/api/buildjobs/" + _jobId + "/artifacts/ryujinx-" + _buildVer + "-" + _platformExt;

                if (!Directory.Exists(localAppPath))
                {
                    Directory.CreateDirectory(localAppPath);
                }

                // Get Version from app.config to compare versions

                Version newVersion = Version.Parse(_buildVer);
                Version currentVersion = Version.Parse(ConfigurationManager.AppSettings["Version"]);

                if (newVersion < currentVersion)
                {
                    GtkDialog.CreateInfoDialog("Update", "Ryujinx - Updater", "You are already using the most updated version of Ryujinx!", "");

                    return;
                }

                // Show a message asking the user if they want to update

                using (MessageDialog dialog = GtkDialog.CreateAcceptDialog("Update", "Ryujinx - Update", "Would you like to update?", "Version " + _buildVer + " is available."))
                {
                    if (dialog.Run() == (int)ResponseType.Yes)
                    {
                        try
                        {
                            // Start Updater.exe

                            string updaterPath = Path.Combine(RyuDir, "Updater.exe");

                            ProcessStartInfo startInfo = new ProcessStartInfo(updaterPath);
                            startInfo.Arguments = _buildUrl + " " + _buildVer;
                            startInfo.UseShellExecute = true;
                            Process.Start(startInfo);

                            Application.Quit();
                        }
                        catch (Exception ex)
                        {
                            GtkDialog.CreateErrorDialog(ex.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.PrintError(LogClass.Application, ex.Message);
                GtkDialog.CreateErrorDialog("Update failed to grab or parse the information.\nPlease try at a later time, or report the error to our GitHub.");
            }
        }

    }
}