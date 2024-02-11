using LibHac;
using LibHac.Account;
using LibHac.Common;
using LibHac.Fs;
using LibHac.Fs.Shim;
using LibHac.Ns;
using Ryujinx.Ava.UI.Windows;
using Ryujinx.Common.Logging;
using Ryujinx.Ui.Common.Helper;
using Ryujinx.Ui.Common.SaveManager;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using LibHacUserId = LibHac.Fs.UserId;
using Path = System.IO.Path;

namespace Ryujinx.Ava.Common.SaveManager
{
    public class SaveManager
    {
        // UI Metadata
        public event EventHandler<LoadingBarEventArgs> BackupProgressUpdated;
        public event EventHandler<ImportSaveEventArgs> BackupImportSave;
        private readonly LoadingBarEventArgs _loadingEventArgs = new();

        private readonly HorizonClient _horizonClient;

        public SaveManager(HorizonClient hzClient)
        {
            _horizonClient = hzClient;
        }

        #region Backup
        public async Task<bool> BackupUserSaveDataToZip(LibHacUserId userId, Uri savePath)
        {
            var userSaves = GetUserSaveData(userId).ToArray();
            if (userSaves.Length == 0)
            {
                Logger.Warning?.Print(LogClass.Application, "No save data found");
                return false;
            }

            _loadingEventArgs.Curr = 0;
            _loadingEventArgs.Max = userSaves.Length + 1; // Add one for metadata file
            BackupProgressUpdated?.Invoke(this, _loadingEventArgs);

            return await CreateOrReplaceZipFile(userSaves, savePath.LocalPath);
        }

        private IEnumerable<BackupSaveMeta> GetUserSaveData(LibHacUserId userId)
        {
            try
            {
                // Almost all games have user saves
                var userSaves = GetSaveData(userId, SaveDataType.Account)
                    .ToList();

                var deviceSaves = GetSaveData(default, SaveDataType.Device);
                userSaves.AddRange(deviceSaves);

                var bcatSaves = GetSaveData(default, SaveDataType.Bcat);
                userSaves.AddRange(bcatSaves);

                return userSaves;
            }
            catch (Exception ex)
            {
                Logger.Error?.Print(LogClass.Application, $"Failed to enumerate user save data - {ex.Message}");
            }

            return Enumerable.Empty<BackupSaveMeta>();
        }

        private IEnumerable<BackupSaveMeta> GetSaveData(LibHacUserId userId, SaveDataType saveType)
        {
            var saveDataFilter = SaveDataFilter.Make(
                programId: default,
                saveType: saveType,
                userId: userId,
                saveDataId: default,
                index: default);

            using var saveDataIterator = new UniqueRef<SaveDataIterator>();

            _horizonClient.Fs
                .OpenSaveDataIterator(ref saveDataIterator.Ref, SaveDataSpaceId.User, in saveDataFilter)
                .ThrowIfFailure();

            Span<SaveDataInfo> saveDataInfo = stackalloc SaveDataInfo[10];
            List<BackupSaveMeta> saves = new();

            do
            {
                saveDataIterator.Get
                    .ReadSaveDataInfo(out long readCount, saveDataInfo)
                    .ThrowIfFailure();

                if (readCount == 0)
                {
                    break;
                }

                for (int i = 0; i < readCount; i++)
                {
                    if (saveDataInfo[i].ProgramId.Value != 0)
                    {
                        saves.Add(new BackupSaveMeta
                        {
                            SaveDataId = saveDataInfo[i].SaveDataId,
                            Type = saveDataInfo[i].Type,
                            TitleId = saveDataInfo[i].ProgramId,
                        });
                    }
                }
            } while (true);

            return saves;
        }

        private async static Task<bool> CreateOrReplaceZipFile(IEnumerable<BackupSaveMeta> userSaves, string zipPath)
        {
            await using FileStream zipFileSteam = new(zipPath, FileMode.Create, FileAccess.ReadWrite);
            using ZipArchive zipArchive = new(zipFileSteam, ZipArchiveMode.Create);

            foreach (var save in userSaves)
            {
                // Find the most recent version of the data, there is a committed (0) and working (1) paths directory
                var saveRootPath = ApplicationHelper.FindValidSaveDir(save.SaveDataId);

                // The actual title in the name would be nice but titleId is more reliable
                // /[titleId]/[saveType]
                var copyDestPath = Path.Combine(save.TitleId.Value.ToString(), save.Type.ToString());

                foreach (string filename in Directory.EnumerateFileSystemEntries(saveRootPath, "*", SearchOption.AllDirectories))
                {
                    var attributes = File.GetAttributes(filename);
                    if (attributes.HasFlag(FileAttributes.Hidden | FileAttributes.System | FileAttributes.Directory))
                    {
                        continue;
                    }

                    try
                    {
                        await using FileStream sourceFile = new(filename, FileMode.Open, FileAccess.Read);

                        var filePath = Path.Join(copyDestPath, Path.GetRelativePath(saveRootPath, filename));

                        ZipArchiveEntry entry = zipArchive.CreateEntry(filePath, CompressionLevel.SmallestSize);

                        await using StreamWriter writer = new(entry.Open());

                        await sourceFile.CopyToAsync(writer.BaseStream);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error?.Print(LogClass.Application, $"Failed to zip file: {ex.Message}");
                    }
                }
            }

            return true;
        }
        #endregion

