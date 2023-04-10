using Ryujinx.Common.Configuration;
using System;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Ryujinx.Common.Utilities
{
    public static class DesktopShortcut
    {
        public static void CreateAppShortcut(string appFilePath, string appName, string titleId, byte[] iconData)
        {
            MemoryStream iconDataStream = new(iconData);
            string basePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Ryujinx");
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

            string iconPath = Path.Combine(AppDataManager.BaseDirPath, "games", titleId, "app.ico");
            string cleanedAppName = string.Join("_", appName.Split(Path.GetInvalidFileNameChars()));

            if (OperatingSystem.IsWindows())
            {
                using (Image image = Image.FromStream(iconDataStream))
                {
                    using Bitmap bitmap = new Bitmap(128, 128);
                    using Graphics graphic = Graphics.FromImage((Image)bitmap);
                    graphic.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphic.DrawImage(image, 0, 0, 128, 128);
                    SaveBitmapAsIcon(bitmap, iconPath);
                }

                IWshRuntimeLibrary.IWshShortcut shortcut = new IWshRuntimeLibrary.WshShell().CreateShortcut(Path.Combine(desktopPath, cleanedAppName + ".lnk"));
                shortcut.Description = cleanedAppName;
                shortcut.TargetPath = basePath + ".exe";
                shortcut.IconLocation = iconPath;
                shortcut.Arguments = $"""{basePath} "{appFilePath}" --fullscreen""";
                shortcut.Save();
            }
            else if (OperatingSystem.IsLinux())
            {

            }
            else if (OperatingSystem.IsMacOS())
            {

            }
        }

        /// <summary>
        /// Creates a Icon (.ico) file using the source bitmap image at the specified file path.
        /// </summary>
        /// <param name="source">The source bitmap image that will be saved as an .ico file</param>
        /// <param name="filePath">The location that the new .ico file will be saved too (Make sure to include '.ico' in the path).</param>
        /// <exception cref="NotSupportedException"></exception>
        private static void SaveBitmapAsIcon(Bitmap source, string filePath)
        {
            if (!OperatingSystem.IsWindows())
            {
                throw new NotSupportedException("Cannot save .ico files on Operating Systems other then Windows.");
            }

            // Code Modified From https://stackoverflow.com/a/11448060/368354 by Benlitz
            using FileStream FS = new FileStream(filePath, FileMode.Create);
            // ICO header
            FS.WriteByte(0);
            FS.WriteByte(0);
            FS.WriteByte(1);
            FS.WriteByte(0);
            FS.WriteByte(1);
            FS.WriteByte(0);
            // Image size
            // Set to 0 for 256 px width/height
            FS.WriteByte(0);
            FS.WriteByte(0);
            // Palette
            FS.WriteByte(0);
            // Reserved
            FS.WriteByte(0);
            // Number of color planes
            FS.WriteByte(1);
            FS.WriteByte(0);
            // Bits per pixel
            FS.WriteByte(32);
            FS.WriteByte(0);
            // Data size, will be written after the data
            FS.WriteByte(0);
            FS.WriteByte(0);
            FS.WriteByte(0);
            FS.WriteByte(0);
            // Offset to image data, fixed at 22
            FS.WriteByte(22);
            FS.WriteByte(0);
            FS.WriteByte(0);
            FS.WriteByte(0);
            // Writing actual data
            source.Save(FS, System.Drawing.Imaging.ImageFormat.Png);
            // Getting data length (file length minus header)
            long Len = FS.Length - 22;
            // Write it in the correct place
            FS.Seek(14, SeekOrigin.Begin);
            FS.WriteByte((byte)Len);
            FS.WriteByte((byte)(Len >> 8));
        }
    }
}
