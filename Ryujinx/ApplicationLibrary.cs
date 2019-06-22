using LibHac;
using LibHac.Fs;
using LibHac.Fs.NcaUtils;
using Ryujinx.Common.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

            List<string> Games = new List<string>();

            foreach (string GameDir in SwitchSettings.SwitchConfig.GameDirs)
            {
                if (Directory.Exists(GameDir) == false) { Logger.PrintError(LogClass.Application, $"The \"game_dirs\" section in \"Config.json\" contains an invalid directory: \"{GameDir}\""); continue; }

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
                double filesize = new FileInfo(GamePath).Length * 0.000000000931;

                using (FileStream file = new FileStream(GamePath, FileMode.Open, FileAccess.Read))
                {
                    Nca controlNca          = null;
                    PartitionFileSystem PFS = null;
                    IFileSystem ControlFs   = null;
                    string TitleName        = null;
                    Gdk.Pixbuf GameIcon     = null;

                    if ((Path.GetExtension(GamePath) == ".nsp") || (Path.GetExtension(GamePath) == ".pfs0"))
                    {
                        PFS = new PartitionFileSystem(file.AsStorage());
                    }

                    else if (Path.GetExtension(GamePath) == ".xci")
                    {
                        Xci xci = new Xci(MainMenu.device.System.KeySet, file.AsStorage());
                        PFS     = xci.OpenPartition(XciPartitionType.Secure);
                    }

                    if (PFS != null)
                    { 
                        foreach (DirectoryEntry ticketEntry in PFS.EnumerateEntries("*.tik"))
                        {
                            Ticket ticket = new Ticket(PFS.OpenFile(ticketEntry.FullPath, OpenMode.Read).AsStream());

                            if (!MainMenu.device.System.KeySet.TitleKeys.ContainsKey(ticket.RightsId))
                            {
                                MainMenu.device.System.KeySet.TitleKeys.Add(ticket.RightsId, ticket.GetTitleKey(MainMenu.device.System.KeySet));
                            }
                        }

                        foreach (DirectoryEntry fileEntry in PFS.EnumerateEntries("*.nca"))
                        {
                            Nca nca = new Nca(MainMenu.device.System.KeySet, PFS.OpenFile(fileEntry.FullPath, OpenMode.Read).AsStorage());
                            if (nca.Header.ContentType == ContentType.Control)
                            {
                                controlNca = nca;
                            }
                        }

                        ControlFs = controlNca.OpenFileSystem(NcaSectionType.Data, MainMenu.device.System.FsIntegrityCheckLevel);
                    }

                    if ((Path.GetExtension(GamePath) == ".nca") || (Path.GetExtension(GamePath) == ".nro") || (Path.GetExtension(GamePath) == ".nso")) { TitleName = Path.GetFileName(GamePath); }
                    else
                    {
                        IFile controlFile = ControlFs.OpenFile("/control.nacp", OpenMode.Read);
                        Nacp ControlData  = new Nacp(controlFile.AsStream());

                        TitleName = ControlData.Descriptions[(int)MainMenu.device.System.State.DesiredTitleLanguage].Title;
                        if (string.IsNullOrWhiteSpace(TitleName))
                        {
                            TitleName = ControlData.Descriptions.ToList().Find(x => !string.IsNullOrWhiteSpace(x.Title)).Title;
                        }
                    }

                    if (Path.GetExtension(GamePath) == ".nca") { GameIcon = RyujinxNCAIcon; }

                    else if ((Path.GetExtension(GamePath) == ".xci") || (Path.GetExtension(GamePath) == ".nsp") || (Path.GetExtension(GamePath) == ".pfs0"))
                    {
                        try
                        {
                            IFile logo = ControlFs.OpenFile($"/icon_{MainMenu.device.System.State.DesiredTitleLanguage}.dat", OpenMode.Read);
                            GameIcon = new Gdk.Pixbuf(logo.AsStream(), 75, 75);
                        }
                        catch(FileNotFoundException)
                        {
                            try
                            {
                                IFile logo = ControlFs.OpenFile($"/icon_AmericanEnglish.dat", OpenMode.Read);
                                GameIcon = new Gdk.Pixbuf(logo.AsStream(), 75, 75);
                            }
                            catch (FileNotFoundException)
                            {
                                if (Path.GetExtension(GamePath) == ".xci") { GameIcon = RyujinxXCIIcon; }
                                else { GameIcon = RyujinxNSPIcon; }
                            }
                        }
                    }

                    else if (Path.GetExtension(GamePath) == ".nso") { GameIcon = RyujinxNSOIcon; }

                    else if (Path.GetExtension(GamePath) == ".nro")
                    {
                        BinaryReader Reader = new BinaryReader(file);

                        file.Seek(24, SeekOrigin.Begin);
                        int AssetOffset = Reader.ReadInt32();

                        byte[] Read(long Position, int Size)
                        {
                            file.Seek(Position, SeekOrigin.Begin);
                            return Reader.ReadBytes(Size);
                        }

                        if (Encoding.ASCII.GetString(Read(AssetOffset, 4)) == "ASET")
                        {
                            byte[] IconSectionInfo = Read(AssetOffset + 8, 0x10);

                            long IconOffset = BitConverter.ToInt64(IconSectionInfo, 0);
                            long IconSize   = BitConverter.ToInt64(IconSectionInfo, 8);

                            byte[] IconData = Read(AssetOffset + IconOffset, (int)IconSize);

                            GameIcon = new Gdk.Pixbuf(IconData, 75, 75);
                        }
                        else { GameIcon = RyujinxNROIcon; }
                    }

                    ApplicationData data = new ApplicationData()
                    {
                        Icon     = GameIcon,
                        Game     = TitleName,
                        TP       = "",
                        LP       = "",
                        FileSize = (filesize < 1) ? (filesize * 1024).ToString("0.##") + "MB" : filesize.ToString("0.##") + "GB",
                        Path     = GamePath,
                    };

                    ApplicationLibraryData.Add(data);
                }
            }
        }
    }
}
