using Ryujinx.Common.Configuration;
using System;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.PixelFormats;

namespace Ryujinx.Common.Utilities
{
    public static class DesktopShortcut
    {
        public static void CreateAppShortcut(string appFilePath, string appName, string titleId, byte[] iconData)
        {
            string basePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Ryujinx");
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

            string iconPath = Path.Combine(AppDataManager.BaseDirPath, "games", titleId, "app");
            string cleanedAppName = string.Join("_", appName.Split(Path.GetInvalidFileNameChars()));
#if OS_WINDOWS
            if (OperatingSystem.IsWindows())
            {
                MemoryStream iconDataStream = new(iconData);
                using (System.Drawing.Image image = System.Drawing.Image.FromStream(iconDataStream))
                {
                    using System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(128, 128);
                    using System.Drawing.Graphics graphic = System.Drawing.Graphics.FromImage(bitmap);
                    graphic.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    graphic.DrawImage(image, 0, 0, 128, 128);
                    SaveBitmapAsIcon(bitmap, iconPath + ".ico");
                }
                IWshRuntimeLibrary.IWshShortcut shortcut = new IWshRuntimeLibrary.WshShell().CreateShortcut(Path.Combine(desktopPath, cleanedAppName + ".lnk"));
                shortcut.Description = cleanedAppName;
                shortcut.TargetPath = basePath + ".exe";
                shortcut.IconLocation = iconPath + ".ico";
                shortcut.Arguments = $"""{basePath} "{appFilePath}" --fullscreen""";
                shortcut.Save();
            }
#else
            //if (OperatingSystem.IsMacOS())
            //{

            //}
            if (OperatingSystem.IsLinux())
            {
                var image = Image.Load<Rgba32>(iconData);
                image.SaveAsPng(iconPath + ".png");
                var desktopFile = """
                    [Desktop Entry]
                    Version=1.0
                    Name={0}
                    Type=Application
                    Icon={1}
                    Exec={2} {3} %f
                    Comment=A Nintendo Switch Emulator
                    GenericName=Nintendo Switch Emulator
                    Terminal=false
                    Categories=Game;Emulator;
                    MimeType=application/x-nx-nca;application/x-nx-nro;application/x-nx-nso;application/x-nx-nsp;application/x-nx-xci;
                    Keywords=Switch;Nintendo;Emulator;
                    StartupWMClass=Ryujinx
                    PrefersNonDefaultGPU=true

                    """;
                using StreamWriter outputFile = new StreamWriter(Path.Combine(desktopPath, cleanedAppName + ".desktop"));
                outputFile.Write(String.Format(desktopFile, cleanedAppName, iconPath + ".png", basePath, $"\"appFilePath\""));
            }
#endif
        }

#if OS_WINDOWS

        /// <summary>
        /// Creates a Icon (.ico) file using the source bitmap image at the specified file path.
        /// </summary>
        /// <param name="source">The source bitmap image that will be saved as an .ico file</param>
        /// <param name="filePath">The location that the new .ico file will be saved too (Make sure to include '.ico' in the path).</param>
        private static void SaveBitmapAsIcon(System.Drawing.Bitmap source, string filePath)
        {
            if (!OperatingSystem.IsWindows()) return;

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
#endif
    }
}
