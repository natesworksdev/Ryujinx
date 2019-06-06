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
        public static Gdk.Pixbuf RyujinxROMIcon { get; private set; }

        public static List<ApplicationData> ApplicationLibraryData { get; private set; }

        public struct ApplicationData
        {
            public Gdk.Pixbuf Icon;
            public string Game;
            public string Version;
            public string DLC;
            public string TP;
            public string LP;
            public string Path;
        }

        public static void Init()
        {
            using (Stream iconstream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Ryujinx.ryujinxROMIcon.png"))
            using (StreamReader reader = new StreamReader(iconstream))
            {
                RyujinxROMIcon = new Gdk.Pixbuf(iconstream);
            }

            string dat = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GameDirs.dat");
            if (File.Exists(dat) == false) { File.Create(dat).Close(); }
            string[] GameDirs = File.ReadAllLines(dat);
            List<string> Games = new List<string>();

            foreach (string GameDir in GameDirs)
            {
                if (Directory.Exists(GameDir) == false) { Logger.PrintError(LogClass.Application, "There is an invalid game directory in \"GameDirs.dat\""); }

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
                ApplicationData data = new ApplicationData()
                {
                    Icon = GetGameIcon(GamePath),
                    Game = (Path.GetExtension(GamePath) == ".nro") ? "Application" : "",
                    Version = "",
                    DLC = "",
                    TP = "",
                    LP = "",
                    Path = GamePath
                };
                ApplicationLibraryData.Add(data);
            }
        }

        internal static Gdk.Pixbuf GetGameIcon(string filePath)
        {
            using (FileStream Input = File.Open(filePath, FileMode.Open, FileAccess.Read))
            {
                BinaryReader Reader = new BinaryReader(Input);

                if (Path.GetExtension(filePath) == ".nro")
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
                    else { return RyujinxROMIcon; }
                }
                else
                {
                    return RyujinxROMIcon; //temp
                }
            }
        }
    }
}
