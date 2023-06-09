using LibHac;
using LibHac.Common;
using LibHac.Fs;
using LibHac.Fs.Shim;
using LibHac.Ncm;
using Microsoft.IdentityModel.Tokens;
using Ryujinx.Ava.UI.ViewModels;
using Ryujinx.Common.Configuration;
using Ryujinx.Common.Logging;
using Ryujinx.HLE.HOS.Services.Account.Acc;
using Ryujinx.Ui.App.Common;
using Ryujinx.Ui.Common.Helper;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Path = System.IO.Path;

namespace Ryujinx.Ava.Common
{
    #region HelperClasses
    public readonly record struct BackupRequestOutcome
    {
        public bool DidFail { get; init; }
        public string Message { get; init; }
    }

    internal readonly record struct SaveMeta
    {
        public ulong SaveDataId { get; init; }
        public SaveDataType Type { get; init; }

        // TODO: needed?
        public LibHac.Fs.UserId UserId { get; init; }

        // Title Id
        public ProgramId ProgramId { get; init; }
    }

    internal readonly record struct UserFriendlySaveMetadata
    {
        public string UserId { get; init; }
        public string ProfileName { get; init; }
        public DateTime CreationTimeUtc { get; init; }
        public IEnumerable<UserFriendlyAppData> ApplicationMap { get; init; }
    }

    internal readonly record struct UserFriendlyAppData
    {
        public ulong TitleId { get; init; }
        public string Title { get; init; }
        public string TitleIdHex { get; init; }
    }

    [Flags]
    public enum SaveOptions
    {
        // save types to use
        SaveTypeAccount,
        SaveTypeBcat,
        SaveTypeDevice,
        SaveTypeAll = SaveTypeAccount | SaveTypeBcat | SaveTypeDevice,
        
        // Request Semantics
        SkipEmptyDirectories,
        FlattenSaveStructure,
        StopOnFailure,
        UseDateInName,

        Default = SaveTypeAll
    }
    #endregion

    public class BackupManager
    {
        // UI callbacks
        public event EventHandler<LoadingBarEventArgs> BackupProgressUpdated;
        private LoadingBarEventArgs _loadingEventArgs;

        private readonly HorizonClient _horizonClient;
        private readonly ApplicationLibrary _applicationLibrary;
        private readonly AccountManager _accountManager;

        public BackupManager(HorizonClient hzClient,
            ApplicationLibrary appLib,
            AccountManager acctManager)
        {
            _loadingEventArgs = new();

            _horizonClient = hzClient;
            _applicationLibrary = appLib;
            _accountManager = acctManager;
        }

        #region Save
        public async Task<BackupRequestOutcome> BackupUserSaveData(LibHac.Fs.UserId userId,
            string location,
            SaveOptions saveOptions = SaveOptions.Default)
        {
            // TODO: cancellation source

            var userSaves = GetUserSaveData(userId, saveOptions);
            if (userSaves.IsNullOrEmpty())
            {
                Logger.Warning?.Print(LogClass.Application, "No save data found");
                return new BackupRequestOutcome
                {
                    DidFail = false,
                    Message = "No save data found"
                };
            }

            _loadingEventArgs.Curr = 0;
            _loadingEventArgs.Max = userSaves.Count() + 1; // add one for metadata file
            BackupProgressUpdated?.Invoke(this, _loadingEventArgs);

            // Create the top level temp dir for the intermediate copies - ensure it's empty
            // TODO: should this go in the location since data has to go there anyway? might make the ultimate zip faster since IO is local?
            var backupTempDir = Path.Combine(AppDataManager.BackupDirPath, $"{userId}_library_saveTemp");

            // Generate a metadata item so users know what titleIds are and such in case they're moving around, jksv, sanity, etc

            try
            {
                // Delete temp for good measure?
                _ = Directory.CreateDirectory(backupTempDir);

                var outcome = await BatchCopySavesToTempDir(userSaves, backupTempDir)
                    && CompleteBackup(location, userId, backupTempDir);

                return new BackupRequestOutcome
                {
                    DidFail = !outcome,
                    Message = outcome
                        ? string.Empty
                        : "Failed to backup user saves"
                };
            }
            catch (Exception ex)
            {
                Logger.Error?.Print(LogClass.Application, $"Failed to backup user data - {ex.Message}");
                return new BackupRequestOutcome
                {
                    DidFail = true,
                    Message = $"Failed to backup user data - {ex.Message}"
                };
            }
            finally
            {
                if (Directory.Exists(backupTempDir))
                {
                    Directory.Delete(backupTempDir, true);
                }
            }

            // Produce the actual zip
            static bool CompleteBackup(string location, LibHac.Fs.UserId userId, string backupTempDir)
            {
                var currDate = DateTime.UtcNow.ToString("yyyy-MM-dd");
                var backupFile = Path.Combine(location, $"{currDate}_{userId}_saves.zip");
                return CreateOrReplaceZipFile(backupTempDir, backupFile);
            }
        }

