using LibHac;
using LibHac.Common;
using LibHac.Fs;
using LibHac.Fs.Shim;
using LibHac.FsSystem;
using LibHac.FsSystem.Save;
using LibHac.Ncm;
using Ryujinx.HLE;
using Ryujinx.HLE.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Ryujinx.Ui
{
    internal class Migration
    {
        private Switch Device { get; }

        public Migration(Switch device)
        {
            Device = device;
        }

        public void Migrate()
        {
            string basePath = Device.FileSystem.GetBasePath();
            string backupPath = Path.Combine(basePath, "Migration backup (Can delete if successful)");
            string backupUserSavePath = Path.Combine(backupPath, "nand/user/save");

            if (!IsMigrationNeeded(basePath))
                return;

            BackupSaves(basePath, backupPath);

            MigrateDirectories(basePath);

            MigrateSaves(Device.System.FsClient, backupUserSavePath);
        }

        private static bool IsMigrationNeeded(string basePath)
        {
            return !Directory.Exists(Path.Combine(basePath, "bis")) &&
                   !Directory.Exists(Path.Combine(basePath, "sdcard"));
        }

        private static void MigrateDirectories(string basePath)
        {
            RenameDirectory(Path.Combine(basePath, "nand"), Path.Combine(basePath, "bis"));
            RenameDirectory(Path.Combine(basePath, "sdmc"), Path.Combine(basePath, "sdcard"));
        }

        private static bool RenameDirectory(string oldDir, string newDir)
        {
            if (Directory.Exists(newDir))
                return false;

            if (!Directory.Exists(oldDir))
            {
                Directory.CreateDirectory(newDir);

                return true;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(newDir));
            Directory.Move(oldDir, newDir);

            return true;
        }

        private static void BackupSaves(string basePath, string backupPath)
        {
            Directory.CreateDirectory(backupPath);

            string userSaveDir = Path.Combine(basePath, "nand/user/save");
            string backupUserSaveDir = Path.Combine(backupPath, "nand/user/save");

            if (Directory.Exists(userSaveDir))
            {
                RenameDirectory(userSaveDir, backupUserSaveDir);
            }

            string systemSaveDir = Path.Combine(basePath, "nand/system/save");
            string backupSystemSaveDir = Path.Combine(backupPath, "nand/system/save");

            if (Directory.Exists(systemSaveDir))
            {
                RenameDirectory(systemSaveDir, backupSystemSaveDir);
            }
        }

        private static void MigrateSaves(FileSystemClient fsClient, string rootSaveDir)
        {
            if (!Directory.Exists(rootSaveDir))
            {
                return;
            }

            SaveFinder finder = new SaveFinder();
            finder.FindSaves(rootSaveDir);

            foreach (SaveToMigrate save in finder.Saves)
            {
                Result migrateResult = MigrateSave(fsClient, save);

                if (migrateResult.IsFailure())
                {
                    throw new HorizonResultException(migrateResult, $"Error migrating save {save.Path}");
                }
            }
        }

        private static Result MigrateSave(FileSystemClient fs, SaveToMigrate save)
        {
            SaveDataAttribute key = save.Attribute;

            Result result = fs.CreateSaveData(key.TitleId, key.UserId, key.TitleId, 0, 0, 0);
            if (result.IsFailure()) return result;

            bool isOldMounted = false;
            bool isNewMounted = false;

            try
            {
                result = fs.Register("OldSave".ToU8Span(), new LocalFileSystem(save.Path));
                if (result.IsFailure()) return result;

                isOldMounted = true;

                result = fs.MountSaveData("NewSave".ToU8Span(), key.TitleId, key.UserId);
                if (result.IsFailure()) return result;

                isNewMounted = true;

                result = fs.CopyDirectory("OldSave:/", "NewSave:/");
                if (result.IsFailure()) return result;

                result = fs.Commit("NewSave");
            }
            finally
            {
                if (isOldMounted)
                {
                    fs.Unmount("OldSave");
                }

                if (isNewMounted)
                {
                    fs.Unmount("NewSave");
                }
            }

            return result;
        }

        private class SaveFinder
        {
            public List<SaveToMigrate> Saves { get; } = new List<SaveToMigrate>();

            public void FindSaves(string rootPath)
            {
                foreach (string subDir in Directory.EnumerateDirectories(rootPath))
                {
                    if (TryGetUInt64(subDir, out ulong saveDataId))
                    {
                        SearchSaveId(subDir, saveDataId);
                    }
                }
            }

            private void SearchSaveId(string path, ulong saveDataId)
            {
                foreach (string subDir in Directory.EnumerateDirectories(path))
                {
                    if (TryGetUserId(subDir, out UserId userId))
                    {
                        SearchUser(subDir, saveDataId, userId);
                    }
                }
            }

            private void SearchUser(string path, ulong saveDataId, UserId userId)
            {
                foreach (string subDir in Directory.EnumerateDirectories(path))
                {
                    if (TryGetUInt64(subDir, out ulong titleId) && TryGetDataPath(subDir, out string dataPath))
                    {
                        SaveDataAttribute attribute = new SaveDataAttribute
                        {
                            Type = SaveDataType.SaveData,
                            UserId = userId,
                            TitleId = new TitleId(titleId)
                        };

                        SaveToMigrate save = new SaveToMigrate(dataPath, attribute);

                        Saves.Add(save);
                    }
                }
            }

            private static bool TryGetDataPath(string path, out string dataPath)
            {
                string committedPath = Path.Combine(path, "0");
                string workingPath = Path.Combine(path, "1");

                if (Directory.Exists(committedPath) && Directory.EnumerateFileSystemEntries(committedPath).Any())
                {
                    dataPath = committedPath;
                    return true;
                }

                if (Directory.Exists(workingPath) && Directory.EnumerateFileSystemEntries(workingPath).Any())
                {
                    dataPath = workingPath;
                    return true;
                }

                dataPath = default;
                return false;
            }

            private static bool TryGetUInt64(string path, out ulong converted)
            {
                string name = Path.GetFileName(path);

                if (name.Length == 16)
                {
                    try
                    {
                        converted = Convert.ToUInt64(name, 16);
                        return true;
                    }
                    catch { }
                }

                converted = default;
                return false;
            }

            private static bool TryGetUserId(string path, out UserId userId)
            {
                string name = Path.GetFileName(path);

                if (name.Length == 32)
                {
                    try
                    {
                        UInt128 id = new UInt128(name);

                        userId = Unsafe.As<UInt128, UserId>(ref id);
                        return true;
                    }
                    catch { }
                }

                userId = default;
                return false;
            }
        }

        private class SaveToMigrate
        {
            public string Path { get; }
            public SaveDataAttribute Attribute { get; }

            public SaveToMigrate(string path, SaveDataAttribute attribute)
            {
                Path = path;
                Attribute = attribute;
            }
        }
    }
}
