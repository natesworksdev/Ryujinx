using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Ryujinx.Updater
{
    public class Program
    {
        public static string RyuDir = Environment.CurrentDirectory;

        public static string updateSaveLocation;
        public static string metaFilePath;

        public static string versionNumber;
        public static string downloadUrl;

        private static void CloneDirectory(string root, string dest)
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

                CloneDirectory(directory, Path.Combine(dest, dirName));
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
        public static void Main()
        {
            // Create Temp Directory

            if (!Directory.Exists(Path.Combine(RyuDir, "Temp")))
            {
                Directory.CreateDirectory(Path.Combine(RyuDir, "Temp"));
            }

            updateSaveLocation = Path.Combine(RyuDir, "Temp", "RyujinxPackage.zip");
            metaFilePath = Path.Combine(RyuDir, "Meta.json");

            string[] metaFileData = File.ReadAllLines(metaFilePath);

            versionNumber = metaFileData[0];
            downloadUrl = metaFileData[1];

            MessageBox.Show(downloadUrl);

            using (WebClient client = new WebClient())
            {
                client.DownloadFile(downloadUrl, updateSaveLocation);
            }

            foreach (string file in Directory.GetFiles(RyuDir, "*", SearchOption.AllDirectories))
            {
                if (Path.GetFileName(file) != "Config.json" && Path.GetFileName(file) != "Meta.json" && Path.GetFileName(file) != "RyujinxPackage.zip")
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch
                    {

                    }
                }
            }

            ZipFile.ExtractToDirectory(updateSaveLocation, RyuDir, true);

            CloneDirectory(Path.Combine(RyuDir, "publish"), RyuDir);
            Directory.Delete(Path.Combine(RyuDir, "publish"), true);
            Directory.Delete(Path.Combine(RyuDir, "Temp"), true);

            Process.Start(Path.Combine(RyuDir, "Ryujinx.exe"));

            Application.Exit();
        }

    }
}