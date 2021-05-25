using LibHac;
using LibHac.Common;
using LibHac.Common.Keys;
using LibHac.Fs;
using LibHac.Fs.Fsa;
using LibHac.FsSrv;
using LibHac.FsSystem;
using LibHac.Spl;
using Ryujinx.Common.Configuration;
using Ryujinx.HLE.FileSystem.Content;
using Ryujinx.HLE.HOS;
using System;
using System.IO;

namespace Ryujinx.HLE.FileSystem
{
    public class VirtualFileSystem : IDisposable
    {
        public const string NandPath   = AppDataManager.DefaultNandDir;
        public const string SdCardPath = AppDataManager.DefaultSdcardDir;

        public static string SafeNandPath   = Path.Combine(NandPath, "safe");
        public static string SystemNandPath = Path.Combine(NandPath, "system");
        public static string UserNandPath   = Path.Combine(NandPath, "user");
        
        private static bool _isInitialized = false;

        public KeySet           KeySet   { get; private set; }
        public EmulatedGameCard GameCard { get; private set; }
        public EmulatedSdCard   SdCard   { get; private set; }

        public ModLoader ModLoader { get; private set; }

        private VirtualFileSystem()
        {
            ReloadKeySet();
            ModLoader = new ModLoader(); // Should only be created once
        }

        public Stream RomFs { get; private set; }

        public void LoadRomFs(string fileName)
        {
            RomFs = new FileStream(fileName, FileMode.Open, FileAccess.Read);
        }

        public void SetRomFs(Stream romfsStream)
        {
            RomFs?.Close();
            RomFs = romfsStream;
        }

        public string GetFullPath(string basePath, string fileName)
        {
            if (fileName.StartsWith("//"))
            {
                fileName = fileName.Substring(2);
            }
            else if (fileName.StartsWith('/'))
            {
                fileName = fileName.Substring(1);
            }
            else
            {
                return null;
            }

            string fullPath = Path.GetFullPath(Path.Combine(basePath, fileName));

            if (!fullPath.StartsWith(GetBasePath()))
            {
                return null;
            }

            return fullPath;
        }

        internal string GetBasePath() => AppDataManager.BaseDirPath;
        internal string GetSdCardPath() => MakeFullPath(SdCardPath);
        public string GetNandPath() => MakeFullPath(NandPath);

        public string GetFullPartitionPath(string partitionPath)
        {
            return MakeFullPath(partitionPath);
        }

        public string SwitchPathToSystemPath(string switchPath)
        {
            string[] parts = switchPath.Split(":");

            if (parts.Length != 2)
            {
                return null;
            }

            return GetFullPath(MakeFullPath(parts[0]), parts[1]);
        }

        public string SystemPathToSwitchPath(string systemPath)
        {
            string baseSystemPath = GetBasePath() + Path.DirectorySeparatorChar;

            if (systemPath.StartsWith(baseSystemPath))
            {
                string rawPath              = systemPath.Replace(baseSystemPath, "");
                int    firstSeparatorOffset = rawPath.IndexOf(Path.DirectorySeparatorChar);

                if (firstSeparatorOffset == -1)
                {
                    return $"{rawPath}:/";
                }

                string basePath = rawPath.Substring(0, firstSeparatorOffset);
                string fileName = rawPath.Substring(firstSeparatorOffset + 1);

                return $"{basePath}:/{fileName}";
            }
            return null;
        }

