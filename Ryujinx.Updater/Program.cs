using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace Ryujinx.Updater
{
    class Program
    {
        private static void MoveAllFilesOver(string root, string dest)
        {
            foreach (var directory in Directory.GetDirectories(root))
            {
                string dirName = Path.GetFileName(directory);

                if (!Directory.Exists(Path.Combine(dest, dirName)))
                {
                    Directory.CreateDirectory(Path.Combine(dest, dirName));
                }

                MoveAllFilesOver(directory, Path.Combine(dest, dirName));
            }

            foreach (var file in Directory.GetFiles(root))
            {
                File.Move(file, Path.Combine(dest, Path.GetFileName(file)), true);
            }
        }

        static void Main(string[] args)
        {
            string homeDir = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
            string coreDir = Path.Combine(homeDir, "core");
            string updateCoreDir = Path.Combine(homeDir, "update", "core");

            // If update is present replace
            if (Directory.Exists(updateCoreDir))
            {
                MoveAllFilesOver(updateCoreDir, coreDir);
                Directory.Delete(updateCoreDir, true);
            }

            string filename = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "Ryujinx.exe" : "Ryujinx";

            // Launch Ryujinx
            Process process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    WorkingDirectory = Path.GetDirectoryName(Path.Combine(coreDir, filename)),
                    FileName = Path.Combine(coreDir, filename)
                }
            };

            process.Start();

            Environment.Exit(0);
        }
    }
}