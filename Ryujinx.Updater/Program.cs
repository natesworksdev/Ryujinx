using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Security.Cryptography;
using System.Threading;

namespace Ryujinx.Updater
{
    class Program
    {
        public static string localAppPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Ryujinx");
        public static string ryuDir = Environment.CurrentDirectory;

        public static string updateSaveLocation;

        public static int lastPercentage;

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
                catch (Exception ex)
                {
                    File.Create(Path.Combine(ryuDir, "Updater Log.txt")).Close();
                    File.WriteAllText(Path.Combine(ryuDir, "Updater Log.txt"), ex.Message);
                    Environment.Exit(0);
                }

                MoveAllFilesOver(directory, Path.Combine(dest, dirName));
            }

            foreach (var file in Directory.GetFiles(root))
            {
                try
                {
                    File.Move(file, Path.Combine(dest, Path.GetFileName(file)), true);
                }
                catch (Exception ex)
                {
                    File.Create(Path.Combine(ryuDir, "Updater Log.txt")).Close();
                    File.WriteAllText(Path.Combine(ryuDir, "Updater Log.txt"), ex.Message);
                    Environment.Exit(0);
                }
            }
        }

        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                return;
            }

            string fileHash = args[1];

            Console.WriteLine($"Updating Ryujinx...");

            // Create temp directory

            if (!Directory.Exists(Path.Combine(localAppPath, "Temp")))
            {
                Directory.CreateDirectory(Path.Combine(localAppPath, "Temp"));
            }

            // Download latest update

            string downloadUrl = args[0];

            updateSaveLocation = Path.Combine(localAppPath, "Temp", "RyujinxPackage.zip");

            Console.WriteLine($"Downloading latest Ryujinx package...");

            WebClient client = new WebClient();

            client.DownloadProgressChanged += (s, e) =>
            {
                if (e.ProgressPercentage != lastPercentage)
                {
                    Console.WriteLine("Package downloading... " + e.ProgressPercentage + "%");
                }

                lastPercentage = e.ProgressPercentage;
            };

            client.DownloadFileTaskAsync(new Uri(downloadUrl), updateSaveLocation).Wait();

            // Extract Update .zip

            using (FileStream SourceStream = File.OpenRead(updateSaveLocation))
            {
                using (SHA256 myHash = SHA256.Create())
                {
                    byte[] hashValue = myHash.ComputeHash(SourceStream);

                    if (BitConverter.ToString(hashValue).Replace("-", "").ToUpper() != fileHash.ToUpper())
                    {
                        return;
                    }

                    SourceStream.Position = 0;

                    ZipArchive zipArchive = new ZipArchive(SourceStream);

                    Console.WriteLine($"Extracting Ryujinx package...");

                    zipArchive.ExtractToDirectory(Path.Combine(localAppPath, "Extract"));
                }
            }

            // Copy new files over to Ryujinx folder

            Console.WriteLine($"Replacing old version...");

            MoveAllFilesOver(Path.Combine(localAppPath, "Extract"), ryuDir);

            // Remove temp folders

            Directory.Delete(Path.Combine(localAppPath, "Extract"), true);
            Directory.Delete(Path.Combine(localAppPath, "Temp"), true);

            // Start new Ryujinx version and close Updater

            ProcessStartInfo startInfo = new ProcessStartInfo(Path.Combine(ryuDir, "Ryujinx.exe"));
            startInfo.UseShellExecute = true;
            Process.Start(startInfo);
        }
    }
}