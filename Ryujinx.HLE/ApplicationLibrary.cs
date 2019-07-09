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
        private static Keyset KeySet;
        private static HLE.HOS.SystemState.TitleLanguage DesiredTitleLanguage;

        public static byte[] RyujinxNspIcon { get; private set; }
        public static byte[] RyujinxXciIcon { get; private set; }
        public static byte[] RyujinxNcaIcon { get; private set; }
        public static byte[] RyujinxNroIcon { get; private set; }
        public static byte[] RyujinxNsoIcon { get; private set; }

        public static List<ApplicationData> ApplicationLibraryData { get; private set; }

        public struct ApplicationData
        {
            public byte[] Icon;
            public string TitleName;
            public string TitleId;
            public string Developer;
            public string Version;
            public string TimePlayed;
            public string LastPlayed;
            public string FileExt;
            public string FileSize;
            public string Path;
        }

        public static void Init(List<string> AppDirs, Keyset keySet, HLE.HOS.SystemState.TitleLanguage desiredTitleLanguage)
        {
            KeySet                = keySet;
            DesiredTitleLanguage  = desiredTitleLanguage;

            // Loads the default application Icons
            using (Stream resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Ryujinx.HLE.ryujinxNSPIcon.png"))
            {
                using (MemoryStream ms = new MemoryStream()) { resourceStream.CopyTo(ms); RyujinxNspIcon = ms.ToArray(); }
            }
            using (Stream resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Ryujinx.HLE.ryujinxXCIIcon.png"))
            {
                using (MemoryStream ms = new MemoryStream()) { resourceStream.CopyTo(ms); RyujinxXciIcon = ms.ToArray(); }
            }
            using (Stream resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Ryujinx.HLE.ryujinxNCAIcon.png"))
            {
                using (MemoryStream ms = new MemoryStream()) { resourceStream.CopyTo(ms); RyujinxNcaIcon = ms.ToArray(); }
            }
            using (Stream resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Ryujinx.HLE.ryujinxNROIcon.png"))
            {
                using(MemoryStream ms = new MemoryStream()) { resourceStream.CopyTo(ms); RyujinxNroIcon = ms.ToArray(); }
            }
            using (Stream resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Ryujinx.HLE.ryujinxNSOIcon.png"))
            {
                using (MemoryStream ms = new MemoryStream()) { resourceStream.CopyTo(ms); RyujinxNsoIcon = ms.ToArray(); }
            }

            // Builds the applications list with paths to found applications
            List<string> applications = new List<string>();
            foreach (string appDir in AppDirs)
            {
                if (Directory.Exists(appDir) == false)
                {
                    Logger.PrintError(LogClass.Application, $"The \"game_dirs\" section in \"Config.json\" contains an invalid directory: \"{appDir}\"");

                    continue;
                }

                DirectoryInfo AppDirInfo = new DirectoryInfo(appDir);
                foreach (var App in AppDirInfo.GetFiles())
                {
                    if ((Path.GetExtension(App.ToString()) == ".xci")  ||
                        (Path.GetExtension(App.ToString()) == ".nca")  ||
                        (Path.GetExtension(App.ToString()) == ".nsp")  ||
                        (Path.GetExtension(App.ToString()) == ".pfs0") ||
                        (Path.GetExtension(App.ToString()) == ".nro")  ||
                        (Path.GetExtension(App.ToString()) == ".nso"))
                    {
                        applications.Add(App.ToString());
                    }
                }
            }

            // Loops through applications list, creating a struct for each application and then adding the struct to a list of structs
            ApplicationLibraryData = new List<ApplicationData>();
            foreach (string applicationPath in applications)
            {
                double filesize         = new FileInfo(applicationPath).Length * 0.000000000931;
                string titleName        = null;
                string titleId          = null;
                string developer        = null;
                string version          = null;
                byte[] applicationIcon  = null;

                using (FileStream file = new FileStream(applicationPath, FileMode.Open, FileAccess.Read))
                {
                    if ((Path.GetExtension(applicationPath) == ".nsp") || (Path.GetExtension(applicationPath) == ".pfs0") || (Path.GetExtension(applicationPath) == ".xci"))
                    {
                        try
                        {
                            IFileSystem controlFs = null;

                            // Store the ControlFS in variable called controlFs
                            if (Path.GetExtension(applicationPath) == ".xci")
                            {
                                Xci xci = new Xci(KeySet, file.AsStorage());
                                controlFs = GetControlFs(xci.OpenPartition(XciPartitionType.Secure));
                            }
                            else { controlFs = GetControlFs(new PartitionFileSystem(file.AsStorage())); }

                            // Creates NACP class from the NACP file
                            IFile controlNacp = controlFs.OpenFile("/control.nacp", OpenMode.Read);
                            Nacp controlData  = new Nacp(controlNacp.AsStream());

                            // Get the title name, title ID, developer name and version number from the NACP
                            version = controlData.DisplayVersion;

                            titleName = controlData.Descriptions[(int)DesiredTitleLanguage].Title;
                            if (string.IsNullOrWhiteSpace(titleName))
                            {
                                titleName = controlData.Descriptions.ToList().Find(x => !string.IsNullOrWhiteSpace(x.Title)).Title;
                            }

                            titleId = controlData.PresenceGroupId.ToString("x16");
                            if (string.IsNullOrWhiteSpace(titleId)) { titleId = controlData.SaveDataOwnerId.ToString("x16"); }
                            if (string.IsNullOrWhiteSpace(titleId)) { titleId = (controlData.AddOnContentBaseId - 0x1000).ToString("x16"); }

                            developer = controlData.Descriptions[(int)DesiredTitleLanguage].Developer;
                            if (string.IsNullOrWhiteSpace(developer))
                            {
                                developer = controlData.Descriptions.ToList().Find(x => !string.IsNullOrWhiteSpace(x.Developer)).Developer;
                            }

                            // Read the icon from the ControlFS and store it as a byte array
                            try
                            {
                                IFile icon = controlFs.OpenFile($"/icon_{DesiredTitleLanguage}.dat", OpenMode.Read);
                                using (MemoryStream ms = new MemoryStream())
                                {
                                    icon.AsStream().CopyTo(ms);
                                    applicationIcon = ms.ToArray();
                                }
                            }
                            catch (FileNotFoundException)
                            {
                                IDirectory controlDir = controlFs.OpenDirectory("./", OpenDirectoryMode.All);
                                foreach (DirectoryEntry entry in controlDir.Read())
                                {
                                    if (entry.Name == "control.nacp") { continue; }

                                    IFile icon = controlFs.OpenFile(entry.FullPath, OpenMode.Read);
                                    using (MemoryStream ms = new MemoryStream())
                                    {
                                        icon.AsStream().CopyTo(ms);
                                        applicationIcon = ms.ToArray();
                                    }

                                    if (applicationIcon != null) { break; }
                                }

                                if (applicationIcon == null)
                                {
                                    if (Path.GetExtension(applicationPath) == ".xci") { applicationIcon = RyujinxXciIcon; }
                                    else { applicationIcon = RyujinxNspIcon; }
                                }
                            }
                        }
                        catch (MissingKeyException exception)
                        {
                            titleName = "Unknown";
                            titleId   = "Unknown";
                            developer = "Unknown";
                            version   = "?";

                            if (Path.GetExtension(applicationPath) == ".xci") { applicationIcon = RyujinxXciIcon; }
                            else { applicationIcon = RyujinxNspIcon; }

                            Logger.PrintError(LogClass.Application, $"Your key set is missing a key with the name: {exception.Name}");
                        }
                        catch (InvalidDataException)
                        {
                            titleName = "Unknown";
                            titleId   = "Unknown";
                            developer = "Unknown";
                            version   = "?";

                            if (Path.GetExtension(applicationPath) == ".xci") { applicationIcon = RyujinxXciIcon; }
                            else { applicationIcon = RyujinxNspIcon; }

                            Logger.PrintError(LogClass.Application, $"Unable to decrypt NCA header. The header key must be incorrect. File: {applicationPath}");
                        }
                    }

                    else if (Path.GetExtension(applicationPath) == ".nro")
                    {
                        BinaryReader reader = new BinaryReader(file);

                        byte[] Read(long Position, int Size)
                        {
                            file.Seek(Position, SeekOrigin.Begin);
                            return reader.ReadBytes(Size);
                        }

                        file.Seek(24, SeekOrigin.Begin);
                        int AssetOffset = reader.ReadInt32();

                        if (Encoding.ASCII.GetString(Read(AssetOffset, 4)) == "ASET")
                        {
                            byte[] IconSectionInfo = Read(AssetOffset + 8, 0x10);

                            long iconOffset = BitConverter.ToInt64(IconSectionInfo, 0);
                            long iconSize   = BitConverter.ToInt64(IconSectionInfo, 8);

                            ulong nacpOffset = reader.ReadUInt64();
                            ulong nacpSize   = reader.ReadUInt64();

                            // Reads and stores game icon as byte array
                            applicationIcon = Read(AssetOffset + iconOffset, (int)iconSize);

                            // Creates memory stream out of byte array which is the NACP
                            using (MemoryStream stream = new MemoryStream(Read(AssetOffset + (int)nacpOffset, (int)nacpSize)))
                            {
                                // Creates NACP class from the memory stream
                                Nacp controlData = new Nacp(stream);

                                // Get the title name, title ID, developer name and version number from the NACP
                                version = controlData.DisplayVersion;

                                titleName = controlData.Descriptions[(int)DesiredTitleLanguage].Title;
                                if (string.IsNullOrWhiteSpace(titleName))
                                {
                                    titleName = controlData.Descriptions.ToList().Find(x => !string.IsNullOrWhiteSpace(x.Title)).Title;
                                }

                                titleId = controlData.PresenceGroupId.ToString("x16");
                                if (string.IsNullOrWhiteSpace(titleId)) { titleId = controlData.SaveDataOwnerId.ToString("x16"); }
                                if (string.IsNullOrWhiteSpace(titleId)) { titleId = (controlData.AddOnContentBaseId - 0x1000).ToString("x16"); }

                                developer = controlData.Descriptions[(int)DesiredTitleLanguage].Developer;
                                if (string.IsNullOrWhiteSpace(developer))
                                {
                                    developer = controlData.Descriptions.ToList().Find(x => !string.IsNullOrWhiteSpace(x.Developer)).Developer;
                                }
                            }
                        }
                        else { applicationIcon = RyujinxNroIcon; titleName = "Application"; titleId = "0000000000000000"; developer = "Unknown"; version = "?"; }
                    }

                    // If its an NCA or NSO we just set defaults
                    else if ((Path.GetExtension(applicationPath) == ".nca") || (Path.GetExtension(applicationPath) == ".nso"))
                    {
                             if (Path.GetExtension(applicationPath) == ".nca") { applicationIcon = RyujinxNcaIcon; }
                        else if (Path.GetExtension(applicationPath) == ".nso") { applicationIcon = RyujinxNsoIcon; }

                        StringBuilder titlename = new StringBuilder();
                        titlename.Append(Path.GetFileName(applicationPath));
                        titlename.Remove(Path.GetFileName(applicationPath).Length - Path.GetExtension(applicationPath).Length, Path.GetExtension(applicationPath).Length);

                        titleName = titlename.ToString();
                        titleId = "0000000000000000";
                        version = "?";
                        developer = "Unknown";
                    }
                }

                string[] playedData = GetPlayedData(titleId, "00000000000000000000000000000001");

                ApplicationData data = new ApplicationData()
                {
                    Icon       = applicationIcon,
                    TitleName  = titleName,
                    TitleId    = titleId,
                    Developer  = developer,
                    Version    = version,
                    TimePlayed = playedData[0],
                    LastPlayed = playedData[1],
                    FileExt    = Path.GetExtension(applicationPath).ToUpper().Remove(0 ,1),
                    FileSize   = (filesize < 1) ? (filesize * 1024).ToString("0.##") + "MB" : filesize.ToString("0.##") + "GB",
                    Path       = applicationPath,
                };

                ApplicationLibraryData.Add(data);
            }
        }

        private static IFileSystem GetControlFs(PartitionFileSystem Pfs)
        {
            Nca controlNca = null;

            // Add keys to keyset if needed
            foreach (DirectoryEntry ticketEntry in Pfs.EnumerateEntries("*.tik"))
            {
                Ticket ticket = new Ticket(Pfs.OpenFile(ticketEntry.FullPath, OpenMode.Read).AsStream());

                if (!KeySet.TitleKeys.ContainsKey(ticket.RightsId))
                {
                    KeySet.TitleKeys.Add(ticket.RightsId, ticket.GetTitleKey(KeySet));
                }
            }

            // Find the Control NCA and store it in variable called controlNca
            foreach (DirectoryEntry fileEntry in Pfs.EnumerateEntries("*.nca"))
            {
                Nca nca = new Nca(KeySet, Pfs.OpenFile(fileEntry.FullPath, OpenMode.Read).AsStorage());
                if (nca.Header.ContentType == ContentType.Control)
                {
                    controlNca = nca;
                }
            }

            // Return the ControlFS
            return controlNca.OpenFileSystem(NcaSectionType.Data, IntegrityCheckLevel.None);
        }

        private static string[] GetPlayedData(string TitleId, string UserId)
        {
            try
            {
                string[] playedData = new string[2];
                string appdataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string savePath = Path.Combine(appdataPath, "RyuFs", "nand", "user", "save", "0000000000000000", UserId, TitleId);

                if (File.Exists(Path.Combine(savePath, "TimePlayed.dat")) == false)
                {
                    Directory.CreateDirectory(savePath);
                    using (FileStream file = File.OpenWrite(Path.Combine(savePath, "TimePlayed.dat"))) { file.Write(Encoding.ASCII.GetBytes("0")); }
                }
                using (FileStream fs = File.OpenRead(Path.Combine(savePath, "TimePlayed.dat")))
                {
                    using (StreamReader sr = new StreamReader(fs))
                    {
                        float timePlayed = float.Parse(sr.ReadLine());

                        if      (timePlayed <= 60.0)    { playedData[0] = $"{timePlayed}s"; }
                        else if (timePlayed <= 3600.0)  { playedData[0] = $"{Math.Round(timePlayed / 60   , 2, MidpointRounding.AwayFromZero)} mins"; }
                        else if (timePlayed <= 86400.0) { playedData[0] = $"{Math.Round(timePlayed / 3600 , 2, MidpointRounding.AwayFromZero)} hrs";  }
                        else                            { playedData[0] = $"{Math.Round(timePlayed / 86400, 2, MidpointRounding.AwayFromZero)} days"; }
                    }
                }

                if (File.Exists(Path.Combine(savePath, "LastPlayed.dat")) == false)
                {
                    Directory.CreateDirectory(savePath);
                    using (FileStream file = File.OpenWrite(Path.Combine(savePath, "LastPlayed.dat"))) { file.Write(Encoding.ASCII.GetBytes("Never")); }
                }
                using (FileStream fs = File.OpenRead(Path.Combine(savePath, "LastPlayed.dat")))
                {
                    using (StreamReader sr = new StreamReader(fs))
                    {
                        playedData[1] = sr.ReadLine();
                    }
                }

                return playedData;
            }
            catch { return new string[] { "Unknown", "Unknown" }; }
        }
    }
}
