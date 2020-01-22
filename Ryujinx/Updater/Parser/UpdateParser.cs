using Gtk;
using Newtonsoft.Json.Linq;
using Ryujinx.Common.Logging;
using Ryujinx.Ui;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Ryujinx.Updater.Parser
{
    public class UpdateParser
    {
        private static string       _JobID;
        private static string       _BuildVer;
        private static string       _BuildURL               = "https://ci.appveyor.com/api/projects/gdkchan/ryujinx/branch/master";
        public static string        _BuildArt;
        private static string       _BuildCommit;
        private static string       _Branch;
        private static string       _PlatformExt;
        public static string        _RyuDir                 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Ryujinx");
        public static WebClient     _Package                = new WebClient();
        private static string       _URLStr;
        public static int           _PackageProgress;
        public static double        _Percentage;
        public static void BeginParse()
        {
            try
            {
                //Detect current platform
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    _PlatformExt = "osx_x64.zip";
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    _PlatformExt = "win_x64.zip";
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    _PlatformExt = "linux_x64.tar.gz";

                WebClient JSONClient = new WebClient();
                string FetchedJSON          = JSONClient.DownloadString(_BuildURL);
                var __JSONRoot              = JObject.Parse(FetchedJSON);
                var __Build                 = __JSONRoot["build"];
                string __Version            = (string)__Build["version"];
                string __JobsID             = (string)__Build["jobs"][0]["jobId"];
                string __Branch             = (string)__Build["branch"];
                string __BuildCommit        = (string)__Build["commitId"];
                _JobID                      = __JobsID;
                _BuildVer                   = __Version;
                _BuildArt                   = "https://ci.appveyor.com/api/buildjobs/" + _JobID + "/artifacts/ryujinx-" + _BuildVer + "-" + _PlatformExt;
                _BuildCommit                = __BuildCommit.Substring(0, 7);
                _Branch                     = __Branch;
                Logger.PrintInfo(LogClass.Application, "Fetched JSON and Parsed:" + Environment.NewLine + "MetaData: JobID(" + __JobsID + ") BuildVer(" + __Version + ")" + Environment.NewLine + "BuildURL(" + _BuildArt + ")");
                Logger.PrintInfo(LogClass.Application, "Commit-id: (" + _BuildCommit + ")" + " Branch: (" + _Branch + ")");

                using (MessageDialog dialog = GtkDialog.CreateAcceptDialog("Update", _BuildVer))
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
            UpdateData data = new UpdateData()
            {
                JobID           = _JobID,
                BuildVer        = _BuildVer,
                BuildURL        = _BuildURL,
                BuildArt        = _BuildArt,
                BuildCommit     = _BuildCommit,
                Branch          = _Branch
            };
        }

        private static async void GrabPackage()
        {
            if (!Directory.Exists(Path.Combine(_RyuDir, "Data", "Update")) || !Directory.Exists(Path.Combine(_RyuDir, "Data")))
            {
                Directory.CreateDirectory(Path.Combine(_RyuDir, "Data", "Update"));
                Directory.CreateDirectory(Path.Combine(_RyuDir, "Data"));
            }

            try
            {
                _Package.DownloadProgressChanged += new DownloadProgressChangedEventHandler(PackageDownloadProgress);
                _Package.DownloadFileCompleted += new AsyncCompletedEventHandler(PackageDownloadedAsync);
                using (MessageDialog dialog = await GtkDialog.CreateProgressDialogAsync(false, "Update", "Ryujinx - Update", "Downloading update " + _BuildVer + ", 0% complete...", "Please wait while we download the latest package"))
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
                using (MessageDialog dialog = await GtkDialog.CreateProgressDialogAsync(true, "Update", "Ryujinx - Update", "Installing update " + _BuildVer + "...", "Please wait while we install the latest package"))
                {
                    dialog.Run();
                }
                return;
            }
        }

        private static void PackageDownloadProgress(object sender, DownloadProgressChangedEventArgs e)
        {
            _Percentage         = e.ProgressPercentage;
            _PackageProgress    = e.ProgressPercentage;
        }
        public static async Task ExtractPackageAsync()
        {
            try
            {
                //using (ZipFile Package = ZipFile.Read(Path.Combine(_RyuDir, "Data", "Update", "RyujinxPackage.zip")))
                //{
                //    await Task.Run(() => Package.ExtractAll(_InstallDir, ExtractExistingFileAction.OverwriteSilently));
                //}
                throw new Exception("This is a test exception.\nThis is the package extract thread.");
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
