using Ryujinx.Common;
using Ryujinx.Common.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.Versioning;

namespace Ryujinx.Ui.Common.Helper
{
    public static class FileAssociationHelper
    {
        public static bool IsTypeAssociationSupported => OperatingSystem.IsLinux();

        [SupportedOSPlatform("linux")]
        private static bool RegisterLinuxMimeTypes()
        {
            if (ReleaseInformation.IsFlatHubBuild())
            {
                return false;
            }

            string mimeDbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share", "mime");

            if (!File.Exists(Path.Combine(mimeDbPath, "packages", "Ryujinx.xml")))
            {
                string mimeTypesFile = Path.Combine(ReleaseInformation.GetBaseApplicationDirectory(), "mime", "Ryujinx.xml");
                using Process mimeProcess = new();

                mimeProcess.StartInfo.FileName = "xdg-mime";
                mimeProcess.StartInfo.Arguments = $"install --novendor --mode user {mimeTypesFile}";

                mimeProcess.Start();
                mimeProcess.WaitForExit();

                if (mimeProcess.ExitCode != 0)
                {
                    Logger.Error?.PrintMsg(LogClass.Application, $"Unable to install mime types. Make sure xdg-utils is installed. Process exited with code: {mimeProcess.ExitCode}");
                    return false;
                }

                using Process updateMimeProcess = new();

                updateMimeProcess.StartInfo.FileName = "update-mime-database";
                updateMimeProcess.StartInfo.Arguments = mimeDbPath;

                updateMimeProcess.Start();
                updateMimeProcess.WaitForExit();

                if (updateMimeProcess.ExitCode != 0)
                {
                    Logger.Error?.PrintMsg(LogClass.Application, $"Could not update local mime database. Process exited with code: {updateMimeProcess.ExitCode}");
                }
            }

            return true;
        }

        public static bool RegisterTypeAssociations()
        {
            if (OperatingSystem.IsLinux())
            {
                return RegisterLinuxMimeTypes();
            }

            // TODO: Add Windows and macOS support

            return false;
        }
    }
}