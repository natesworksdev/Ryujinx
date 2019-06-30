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
        public static Gdk.Pixbuf RyujinxNspIcon { get; private set; }
        public static Gdk.Pixbuf RyujinxXciIcon { get; private set; }
        public static Gdk.Pixbuf RyujinxNcaIcon { get; private set; }
        public static Gdk.Pixbuf RyujinxNroIcon { get; private set; }
        public static Gdk.Pixbuf RyujinxNsoIcon { get; private set; }

        public static List<ApplicationData> ApplicationLibraryData { get; private set; }

        public struct ApplicationData
        {
            public Gdk.Pixbuf Icon;
            public string     GameName;
            public string     TimePlayed;
            public string     LastPlayed;
            public string     FileSize;
            public string     Path;
        }

        public static void Init()
        {
            RyujinxNspIcon = new Gdk.Pixbuf(Assembly.GetExecutingAssembly(), "Ryujinx.GUI.assets.ryujinxNSPIcon.png", 75, 75);
            RyujinxXciIcon = new Gdk.Pixbuf(Assembly.GetExecutingAssembly(), "Ryujinx.GUI.assets.ryujinxXCIIcon.png", 75, 75);
            RyujinxNcaIcon = new Gdk.Pixbuf(Assembly.GetExecutingAssembly(), "Ryujinx.GUI.assets.ryujinxNCAIcon.png", 75, 75);
            RyujinxNroIcon = new Gdk.Pixbuf(Assembly.GetExecutingAssembly(), "Ryujinx.GUI.assets.ryujinxNROIcon.png", 75, 75);
            RyujinxNsoIcon = new Gdk.Pixbuf(Assembly.GetExecutingAssembly(), "Ryujinx.GUI.assets.ryujinxNSOIcon.png", 75, 75);

            List<string> Games = new List<string>();

            foreach (string GameDir in SwitchSettings.SwitchConfig.GameDirs)
            {
                if (Directory.Exists(GameDir) == false)
                {
                    Logger.PrintError(LogClass.Application, $"The \"game_dirs\" section in \"Config.json\" contains an invalid directory: \"{GameDir}\"");

                    continue;
                }

                DirectoryInfo GameDirInfo = new DirectoryInfo(GameDir);
                foreach (var Game in GameDirInfo.GetFiles())
                {
                    if ((Path.GetExtension(Game.ToString()) == ".xci")  ||
                        (Path.GetExtension(Game.ToString()) == ".nca")  ||
                        (Path.GetExtension(Game.ToString()) == ".nsp")  ||
                        (Path.GetExtension(Game.ToString()) == ".pfs0") ||
                        (Path.GetExtension(Game.ToString()) == ".nro")  ||
                        (Path.GetExtension(Game.ToString()) == ".nso"))
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
                    PartitionFileSystem pfs = null;
                    IFileSystem controlFs   = null;
                    string TitleName        = null;
                    Gdk.Pixbuf GameIcon     = null;

                    if ((Path.GetExtension(GamePath) == ".nsp") || (Path.GetExtension(GamePath) == ".pfs0"))
                    {
                        pfs = new PartitionFileSystem(file.AsStorage());
                    }

                    else if (Path.GetExtension(GamePath) == ".xci")
                    {
                        Xci xci = new Xci(MainWindow._device.System.KeySet, file.AsStorage());
                        pfs     = xci.OpenPartition(XciPartitionType.Secure);
                    }

                    if (pfs != null)
                    { 
                        foreach (DirectoryEntry ticketEntry in pfs.EnumerateEntries("*.tik"))
                        {
                            Ticket ticket = new Ticket(pfs.OpenFile(ticketEntry.FullPath, OpenMode.Read).AsStream());

                            if (!MainWindow._device.System.KeySet.TitleKeys.ContainsKey(ticket.RightsId))
                            {
                                MainWindow._device.System.KeySet.TitleKeys.Add(ticket.RightsId, ticket.GetTitleKey(MainWindow._device.System.KeySet));
                            }
                        }

                        foreach (DirectoryEntry fileEntry in pfs.EnumerateEntries("*.nca"))
                        {
                            Nca nca = new Nca(MainWindow._device.System.KeySet, pfs.OpenFile(fileEntry.FullPath, OpenMode.Read).AsStorage());
                            if (nca.Header.ContentType == ContentType.Control)
                            {
                                controlNca = nca;
                            }
                        }

                        controlFs = controlNca.OpenFileSystem(NcaSectionType.Data, MainWindow._device.System.FsIntegrityCheckLevel);
                    }

                    if ((Path.GetExtension(GamePath) == ".nca") || (Path.GetExtension(GamePath) == ".nro") || (Path.GetExtension(GamePath) == ".nso")) { TitleName = Path.GetFileName(GamePath); }
                    else
                    {
                        IFile controlFile = controlFs.OpenFile("/control.nacp", OpenMode.Read);
                        Nacp  controlData = new Nacp(controlFile.AsStream());

                        TitleName = controlData.Descriptions[(int)MainWindow._device.System.State.DesiredTitleLanguage].Title;
                        if (string.IsNullOrWhiteSpace(TitleName))
                        {
                            TitleName = controlData.Descriptions.ToList().Find(x => !string.IsNullOrWhiteSpace(x.Title)).Title;
                        }
                    }

                    if (Path.GetExtension(GamePath) == ".nca") { GameIcon = RyujinxNcaIcon; }

                    else if ((Path.GetExtension(GamePath) == ".xci") || (Path.GetExtension(GamePath) == ".nsp") || (Path.GetExtension(GamePath) == ".pfs0"))
                    {
                        try
                        {
                            IFile logo = controlFs.OpenFile($"/icon_{MainWindow._device.System.State.DesiredTitleLanguage}.dat", OpenMode.Read);
                            GameIcon = new Gdk.Pixbuf(logo.AsStream(), 75, 75);
                        }
                        catch(FileNotFoundException)
                        {
                            try
                            {
                                IFile logo = controlFs.OpenFile($"/icon_AmericanEnglish.dat", OpenMode.Read);
                                GameIcon = new Gdk.Pixbuf(logo.AsStream(), 75, 75);
                            }
                            catch (FileNotFoundException)
                            {
                                if (Path.GetExtension(GamePath) == ".xci") { GameIcon = RyujinxXciIcon; }
                                else { GameIcon = RyujinxNspIcon; }
                            }
                        }
                    }

                    else if (Path.GetExtension(GamePath) == ".nso") { GameIcon = RyujinxNsoIcon; }

                    else if (Path.GetExtension(GamePath) == ".nro")
                    {
                        BinaryReader reader = new BinaryReader(file);

                        file.Seek(24, SeekOrigin.Begin);
                        int AssetOffset = reader.ReadInt32();

                        byte[] Read(long Position, int Size)
                        {
                            file.Seek(Position, SeekOrigin.Begin);
                            return reader.ReadBytes(Size);
                        }

                        if (Encoding.ASCII.GetString(Read(AssetOffset, 4)) == "ASET")
                        {
                            byte[] IconSectionInfo = Read(AssetOffset + 8, 0x10);

                            long IconOffset = BitConverter.ToInt64(IconSectionInfo, 0);
                            long IconSize   = BitConverter.ToInt64(IconSectionInfo, 8);

                            byte[] IconData = Read(AssetOffset + IconOffset, (int)IconSize);

                            GameIcon = new Gdk.Pixbuf(IconData, 75, 75);
                        }
                        else { GameIcon = RyujinxNroIcon; }
                    }

                    ApplicationData data = new ApplicationData()
                    {
                        Icon       = GameIcon,
                        GameName   = TitleName,
                        TimePlayed = "",
                        LastPlayed = "",
                        FileSize   = (filesize < 1) ? (filesize * 1024).ToString("0.##") + "MB" : filesize.ToString("0.##") + "GB",
                        Path       = GamePath,
                    };

                    ApplicationLibraryData.Add(data);
                }
            }
        }
    }
}
