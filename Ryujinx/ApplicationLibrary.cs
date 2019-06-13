using Ryujinx.Common.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace Ryujinx
{
    public class ApplicationLibrary
    {
        public static Gdk.Pixbuf RyujinxNSPIcon { get; private set; }
        public static Gdk.Pixbuf RyujinxXCIIcon { get; private set; }
        public static Gdk.Pixbuf RyujinxNCAIcon { get; private set; }
        public static Gdk.Pixbuf RyujinxNROIcon { get; private set; }
        public static Gdk.Pixbuf RyujinxNSOIcon { get; private set; }
        public static Gdk.Pixbuf RyujinxROMIcon { get; private set; }

        public static List<ApplicationData> ApplicationLibraryData { get; private set; }

        public struct ApplicationData
        {
            public Gdk.Pixbuf Icon;
            public string     Game;
            public string     Version;
            public string     DLC;
            public string     TP;
            public string     LP;
            public string     FileSize;
            public string     Path;
        }

        public static void Init()
        {
            RyujinxNSPIcon = new Gdk.Pixbuf(Assembly.GetExecutingAssembly(), "Ryujinx.GUI.assets.ryujinxNSPIcon.png", 75, 75);
            RyujinxXCIIcon = new Gdk.Pixbuf(Assembly.GetExecutingAssembly(), "Ryujinx.GUI.assets.ryujinxXCIIcon.png", 75, 75);
            RyujinxNCAIcon = new Gdk.Pixbuf(Assembly.GetExecutingAssembly(), "Ryujinx.GUI.assets.ryujinxNCAIcon.png", 75, 75);
            RyujinxNROIcon = new Gdk.Pixbuf(Assembly.GetExecutingAssembly(), "Ryujinx.GUI.assets.ryujinxNROIcon.png", 75, 75);
            RyujinxNSOIcon = new Gdk.Pixbuf(Assembly.GetExecutingAssembly(), "Ryujinx.GUI.assets.ryujinxNSOIcon.png", 75, 75);
            RyujinxROMIcon = new Gdk.Pixbuf(Assembly.GetExecutingAssembly(), "Ryujinx.GUI.assets.ryujinxROMIcon.png", 75, 75);

            List<string> Games = new List<string>();

            foreach (string GameDir in SwitchSettings.SwitchConfig.GameDirs)
            {
                if (Directory.Exists(GameDir) == false) { Logger.PrintError(LogClass.Application, $"\"GameDirs.dat\" contains an invalid directory: \"{GameDir}\""); continue; }

                DirectoryInfo GameDirInfo = new DirectoryInfo(GameDir);
                foreach (var Game in GameDirInfo.GetFiles())
                {
                    if ((Path.GetExtension(Game.ToString()) == ".xci") || (Path.GetExtension(Game.ToString()) == ".nca") || (Path.GetExtension(Game.ToString()) == ".nsp") || (Path.GetExtension(Game.ToString()) == ".pfs0") || (Path.GetExtension(Game.ToString()) == ".nro") || (Path.GetExtension(Game.ToString()) == ".nso"))
                    {
                        Games.Add(Game.ToString());
                    }
                }
            }

            ApplicationLibraryData = new List<ApplicationData>();
            foreach (string GamePath in Games)
            {
                double filesize      = new FileInfo(GamePath).Length * 0.000000000931;
                ApplicationData data = new ApplicationData()
                {
                    Icon     = GetGameIcon(GamePath),
                    Game     = (Path.GetExtension(GamePath) == ".nro") ? "Application" : "",
                    Version  = "",
                    DLC      = (Path.GetExtension(GamePath) == ".nro") ? "N/A" : "",
                    TP       = "",
                    LP       = "",
                    FileSize = (filesize < 1) ? (filesize * 1024).ToString("0.##") + "MB" : filesize.ToString("0.##") + "GB",
                    Path     = GamePath,
                };
                ApplicationLibraryData.Add(data);
            }
        }

        internal static Gdk.Pixbuf GetGameIcon(string filePath)
        {
            using (FileStream Input = File.Open(filePath, FileMode.Open, FileAccess.Read))
            {
                BinaryReader Reader = new BinaryReader(Input);

                if ((Path.GetExtension(filePath) == ".nsp") || (Path.GetExtension(filePath) == ".pfs0")) { return RyujinxNSPIcon; }

                else if (Path.GetExtension(filePath) == ".xci") { return RyujinxXCIIcon; }

                else if (Path.GetExtension(filePath) == ".nca") { return RyujinxNCAIcon; }

                else if (Path.GetExtension(filePath) == ".nso") { return RyujinxNSOIcon; }

                else if (Path.GetExtension(filePath) == ".nro")
                {
                    Input.Seek(24, SeekOrigin.Begin);
                    int AssetOffset = Reader.ReadInt32();

                    byte[] Read(long Position, int Size)
                    {
                        Input.Seek(Position, SeekOrigin.Begin);
                        return Reader.ReadBytes(Size);
                    }

                    if (Encoding.ASCII.GetString(Read(AssetOffset, 4)) == "ASET")
                    {
                        byte[] IconSectionInfo = Read(AssetOffset + 8, 0x10);

                        long IconOffset = BitConverter.ToInt64(IconSectionInfo, 0);
                        long IconSize = BitConverter.ToInt64(IconSectionInfo, 8);

                        byte[] IconData = Read(AssetOffset + IconOffset, (int)IconSize);

                        return new Gdk.Pixbuf(IconData, 75, 75);
                    }
                    else { return RyujinxNROIcon; }
                }

                else { return RyujinxROMIcon; }
            }
        }
    }
}