        private string MakeFullPath(string path, bool isDirectory = true)
        {
            // Handles Common Switch Content Paths
            switch (path)
            {
                case ContentPath.SdCard:
                case "@Sdcard":
                    path = SdCardPath;
                    break;
                case ContentPath.User:
                    path = UserNandPath;
                    break;
                case ContentPath.System:
                    path = SystemNandPath;
                    break;
                case ContentPath.SdCardContent:
                    path = Path.Combine(SdCardPath, "Nintendo", "Contents");
                    break;
                case ContentPath.UserContent:
                    path = Path.Combine(UserNandPath, "Contents");
                    break;
                case ContentPath.SystemContent:
                    path = Path.Combine(SystemNandPath, "Contents");
                    break;
            }

            string fullPath = Path.Combine(GetBasePath(), path);

            if (isDirectory)
            {
                if (!Directory.Exists(fullPath))
                {
                    Directory.CreateDirectory(fullPath);
                }
            }

            return fullPath;
        }

        public DriveInfo GetDrive()
        {
            return new DriveInfo(Path.GetPathRoot(GetBasePath()));
        }

        public void InitializeFsServer(LibHac.Horizon horizon, out HorizonClient fsServerClient)
        {
            LocalFileSystem serverBaseFs = new LocalFileSystem(GetBasePath());

            fsServerClient = horizon.CreatePrivilegedHorizonClient();
            var fsServer = new FileSystemServer(fsServerClient);

            DefaultFsServerObjects fsServerObjects = DefaultFsServerObjects.GetDefaultEmulatedCreators(serverBaseFs, KeySet, fsServer);

            GameCard = fsServerObjects.GameCard;
            SdCard = fsServerObjects.SdCard;

            SdCard.SetSdCardInsertionStatus(true);

            var fsServerConfig = new FileSystemServerConfig
            {
                DeviceOperator = fsServerObjects.DeviceOperator,
                ExternalKeySet = KeySet.ExternalKeySet,
                FsCreators = fsServerObjects.FsCreators
            };

            FileSystemServerInitializer.InitializeWithConfig(fsServerClient, fsServer, fsServerConfig);
        }

        public void ReloadKeySet()
        {
            KeySet ??= KeySet.CreateDefaultKeySet();

            string keyFile        = null;
            string titleKeyFile   = null;
            string consoleKeyFile = null;

            if (AppDataManager.Mode == AppDataManager.LaunchMode.UserProfile)
            {
                LoadSetAtPath(AppDataManager.KeysDirPathUser);
            }

            LoadSetAtPath(AppDataManager.KeysDirPath);

            void LoadSetAtPath(string basePath)
            {
                string localKeyFile        = Path.Combine(basePath, "prod.keys");
                string localTitleKeyFile   = Path.Combine(basePath, "title.keys");
                string localConsoleKeyFile = Path.Combine(basePath, "console.keys");

                if (File.Exists(localKeyFile))
                {
                    keyFile = localKeyFile;
                }

                if (File.Exists(localTitleKeyFile))
                {
                    titleKeyFile = localTitleKeyFile;
                }

                if (File.Exists(localConsoleKeyFile))
                {
                    consoleKeyFile = localConsoleKeyFile;
                }
            }

            ExternalKeyReader.ReadKeyFile(KeySet, keyFile, titleKeyFile, consoleKeyFile, null);
        }

        public void ImportTickets(IFileSystem fs)
        {
            foreach (DirectoryEntryEx ticketEntry in fs.EnumerateEntries("/", "*.tik"))
            {
                Result result = fs.OpenFile(out IFile ticketFile, ticketEntry.FullPath.ToU8Span(), OpenMode.Read);

                if (result.IsSuccess())
                {
                    Ticket ticket = new Ticket(ticketFile.AsStream());

                    if (ticket.TitleKeyType == TitleKeyType.Common)
                    {
                        KeySet.ExternalKeySet.Add(new RightsId(ticket.RightsId), new AccessKey(ticket.GetTitleKey(KeySet)));
                    }
                }
            }
        }

        public void Unload()
        {
            RomFs?.Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Unload();
            }
        }

        public static VirtualFileSystem CreateInstance()
        {
            if (_isInitialized)
            {
                throw new InvalidOperationException("VirtualFileSystem can only be instantiated once!");
            }

            _isInitialized = true;

            return new VirtualFileSystem();
        }
    }
}