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
        private static string _masterUrl = "https://raw.githubusercontent.com/Thog/temp_test/6afe6009c6a6258f65255865933d909be3b2d661/latest.json";

        public static string RyuDir = Environment.CurrentDirectory;
        public static string localAppPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Ryujinx");
        public static WebClient Package = new WebClient();
        public static int PackageProgress;
        public static double Percentage;

        public static void BeginParse()
        {
            // Detect current platform
            int platform = -1;

            if (RuntimeInformation.ProcessArchitecture == Architecture.X64)
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    platform = 0;
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    platform = 1;
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    platform = 2;
                }
            }
            else
            {
                Logger.PrintError(LogClass.Application, $"You are using an operating system architecture ({RuntimeInformation.ProcessArchitecture.ToString()}) not compatible with Ryujinx.");
                GtkDialog.CreateWarningDialog($"You are using an operating system architecture ({RuntimeInformation.ProcessArchitecture.ToString()}) not compatible with Ryujinx.", "");

                return;
            }

            // Begin the Appveyor parsing

            WebClient jsonClient = new WebClient();
            string fetchedJSON = jsonClient.DownloadString(_masterUrl);
            JObject jsonRoot = JObject.Parse(fetchedJSON);

            string fileHash = (string)jsonRoot["artifacts"][platform]["fileHash"];
            string version = (string)jsonRoot["version"];
            string url = (string)jsonRoot["artifacts"][platform]["url"];

            if (!Directory.Exists(localAppPath))
            {
                Directory.CreateDirectory(localAppPath);
            }

            // Get Version from app.config to compare versions

            Version newVersion = Version.Parse(version);
            Version currentVersion = new Version();

            try
            {
                currentVersion = Version.Parse(Program.Version);
            }
            catch
            {
                currentVersion = Version.Parse("0.0.0");
            }

            if (newVersion < currentVersion)
            {
                GtkDialog.CreateDialog("Update", "Ryujinx - Updater", "You are already using the most updated version of Ryujinx!");

                return;
            }

            // Show a message asking the user if they want to update

            using (MessageDialog dialog = GtkDialog.CreateAcceptDialog("Ryujinx - Update", "Would you like to update?", "Version " + version + " is available."))
            {
                if (dialog.Run() == (int)ResponseType.Yes)
                {
                    try
                    {
                        // Start Updater.exe

                        string updaterPath = Path.Combine(RyuDir, "Updater.exe");

                        ProcessStartInfo startInfo = new ProcessStartInfo(updaterPath);
                        startInfo.Arguments = url + " " + fileHash;
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
    }

}