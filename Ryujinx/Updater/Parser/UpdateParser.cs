using Gtk;
using Ionic.Zip;
using Newtonsoft.Json.Linq;
using Ryujinx.Common.Logging;
using Ryujinx.HLE.FileSystem;
using Ryujinx.Ui;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using ZipFile = System.IO.Compression.ZipFile;

namespace Ryujinx.Updater.Parser
{
    public class UpdateParser
    {
        private static string _jobid;
        private static string _buildver;
        private static string _buildurl         = "https://ci.appveyor.com/api/projects/gdkchan/ryujinx/branch/master";
        private static string _buildcommit;
        private static string _branch;
        private static string _platformext;

        public static string    BuildArt;
        public static string    RyuDir          = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Ryujinx");
        public static WebClient Package         = new WebClient();
        public static int       PackageProgress;
        public static double    Percentage;
        public static void BeginParse()
        {
            try
            {
                //Detect current platform
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    _platformext = "osx_x64.zip";
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    _platformext = "win_x64.zip";
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    _platformext = "linux_x64.tar.gz";

                //Begin the Appveyor parsing
                WebClient JSONClient = new WebClient();
                string FetchedJSON          = JSONClient.DownloadString(_buildurl);
                var __JSONRoot              = JObject.Parse(FetchedJSON);
                var __Build                 = __JSONRoot["build"];
                string __Version            = (string)__Build["version"];
                string __JobsID             = (string)__Build["jobs"][0]["jobId"];
                string __branch             = (string)__Build["branch"];
                string __buildcommit        = (string)__Build["commitId"];
                _jobid                      = __JobsID;
                _buildver                   = __Version;
                BuildArt                   = "https://ci.appveyor.com/api/buildjobs/" + _jobid + "/artifacts/ryujinx-" + _buildver + "-" + _platformext;
                _buildcommit                = __buildcommit.Substring(0, 7);
                _branch                     = __branch;
                Logger.PrintInfo(LogClass.Application, "Fetched JSON and Parsed:" + Environment.NewLine + "MetaData: JobID(" + __JobsID + ") BuildVer(" + __Version + ")" + Environment.NewLine + "BuildURL(" + BuildArt + ")");
                Logger.PrintInfo(LogClass.Application, "Commit-id: (" + _buildcommit + ")" + " Branch: (" + _branch + ")");

                using (MessageDialog dialog = GtkDialog.CreateAcceptDialog("Update", _buildver))
                {
                    if (dialog.Run() == (int)ResponseType.Yes)
                    {
                        dialog.Dispose();
                        GrabPackage();
                    }
                }
                
            }
            catch (Exception ex)
            {
                Logger.PrintError(LogClass.Application, ex.Message);
                GtkDialog.CreateErrorDialog("Update canceled by user or failed to grab or parse the information.\nPlease try at a later time, or report the error to our GitHub.");
                return;
            }
            //UpdateData data = new UpdateData()
            //{
            //    JobID           = _jobid,
            //    BuildVer        = _buildver,
            //    BuildURL        = _buildurl,
            //    BuildArt        = BuildArt,
            //    BuildCommit     = _buildcommit,
            //    Branch          = _branch
            //};
        }

        private static async void GrabPackage()
        {
            //Check if paths exist
            if (!Directory.Exists(Path.Combine(RyuDir, "Data", "Update")) || !Directory.Exists(Path.Combine(RyuDir, "Data")) || !Directory.Exists(Path.Combine(Environment.CurrentDirectory, "temp")))
            {
                Directory.CreateDirectory(Path.Combine(RyuDir, "Data", "Update"));
                Directory.CreateDirectory(Path.Combine(RyuDir, "Data"));
                Directory.CreateDirectory(Path.Combine(Environment.CurrentDirectory, "temp"));
            }

            try
            {
                //Attempt to grab the latest package
                Package.DownloadProgressChanged += new DownloadProgressChangedEventHandler(PackageDownloadProgress);
                Package.DownloadFileCompleted += new AsyncCompletedEventHandler(PackageDownloadedAsync);
                using (MessageDialog dialog = GtkDialog.CreateProgressDialog("Update", "Ryujinx - Update", "Downloading update " + _buildver, "Please wait while we download the latest package and extract it."))
                {
                    dialog.Run();
                }
            }
            catch (Exception ex)
            {
                Logger.PrintError(LogClass.Application, ex.InnerException.ToString());
                GtkDialog.CreateErrorDialog(ex.Message);
                return;
            }
        }
        private static async void PackageDownloadedAsync(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Cancelled == true)
            {
                Logger.PrintError(LogClass.Application, "Package download failed or cancelled");
                return;
            }
            else
            {
                Logger.PrintWarning(LogClass.Application, "Package is now installing");
                await ExtractPackageAsync();
                return;
            }
        }

        private static void PackageDownloadProgress(object sender, DownloadProgressChangedEventArgs e)
        {
            Percentage         = e.ProgressPercentage;
            PackageProgress    = e.ProgressPercentage;
        }
        public static async Task ExtractPackageAsync()
        {
            try
            {
                //Begin the extaction process
                await Task.Run(() => ZipFile.ExtractToDirectory(Path.Combine(RyuDir, "Data", "Update", "RyujinxPackage.zip"), Path.Combine(Environment.CurrentDirectory, "temp")));
                try
                {
                    Process.Start(new ProcessStartInfo(Path.Combine(Environment.CurrentDirectory, "temp", "publish", "Ryujinx.exe"), "/U") { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    GtkDialog.CreateErrorDialog("Package installation has failed\nCheck the log for more information.");
                    Logger.PrintError(LogClass.Application, "Package installation has failed\n" + ex.InnerException.ToString());
                    return;
                }
                Application.Quit();
            }
            catch (Exception ex)
            {
                Logger.PrintError(LogClass.Application, "Package installation has failed\n" + ex.InnerException.ToString());
                GtkDialog.CreateErrorDialog("Package installation has failed\nCheck the log for more information.");
                return;
            }
        }
    }
}