using Gtk;
using Newtonsoft.Json.Linq;
using Ryujinx.Common.Logging;
using Ryujinx.Ui;
using System;
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
        private static string _buildUrl = "https://ci.appveyor.com/api/projects/gdkchan/ryujinx/branch/master";
        private static string _buildCommit;
        private static string _branch;
        private static string _platformExt;

        public static string _buildArt;

        public static string RyuDir = Environment.CurrentDirectory;
        public static string localAppPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Ryujinx");
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

                // Begin the Appveyor parsing

                WebClient jsonClient = new WebClient();
                string fetchedJSON = jsonClient.DownloadString(_buildUrl);
                JObject jsonRoot = JObject.Parse(fetchedJSON);

                var __Build = jsonRoot["build"];

                string __Version = (string)__Build["version"];
                string __JobsId = (string)__Build["jobs"][0]["jobId"];
                string __branch = (string)__Build["branch"];
                string __buildCommit = (string)__Build["commitId"];

                _jobId = __JobsId;
                _buildVer = __Version;
                _buildArt = "https://ci.appveyor.com/api/buildjobs/" + _jobId + "/artifacts/ryujinx-" + _buildVer + "-" + _platformExt;
                _buildCommit = __buildCommit.Substring(0, 7);
                _branch = __branch;

                if (!Directory.Exists(localAppPath))
                {
                    Directory.CreateDirectory(localAppPath);
                }

                if (File.Exists(Path.Combine(localAppPath, "Version.json")))
                {
                    try
                    {
                        Version newVersion = Version.Parse(_buildVer);

                        string currentVersionJson = File.ReadAllLines(Path.Combine(localAppPath, "Version.json"))[0];
                        Version currentVersion = Version.Parse(currentVersionJson);

                        if (newVersion.CompareTo(currentVersion) == 0)
                        {
                            GtkDialog.CreateErrorDialog("You are already using the most updated version!");

                            return;
                        }
                    }
                    catch
                    {

                    }
                }

                using (MessageDialog dialog = GtkDialog.CreateAcceptDialog("Update", "Ryujinx - Update", "Would you like to update?", "Version " + _buildVer + " is available."))
                {
                    if (dialog.Run() == (int)ResponseType.Yes)
                    {
                        try
                        {
                            // Start Updater.exe

                            string updaterPath = Path.Combine(RyuDir, "Updater.exe");

                            ProcessStartInfo startInfo = new ProcessStartInfo(updaterPath);
                            startInfo.Arguments = _buildArt + " " + _buildVer;
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
                GtkDialog.CreateErrorDialog("Update canceled by user or failed to grab or parse the information.\nPlease try at a later time, or report the error to our GitHub.");
            }
        }

    }
}