        #region Restore
        public async Task<bool> RestoreUserSaveDataFromZip(LibHacUserId userId, string zipPath)
        {
            var titleDirectories = new List<RestoreSaveMeta>();

            try
            {
                await using FileStream zipFileSteam = new(zipPath, FileMode.Open, FileAccess.Read);
                using ZipArchive zipArchive = new(zipFileSteam, ZipArchiveMode.Read);

                // Directories do not always have entries
                foreach (var entry in zipArchive.Entries)
                {
                    var pathByDepth = entry.FullName.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);

                    // Depth 1 is title IDs, Depth 2 the save type
                    if (pathByDepth.Length < 2)
                    {
                        continue;
                    }

                    var parentDirectoryName = pathByDepth[0];
                    var directoryName = pathByDepth[1];

                    if (!ulong.TryParse(parentDirectoryName, out var titleId))
                    {
                        continue;
                    }

                    if (Enum.TryParse<SaveDataType>(directoryName, out var saveType))
                    {
                        var meta = new RestoreSaveMeta { TitleId = titleId, SaveType = saveType };

                        if (!titleDirectories.Contains(meta))
                        {
                            titleDirectories.Add(meta);
                        }
                    }
                }

                try
                {
                    var mappings = new List<MetaToLocalMap?>();

                    // Find the saveId for each titleId and migrate it. Use cache to avoid duplicate lookups of known titleId
                    foreach (var importMeta in titleDirectories)
                    {
                        if (PrepareLocalSaveData(importMeta, userId, out string localDir))
                        {
                            mappings.Add(new MetaToLocalMap
                            {
                                RelativeDir = Path.Join(importMeta.TitleId.ToString(), importMeta.SaveType.ToString()),
                                LocalDir = localDir
                            });
                        }
                    }

                    foreach (var entry in zipArchive.Entries)
                    {
                        if (entry.FullName[^1] == Path.DirectorySeparatorChar)
                        {
                            continue;
                        }

                        var optional = mappings.FirstOrDefault(x => entry.FullName.Contains(x.Value.RelativeDir), null);

                        if (!optional.HasValue)
                        {
                            continue;
                        }

                        var map = optional.Value;
                        var localPath = Path.Join(map.LocalDir, Path.GetRelativePath(map.RelativeDir, entry.FullName));
                        entry.ExtractToFile(localPath, true);
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    Logger.Error?.Print(LogClass.Application, $"Failed to import save data - {ex.Message}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                var error = $"Failed to load save backup zip: {ex.Message}";
                Logger.Error?.Print(LogClass.Application, error);

                return false;
            }
        }

        private bool PrepareLocalSaveData(RestoreSaveMeta meta, UserId userId, out String? path)
        {
            path = null;
            // Lookup the saveId based on title for the user we're importing too
            var saveDataFilter = SaveDataFilter.Make(meta.TitleId,
                meta.SaveType,
                meta.SaveType == SaveDataType.Account
                    ? userId
                    : default,
                saveDataId: default,
                index: default);

            var result = _horizonClient.Fs.FindSaveDataWithFilter(out var saveDataInfo, SaveDataSpaceId.User, in saveDataFilter);
            if (result.IsFailure())
            {
                if (ResultFs.TargetNotFound.Includes(result))
                {
                    Logger.Debug?.Print(LogClass.Application, $"Title {meta.TitleId} does not have existing {meta.SaveType} data");

                    // Try to create it and re-fetch it
                    TryGenerateSaveEntry(meta.TitleId, userId);
                    result = _horizonClient.Fs.FindSaveDataWithFilter(out saveDataInfo, SaveDataSpaceId.User, in saveDataFilter);

                    if (result.IsFailure())
                    {
                        return false;
                    }
                }
                else
                {
                    Logger.Warning?.Print(LogClass.Application, $"Title {meta.TitleId} does not have {meta.SaveType} data - ErrorCode: {result.ErrorCode}");
                    return false;
                }
            }

            // Find the most recent version of the data, there is a committed (0) and working (1) directory
            var userHostSavePath = ApplicationHelper.FindValidSaveDir(saveDataInfo.SaveDataId);
            if (string.IsNullOrWhiteSpace(userHostSavePath))
            {
                Logger.Warning?.Print(LogClass.Application, $"Unable to locate host save directory for {meta.TitleId}");
                return false;
            }

            path = userHostSavePath;
            return true;
        }

        private void TryGenerateSaveEntry(ulong titleId, UserId userId)
        {
            // Resolve from app data
            var titleIdHex = titleId.ToString("x16");
            var appData = MainWindow.MainWindowViewModel.Applications
                .FirstOrDefault(x => x.TitleId.Equals(titleIdHex, StringComparison.OrdinalIgnoreCase));
            if (appData is null)
            {
                Logger.Error?.Print(LogClass.Application, $"No application loaded with titleId {titleIdHex}");
                return;
            }

            ref ApplicationControlProperty control = ref appData.ControlHolder.Value;

            Logger.Info?.Print(LogClass.Application, $"Creating save directory for Title: [{titleId:x16}]");

            if (appData.ControlHolder.ByteSpan.IsZeros())
            {
                // If the current application doesn't have a loaded control property, create a dummy one
                // and set the save data sizes so a user save data will be created.
                control = ref new BlitStruct<ApplicationControlProperty>(1).Value;

                // The set sizes don't actually matter as long as they're non-zero because we use directory save data.
                control.UserAccountSaveDataSize = 0x4000;
                control.UserAccountSaveDataJournalSize = 0x4000;

                Logger.Warning?.Print(LogClass.Application, "No control file was found for this game. Using a dummy one instead. This may cause inaccuracies in some games.");
            }

            Uid user = new(userId.Id.High, userId.Id.Low);
            _horizonClient.Fs.EnsureApplicationSaveData(out _, new LibHac.Ncm.ApplicationId(titleId), in control, in user);
        }
        #endregion
    }
}
