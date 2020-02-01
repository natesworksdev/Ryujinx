using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;

namespace Ryujinx.Updater
{
    public class Program
    {
        public static string RyuDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Ryujinx");
        public static string launchDir = Environment.CurrentDirectory;

        public static string updateSaveLocation;

        private static void MoveAllFilesOver(string root, string dest)
        {
            foreach (var directory in Directory.GetDirectories(root))
            {
                string dirName = Path.GetFileName(directory);

                try
                {
                    if (!Directory.Exists(Path.Combine(dest, dirName)))
                    {
                        Directory.CreateDirectory(Path.Combine(dest, dirName));
                    }
                }
                catch
                {

                }

                MoveAllFilesOver(directory, Path.Combine(dest, dirName));
            }

            foreach (var file in Directory.GetFiles(root))
            {
                try
                {
                    File.Move(file, Path.Combine(dest, Path.GetFileName(file)), true);
                }
                catch
                {

                }
            }
        }

        [STAThread]
        public static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                return;
            }

            File.WriteAllText(Path.Combine(launchDir, "Version.json"), args[1]);

            // Create temp directory

            if (!Directory.Exists(Path.Combine(RyuDir, "Temp")))
            {
                Directory.CreateDirectory(Path.Combine(RyuDir, "Temp"));
            }

            // Download latest update

            string downloadUrl = args[0];

            updateSaveLocation = Path.Combine(RyuDir, "Temp", "RyujinxPackage.zip");

            using (WebClient client = new WebClient())
            {
                client.DownloadFile(downloadUrl, updateSaveLocation);
            }

            // Extract Update .zip

            ZipFile.ExtractToDirectory(updateSaveLocation, RyuDir, true);

            // Copy new files over to Ryujinx folder

            MoveAllFilesOver(Path.Combine(RyuDir, "publish"), launchDir);

            // Remove temp folders

            Directory.Delete(Path.Combine(RyuDir, "publish"), true);
            Directory.Delete(Path.Combine(RyuDir, "Temp"), true);

            // Start new Ryujinx version and close Updater

            Process.Start(Path.Combine(launchDir, "Ryujinx.exe"));
        }

    }
}