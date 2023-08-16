using Ryujinx.Common.Configuration;
using ShellLink;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using Image = System.Drawing.Image;

namespace Ryujinx.Ui.Common.Helper
{
    public static class ShortcutHelper
    {
        [SupportedOSPlatform("windows")]
        private static void CreateShortcutWindows(string applicationFilePath, byte[] iconData, string iconPath, string cleanedAppName, string desktopPath)
        {
            string basePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, AppDomain.CurrentDomain.FriendlyName + ".exe");

            MemoryStream iconDataStream = new(iconData);
            using Image image = Image.FromStream(iconDataStream);
            using Bitmap bitmap = new(128, 128);
            using System.Drawing.Graphics graphic = System.Drawing.Graphics.FromImage(bitmap);
            graphic.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphic.DrawImage(image, 0, 0, 128, 128);
            SaveBitmapAsIcon(bitmap, iconPath + ".ico");

            var shortcut = Shortcut.CreateShortcut(basePath, GetArgsString(basePath, applicationFilePath), iconPath + ".ico", 0);
            shortcut.StringData.NameString = cleanedAppName;
            shortcut.WriteToFile(Path.Combine(desktopPath, cleanedAppName + ".lnk"));
        }

        [SupportedOSPlatform("linux")]
        private static void CreateShortcutLinux(string applicationFilePath, byte[] iconData, string iconPath, string desktopPath, string cleanedAppName)
        {
            string basePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Ryujinx.sh");
            var desktopFile = File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "shortcut-template.desktop"));

            var image = SixLabors.ImageSharp.Image.Load<Rgba32>(iconData);
            image.SaveAsPng(iconPath + ".png");

            using StreamWriter outputFile = new(Path.Combine(desktopPath, cleanedAppName + ".desktop"));
            outputFile.Write(desktopFile, cleanedAppName, iconPath + ".png", GetArgsString(basePath, applicationFilePath));
        }

        [SupportedOSPlatform("macos")]
        private static void CreateShortcutMacos(string appFilePath, byte[] iconData, string desktopPath, string cleanedAppName)
        {
            string basePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, AppDomain.CurrentDomain.FriendlyName);
            // Macos .App folder
            string contentFolderPath = Path.Combine(desktopPath, cleanedAppName + ".app", "Contents");
            string scriptFolderPath = Path.Combine(contentFolderPath, "MacOS");
            if (!Directory.Exists(scriptFolderPath))
            {
                Directory.CreateDirectory(scriptFolderPath);
            }

            // Runner script
            string script = """
                #!/bin/sh
                {0} --fullscreen {1}
                """;

            string scriptPath = Path.Combine(scriptFolderPath, "runner");

            using StreamWriter scriptFile = new(scriptPath);
            scriptFile.Write(script, basePath, $"\"{appFilePath}\"");


            // Set execute permission
            FileInfo fileInfo = new(scriptPath);
            fileInfo.UnixFileMode |= UnixFileMode.UserExecute;

            // img
            string resourceFolderPath = Path.Combine(contentFolderPath, "Resources");
            if (!Directory.Exists(resourceFolderPath))
            {
                Directory.CreateDirectory(resourceFolderPath);
            }

            var image = SixLabors.ImageSharp.Image.Load<Rgba32>(iconData);
            image.SaveAsPng(Path.Combine(resourceFolderPath, "icon.png"));

            // plist file
            string plist = """
                <?xml version="1.0" encoding="UTF-8" standalone="no"?><plist version="1.0">
                  <dict>
                    <key>CFBundleExecutable</key>
                    <string>runner</string>
                    <key>CFBundleGetInfoString</key>
                    <string>runner</string>
                    <key>CFBundleVersion</key>
                    <string>1.0</string>
                    <key>CFBundleShortVersionString</key>
                    <string>1.0</string>
                    <key>CFBundleIconName</key>
                    <string>AppIcon</string>
                    <key>CFBundleIconFile</key>
                    <string>icon.png</string>
                    <key>UIPrerenderedIcon</key>
                    <true/>
                </dict>
                </plist>
                """;

            using StreamWriter plistFile = new(Path.Combine(contentFolderPath, "Info.plist"));
            plistFile.Write(plist, cleanedAppName);
        }

        public static void CreateAppShortcut(string applicationFilePath, string applicationName, string applicationId, byte[] iconData)
        {
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            string cleanedAppName = string.Join("_", applicationName.Split(Path.GetInvalidFileNameChars()));

            if (OperatingSystem.IsWindows())
            {
                string iconPath = Path.Combine(AppDataManager.BaseDirPath, "games", applicationId, "app");
                CreateShortcutWindows(applicationFilePath, iconData, iconPath, cleanedAppName, desktopPath);
                return;
            }

            if (OperatingSystem.IsLinux())
            {
                string iconPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share", "icons", "Ryujinx");
                Directory.CreateDirectory(iconPath);
                CreateShortcutLinux(applicationFilePath, iconData, Path.Combine(iconPath, applicationId), desktopPath, cleanedAppName);
                return;
            }

            if (OperatingSystem.IsMacOS())
            {
                CreateShortcutMacos(applicationFilePath, iconData, desktopPath, cleanedAppName);
                return;
            }

            throw new NotImplementedException("Shortcut support has not been implemented yet for this OS.");
        }

        private static string GetArgsString(string basePath, string appFilePath)
        {
            // args are first defined as a list, for easier adjustments in the future
            var argsList = new List<string>
            {
                basePath,
                "--fullscreen",
                $"\"{appFilePath}\"",
            };
            return String.Join(" ", argsList);
        }

        /// <summary>
        /// Creates a Icon (.ico) file using the source bitmap image at the specified file path.
        /// </summary>
        /// <param name="source">The source bitmap image that will be saved as an .ico file</param>
        /// <param name="filePath">The location that the new .ico file will be saved too (Make sure to include '.ico' in the path).</param>
        [SupportedOSPlatform("windows")]
        private static void SaveBitmapAsIcon(Bitmap source, string filePath)
        {
            // Code Modified From https://stackoverflow.com/a/11448060/368354 by Benlitz
            using FileStream fs = new(filePath, FileMode.Create);
            fs.Write(new ReadOnlySpan<byte>(new byte[] { 0, 0, 1, 0, 1, 0, 0, 0, 0, 0, 1, 0, 32, 0, 0, 0, 0, 0, 22, 0, 0, 0 }));

            // Writing actual data
            source.Save(fs, ImageFormat.Png);
            // Getting data length (file length minus header)
            long Len = fs.Length - 22;
            // Write it in the correct place
            fs.Seek(14, SeekOrigin.Begin);
            fs.WriteByte((byte)Len);
            fs.WriteByte((byte)(Len >> 8));
        }
    }
}
