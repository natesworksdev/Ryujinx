using Ryujinx.Common.Logging;
using System;
using System.IO;

namespace Ryujinx.Common.Configuration
{
    public static class AppDataManager
    {
        private static readonly string _defaultBaseDirPath;

        private static string _baseDirPath = null;
        private static string _gamesDirPath;
        private static string _profilesDirPath;
        private static string _keysDirPath;

        private const string DefaultBaseDir = "Ryujinx";
        public const string DefaultNandDir = "bis";
        public const string DefaultSdcardDir = "sdcard";
        private const string DefaultModsDir = "mods";

        // GamesDir, ProfilesDir and KeysDir are always part of Base Directory
        private const string GamesDir = "games";
        private const string ProfilesDir = "profiles";
        private const string KeysDir = "system";

        // TODO: Actually implement these into VFS
        public static string CustomNandPath { get; set; }
        public static string CustomSdCardPath { get; set; }
        public static string CustomModsPath { get; set; }


        public static bool IsCustomBasePath { get; private set; }

        static AppDataManager()
        {
            _defaultBaseDirPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), DefaultBaseDir);
        }

        public static void Initialize(string baseDirPath)
        {
            _baseDirPath = _defaultBaseDirPath;

            if (baseDirPath != null && baseDirPath != _defaultBaseDirPath)
            {
                if (!Directory.Exists(baseDirPath))
                {
                    Logger.Error?.Print(LogClass.Application, $"Custom Data Directory '{baseDirPath}' does not exist. Using defaults...");
                }
                else
                {
                    _baseDirPath = baseDirPath;
                    IsCustomBasePath = true;
                }
            }

            SetupBasePaths();
        }

        private static void SetupBasePaths()
        {
            Directory.CreateDirectory(_baseDirPath);
            Directory.CreateDirectory(_gamesDirPath = Path.Combine(_baseDirPath, GamesDir));
            Directory.CreateDirectory(_profilesDirPath = Path.Combine(_baseDirPath, ProfilesDir));
            Directory.CreateDirectory(_keysDirPath = Path.Combine(_baseDirPath, KeysDir));
        }

        public static string GetBasePath() => _baseDirPath;
        public static string GetGamesPath() => _gamesDirPath;
        public static string GetProfilesPath() => _profilesDirPath;
        public static string GetKeysPath() => _keysDirPath;

        public static string GetAlternateKeysPath() => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".switch");

        public static string GetNandPath() => Directory.CreateDirectory(Path.Combine(_baseDirPath, DefaultNandDir)).FullName;
        public static string GetSdCardPath() => Directory.CreateDirectory(Path.Combine(_baseDirPath, DefaultSdcardDir)).FullName;
        public static string GetModsPath() => Directory.CreateDirectory(Path.Combine(_baseDirPath, DefaultModsDir)).FullName;
    }
}