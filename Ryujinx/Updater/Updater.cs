using Gtk;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using ICSharpCode.SharpZipLib.Zip;
using Ryujinx.Common.Logging;
using Ryujinx.Ui;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace Ryujinx
{
    class Updater
    {
        private static readonly string HomeDir          = AppDomain.CurrentDomain.BaseDirectory;
        private static readonly string UpdateDir        = Path.Combine(Path.GetTempPath(), "Ryujinx", "update");
        private static readonly string UpdatePublishDir = Path.Combine(Path.GetTempPath(), "Ryujinx", "update", "publish");

        private static void MoveAllFilesOver(string root, string dest, UpdateDialog dialog)
        {
            foreach (string directory in Directory.GetDirectories(root))
            {
                string dirName = Path.GetFileName(directory);

                if (!Directory.Exists(Path.Combine(dest, dirName)))
                {
                    Directory.CreateDirectory(Path.Combine(dest, dirName));
                }

                MoveAllFilesOver(directory, Path.Combine(dest, dirName), dialog);
            }

            foreach (string file in Directory.GetFiles(root))
            {
                File.Move(file, Path.Combine(dest, Path.GetFileName(file)), true);

                Application.Invoke(delegate
                {
                    dialog.ProgressBar.Value++;
                });
            }
        }

        public static void CleanupUpdate()
        {
            foreach (string file in Directory.GetFiles(HomeDir, "*", SearchOption.AllDirectories))
            {
                if (Path.GetExtension(file).EndsWith(".ryuold"))
                {
                    File.Delete(file);
                }
            }
        }

        public async static void UpdateRyujinx(UpdateDialog updateDialog, string downloadUrl, bool isLinux)
        {
            // Empty update dir, although it shouldn't ever have anything inside it
            if (Directory.Exists(UpdateDir))
                Directory.Delete(UpdateDir, true);

            Directory.CreateDirectory(UpdateDir);
            string updateFile = Path.Combine(UpdateDir, "update.bin");

            // Download the update .zip
            updateDialog.MainText.Text        = "Downloading Update...";
            updateDialog.ProgressBar.Value    = 0;
            updateDialog.ProgressBar.MaxValue = 100;

            using (WebClient client = new WebClient())
            {
                client.DownloadProgressChanged += (_, args) =>
                {
                    updateDialog.ProgressBar.Value = args.ProgressPercentage;
                };

                await client.DownloadFileTaskAsync(downloadUrl, updateFile);
            }

            //Extract Update
            updateDialog.MainText.Text     = "Extracting Update...";
            updateDialog.ProgressBar.Value = 0;

            if (isLinux)
            {
                using (Stream         inStream   = File.OpenRead(updateFile))
                using (Stream         gzipStream = new GZipInputStream(inStream))
                using (TarInputStream tarStream  = new TarInputStream(gzipStream))
                {
                    updateDialog.ProgressBar.MaxValue = inStream.Length;

                    await Task.Run(() =>
                    {
                        TarEntry tarEntry;
                        while ((tarEntry = tarStream.GetNextEntry()) != null)
                        {
                            if (tarEntry.IsDirectory) continue;

                            string outPath = Path.Combine(UpdateDir, tarEntry.Name);

                            Directory.CreateDirectory(Path.GetDirectoryName(outPath));

                            using (FileStream outStream = File.OpenWrite(outPath))
                            {
                                tarStream.CopyEntryContents(outStream);
                            }

                            File.SetLastWriteTime(outPath, DateTime.SpecifyKind(tarEntry.ModTime, DateTimeKind.Utc));

                            TarEntry entry = tarEntry;
                            Application.Invoke(delegate
                            {
                                updateDialog.ProgressBar.Value += entry.Size;
                            });
                        }
                    });

                    updateDialog.ProgressBar.Value = inStream.Length;
                }
            }
            else
            {
                using (Stream  inStream = File.OpenRead(updateFile))
                using (ZipFile zipFile  = new ZipFile(inStream))
                {
                    updateDialog.ProgressBar.MaxValue = zipFile.Count;

                    await Task.Run(() =>
                    {
                        foreach (ZipEntry zipEntry in zipFile)
                        {
                            if (zipEntry.IsDirectory) continue;

                            string outPath = Path.Combine(UpdateDir, zipEntry.Name);

                            Directory.CreateDirectory(Path.GetDirectoryName(outPath));

                            using (Stream     zipStream = zipFile.GetInputStream(zipEntry))
                            using (FileStream outStream = File.OpenWrite(outPath))
                            {
                                zipStream.CopyTo(outStream);
                            }

                            File.SetLastWriteTime(outPath, DateTime.SpecifyKind(zipEntry.DateTime, DateTimeKind.Utc));

                            Application.Invoke(delegate
                            {
                                updateDialog.ProgressBar.Value++;
                            });
                        }
                    });
                }
            }

            // Delete downloaded zip
            File.Delete(updateFile);

            string[] allFiles = Directory.GetFiles(HomeDir, "*", SearchOption.AllDirectories);

            updateDialog.MainText.Text        = "Renaming Old Files...";
            updateDialog.ProgressBar.Value    = 0;
            updateDialog.ProgressBar.MaxValue = allFiles.Length;

            // Replace old files
            await Task.Run(() =>
            {
                foreach (string file in allFiles)
                {
                    if (!Path.GetExtension(file).Equals(".log"))
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
                }

                Application.Invoke(delegate
                {
                    updateDialog.MainText.Text        = "Adding New Files...";
                    updateDialog.ProgressBar.Value    = 0;
                    updateDialog.ProgressBar.MaxValue = Directory.GetFiles(UpdatePublishDir, "*", SearchOption.AllDirectories).Length;
                });

                MoveAllFilesOver(UpdatePublishDir, HomeDir, updateDialog);
            });

            Directory.Delete(UpdateDir, true);

            updateDialog.MainText.Text      = "Update Complete!";
            updateDialog.SecondaryText.Text = "Do you want to restart Ryujinx now?";
            updateDialog.Modal              = true;

            updateDialog.ProgressBar.Hide();
            updateDialog.YesButton.Show();
            updateDialog.NoButton.Show();
        }
    }
}