        private IEnumerable<SaveMeta> GetUserSaveData(LibHac.Fs.UserId userId, SaveOptions saveOptions)
        {
            try
            {
                // Always require user saves
                var userSaves = GetSaveData(userId, SaveDataType.Account)
                    .ToList();

                // Device and bcat are optional but enumerate those dirs too if needed
                var deviceSaves = saveOptions.HasFlag(SaveOptions.SaveTypeDevice)
                    ? GetSaveData(default, SaveDataType.Device)
                    : Enumerable.Empty<SaveMeta>();
                userSaves.AddRange(deviceSaves);

                var bcatSaves = saveOptions.HasFlag(SaveOptions.SaveTypeDevice)
                    ? GetSaveData(default, SaveDataType.Bcat)
                    : Enumerable.Empty<SaveMeta>();
                userSaves.AddRange(bcatSaves);

                return userSaves;
            }
            catch (Exception ex)
            {
                Logger.Error?.Print(LogClass.Application, $"Failed to enumerate user save data - {ex.Message}");
            }

            return Enumerable.Empty<SaveMeta>();
        }

        private IEnumerable<SaveMeta> GetSaveData(LibHac.Fs.UserId userId, SaveDataType saveType)
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
            List<SaveMeta> saves = new();

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
                        saves.Add(new SaveMeta
                        {
                            SaveDataId = saveDataInfo[i].SaveDataId,
                            Type = saveDataInfo[i].Type,
                            UserId = saveDataInfo[i].UserId,
                            ProgramId = saveDataInfo[i].ProgramId,
                        });
                    }
                }
            } while (true);

            return saves;
        }

        private async Task<bool> BatchCopySavesToTempDir(IEnumerable<SaveMeta> userSaves, string backupTempDir)
        {
            // keep track of save data metadata <programId, app title>
            Dictionary<ulong, UserFriendlyAppData> userFriendlyMetadataMap = new();

            try
            {
                // batch intermediate copies so we don't overwhelm systems
                const int BATCH_SIZE = 5;
                List<Task<bool>> tempCopyTasks = new(BATCH_SIZE);

                // Copy each applications save data to it's own folder in the temp dir
                foreach (var meta in userSaves)
                {
                    // if the buffer is full, wait for it to drain
                    if (tempCopyTasks.Count >= BATCH_SIZE)
                    {
                        // TODO: error handling?
                        _ = await Task.WhenAll(tempCopyTasks);
                        tempCopyTasks.Clear();
                    }

                    // Add backup task
                    tempCopyTasks.Add(CopySaveDataToIntermediateDirectory(meta, backupTempDir));

                    // Add title metadata entry - might be dupe from bcat/device
                    if (!userFriendlyMetadataMap.ContainsKey(meta.ProgramId.Value))
                    {
                        // This is not great since it's going to incur a disk read per metadata lookup...
                        // TODO: optimize later
                        var titleIdHex = meta.ProgramId.Value.ToString("x16");

                        // TODO: MainWindow.MainWindowViewModel.Applications.FirstOrDefault(x => x.TitleId.ToUpper() == TitleIdString);
                        var appMeta = _applicationLibrary.LoadAndSaveMetaData(titleIdHex);
                        userFriendlyMetadataMap.Add(meta.ProgramId.Value, new UserFriendlyAppData
                        {
                            Title = appMeta?.Title,
                            TitleId = meta.ProgramId.Value,
                            TitleIdHex = titleIdHex
                        });
                    }
                }

                // wait for any outstanding temp copies to complete
                _ = await Task.WhenAll(tempCopyTasks);

                // finally, move the metadata tag file into the backup dir and track progress
                await WriteMetadataFile(backupTempDir, userFriendlyMetadataMap);
                _loadingEventArgs.Curr++;
                BackupProgressUpdated?.Invoke(this, _loadingEventArgs);

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error?.Print(LogClass.Application, $"Failed to copy save data to intermediate directory - {ex.Message}");
            }

            return false;

            #region LocalMethods
            async Task WriteMetadataFile(string backupTempDir, Dictionary<ulong, UserFriendlyAppData> userFriendlyMetadataMap)
            {
                try
                {
                    var tagFile = Path.Combine(backupTempDir, "tag.json");

                    var completeMeta = System.Text.Json.JsonSerializer.Serialize(new UserFriendlySaveMetadata
                    {
                        // TODO: fix this user access.
                        UserId = _accountManager.LastOpenedUser.UserId.ToString(),
                        ProfileName = _accountManager.LastOpenedUser.Name,
                        CreationTimeUtc = DateTime.UtcNow,
                        ApplicationMap = userFriendlyMetadataMap.Values
                    });
                    await File.WriteAllTextAsync(tagFile, completeMeta);
                }
                catch (Exception ex)
                {
                    Logger.Error?.Print(LogClass.Application, $"Failed to write user friendly save metadata file - {ex.Message}");
                }
            }
            #endregion
        }

        private async Task<bool> CopySaveDataToIntermediateDirectory(SaveMeta saveMeta, string destinationDir)
        {
            // Find the most recent version of the data, there is a commited (0) and working (1) paths directory
            var saveRootPath = ApplicationHelper.FindValidSaveDir(saveMeta.SaveDataId);

            // the actual title in the name would be nice but titleId is more reliable
            // [backupLocation]/[titleId]/[saveType]
            var copyDestPath = Path.Combine(destinationDir, saveMeta.ProgramId.Value.ToString(), saveMeta.Type.ToString());

            var result = await CopyDirectoryAsync(saveRootPath, copyDestPath);

            // Update progress for each dir we copy save data for
            _loadingEventArgs.Curr++;
            BackupProgressUpdated?.Invoke(this, _loadingEventArgs);

            return result;
        }

        private static async Task<bool> CopyDirectoryAsync(string sourceDirectory, string destDirectory)
        {
            bool result = true;
            Directory.CreateDirectory(destDirectory);

            foreach (string filename in Directory.EnumerateFileSystemEntries(sourceDirectory))
            {
                var itemDest = Path.Combine(destDirectory, Path.GetFileName(filename));
                var attrs = File.GetAttributes(filename);

                result &= attrs switch
                {
                    _ when ((attrs & FileAttributes.Directory) == FileAttributes.Directory) => await CopyDirectoryAsync(filename, itemDest),
                    _ => await CopyFileAsync(filename, itemDest)
                };

                if (!result)
                {
                    // TODO: use options to decide? or hard fail
                    continue;
                }
            }

            return result;

            #region LocalMethod
            static async Task<bool> CopyFileAsync(string source, string destination, int retryCount = 0)
            {
                try
                {
                    if (retryCount > 0)
                    {
                        Logger.Debug?.Print(LogClass.Application, $"Backing off retrying copy of {source}");
                        await Task.Delay((int)(Math.Pow(2, retryCount) * 200));
                    }

                    using FileStream sourceStream = File.Open(source, FileMode.Open);
                    using FileStream destinationStream = File.Create(destination);

                    await sourceStream.CopyToAsync(destinationStream);
                    return true;
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("is being used by another process", StringComparison.OrdinalIgnoreCase))
                    {
                        const int retryThreshold = 3;
                        return (++retryCount < retryThreshold)
                            && await CopyFileAsync(source, destination, retryCount);
                    }

                    Logger.Error?.Print(LogClass.Application, $"Failed to copy file {source} - {ex.Message}");
                    return false;
                }
            }
            #endregion
        }

        public static bool CreateOrReplaceZipFile(string sourceDataDirectory, string backupDestinationFullPath)
        {
            try
            {
                if (File.Exists(backupDestinationFullPath))
                {
                    File.Delete(backupDestinationFullPath);
                }

                ZipFile.CreateFromDirectory(sourceDataDirectory, backupDestinationFullPath, CompressionLevel.SmallestSize, false);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error?.Print(LogClass.Application, $"Failed to zip data.\n{ex.Message}");
                return false;
            }
        }
        #endregion

        #region Load
        public Task<bool> LoadSaveData(ulong titleId, LibHac.Fs.UserId userId, string sourceDataPath)
        {
            return Task.FromResult(true);
        }
        #endregion
    }
}
