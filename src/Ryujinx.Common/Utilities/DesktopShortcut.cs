using Ryujinx.Common.Configuration;
using System;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.PixelFormats;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace Ryujinx.Common.Utilities
{
    public static class DesktopShortcut
    {
        public static void CreateAppShortcut(string appFilePath, string appName, string titleId, byte[] iconData)
        {
            string basePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, AppDomain.CurrentDomain.FriendlyName);
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

            string iconPath = Path.Combine(AppDataManager.BaseDirPath, "games", titleId, "app");
            string cleanedAppName = string.Join("_", appName.Split(Path.GetInvalidFileNameChars()));

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

                IShellLink shortcut = (IShellLink)new ShellLink();

                shortcut.SetDescription(cleanedAppName);
                shortcut.SetPath(basePath + ".exe");
                shortcut.SetIconLocation(iconPath + ".ico", 0);
                shortcut.SetArguments($"""{basePath} "{appFilePath}" --fullscreen""");

                IPersistFile file = (IPersistFile)shortcut;
                file.Save(Path.Combine(desktopPath, cleanedAppName + ".lnk"), false);
            }
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
        }

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
    }

    #region Implementing the ShellLink Interfaces
    [ComImport]
    [Guid("00021401-0000-0000-C000-000000000046")]
    internal class ShellLink
    {
    }

    /// <summary>
    /// The IShellLink interface is imported using ComImport, instead of directly referencing the COM library in the project (which causes issues).
    /// This handles the various properties of shortcuts on Windows. Create the object using: <code>IShellLink shortcut = (IShellLink)new ShellLink();</code>
    /// </summary>
    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("000214F9-0000-0000-C000-000000000046")]
    internal interface IShellLink
    {
        // The full list of members present here must be kept, otherwise ShellLink starts to misbehave. For additional information,
        // visit: http://www.vbaccelerator.com/home/NET/Code/Libraries/Shell_Projects/Creating_and_Modifying_Shortcuts/article.html
        void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cchMaxPath, out IntPtr pfd, int fFlags);
        void GetIDList(out IntPtr ppidl);
        void SetIDList(IntPtr pidl);
        void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cchMaxName);
        void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
        void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cchMaxPath);
        void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
        void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cchMaxPath);
        void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
        void GetHotkey(out short pwHotkey);
        void SetHotkey(short wHotkey);
        void GetShowCmd(out int piShowCmd);
        void SetShowCmd(int iShowCmd);
        void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cchIconPath, out int piIcon);
        void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
        void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, int dwReserved);
        void Resolve(IntPtr hwnd, int fFlags);
        void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
    }
    #endregion
}
