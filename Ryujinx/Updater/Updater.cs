using Ryujinx.Ui;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Gtk;
using Ryujinx.Common.Logging;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using ICSharpCode.SharpZipLib.Zip;

namespace Ryujinx
{
    class Updater
    {
        private static readonly string HomeDir          = Path.Combine(AppDomain.CurrentDomain.BaseDirectory);
        private static readonly string UpdateDir        = Path.Combine(Path.GetTempPath(), "Ryujinx", "update");
        private static readonly string UpdatePublishDir = Path.Combine(Path.GetTempPath(), "Ryujinx", "update", "publish");

        private static void MoveAllFilesOver(string root, string dest)
        {
            foreach (string directory in Directory.GetDirectories(root))
            {
                string dirName = Path.GetFileName(directory);

                if (dirName != "Logs")
                {
                    if (!Directory.Exists(Path.Combine(dest, dirName)))
                    {
                        Directory.CreateDirectory(Path.Combine(dest, dirName));
                    }

                    MoveAllFilesOver(directory, Path.Combine(dest, dirName));
                }
            }

            foreach (string file in Directory.GetFiles(root))
            {
                File.Move(file, Path.Combine(dest, Path.GetFileName(file)), true);
            }
        }

        public static void CleanupUpdate()
        {
            foreach (string file in Directory.GetFiles(HomeDir, "*", SearchOption.AllDirectories))
            {
                if (Path.GetExtension(file).EndsWith("ryuold"))
                {
                    File.Delete(file);
                }
            }
        }

        public static void ExtractTGZ(String gzArchiveName, String destFolder)
        {
            using (Stream     inStream   = File.OpenRead(gzArchiveName))
            using (Stream     gzipStream = new GZipInputStream(inStream))
            using (TarArchive tarArchive = TarArchive.CreateInputTarArchive(gzipStream))
            {
                tarArchive.ExtractContents(destFolder);
                tarArchive.Close();
            }
        }

        public async static void UpdateRyujinx(UpdateDialog updateDialog, string downloadUrl, bool isLinux)
        {
            updateDialog.MainText.Text = "Downloading Update...";

            // Empty update dir, although it shouldn't ever have anything inside it
            if (Directory.Exists(UpdateDir))
                Directory.Delete(UpdateDir, true);

            Directory.CreateDirectory(UpdateDir);

            // Download the update .zip
            string updateFile = Path.Combine(UpdateDir, "update.bin");

            using (WebClient client = new WebClient())
            {
                client.DownloadProgressChanged += (_, args) =>
                {
                    updateDialog.ProgressBar.Value = args.ProgressPercentage;
                };

                await client.DownloadFileTaskAsync(downloadUrl, updateFile);

                //Extract Update
                updateDialog.MainText.Text = "Extracting Update...";

                await Task.Run(() =>
                {
                    if (isLinux)
                    {
                        ExtractTGZ(updateFile, UpdateDir);
                    } else
                    {
                        FastZip fastZip = new FastZip();
                        string fileFilter = null;
                        fastZip.ExtractZip(updateFile, UpdateDir, fileFilter);
                    }
                });

                // Delete downloaded zip
                File.Delete(updateFile);

                string[] allFiles = Directory.GetFiles(HomeDir, "*", SearchOption.AllDirectories);
                updateDialog.ProgressBar.MaxValue = allFiles.Length;
                updateDialog.MainText.Text        = "Replacing Files...";
                updateDialog.ProgressBar.Value    = 0;

                // Replace old files
                await Task.Run(() =>
                {
                    foreach (string file in Directory.GetFiles(HomeDir, "*", SearchOption.AllDirectories))
                    {
                        try
                        {
                            File.Move(file, file + ".ryuold");

                            Application.Invoke(delegate
                            {
                                updateDialog.ProgressBar.Value++;
                            });
                        }
                        catch
                        {
                            Logger.PrintWarning(LogClass.Application, "Updater wasn't able to rename file: " + file);
                        }
                    }
                });

                MoveAllFilesOver(UpdatePublishDir, HomeDir);

                updateDialog.MainText.Text      = "Update Complete!";
                updateDialog.SecondaryText.Text = "Do you want to restart Ryujinx now?";
                
                updateDialog.ProgressBar.Hide();
                updateDialog.YesButton.Show();
                updateDialog.NoButton.Show();
            }
        }
    }
}