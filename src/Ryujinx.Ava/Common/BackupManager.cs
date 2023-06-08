using LibHac;
using LibHac.Bcat;
using LibHac.Common;
using LibHac.Fs;
using LibHac.Fs.Shim;
using LibHac.Ncm;
using Microsoft.IdentityModel.Tokens;
using Ryujinx.Common.Configuration;
using Ryujinx.Common.Logging;
using Ryujinx.Common.Utilities;
using Ryujinx.HLE.HOS.Services.Account.Acc;
using Ryujinx.Ui.App.Common;
using Ryujinx.Ui.Common.Helper;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Path = System.IO.Path;

namespace Ryujinx.Ava.Common
{
    internal readonly record struct SaveMeta
    {
        public ulong SaveDataId { get; init; }
        public SaveDataType Type { get; init; }

        // TODO: needed?
        public LibHac.Fs.UserId UserId { get; init; }

        // Title Id
        public ProgramId ProgramId { get; init; }
    }

    [Flags]
    public enum SaveOptions
    {
        Account,
        Bcat,
        Device
    }

    public class BackupManager
    {
        private readonly HorizonClient _horizonClient;
        private readonly ApplicationLibrary _applicationLibrary;
        // TODO: remove and pass in
        private readonly AccountManager _accountManager;

        public BackupManager(HorizonClient hzClient,
            ApplicationLibrary appLib,
            AccountManager acctManager)
        {
            _horizonClient = hzClient;
            _applicationLibrary = appLib;
            _accountManager = acctManager;
        }

        #region Save
        public async Task<bool> BackupUserSaveData(LibHac.Fs.UserId userId,
            string location,
            SaveOptions saveOptions = SaveOptions.Account)
        {
            var userSaves = GetUserSaveData(userId, saveOptions);
            if (userSaves.IsNullOrEmpty())
            {
                Logger.Warning?.Print(LogClass.Application, "No save data found");
                return true;
            }

            // Create the top level temp dir for the intermediate copies - ensure it's empty
            // TODO: should this go in the location since data has to go there anyway? might make the ultimate zip faster since IO is local?
            var backupTempDir = Path.Combine(AppDataManager.BackupDirPath, $"{userId}_library_saveTemp");

            // Generate a metadata item so users know what titleIds are and such in case they're moving around, jksv, sanity, etc

            try
            {
                // Delete temp for good measure?
                _ = Directory.CreateDirectory(backupTempDir);

                return await BatchCopySavesToTempDir(userSaves, backupTempDir)
                    && CompleteBackup(location, userId, backupTempDir);
            }
            catch (Exception ex)
            {
                Logger.Error?.Print(LogClass.Application, $"Failed to backup user data - {ex.Message}");
                return false;
            }
            finally
            {
                Directory.Delete(backupTempDir, true);
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
                var deviceSaves = saveOptions.HasFlag(SaveOptions.Device)
                    ? GetSaveData(default, SaveDataType.Device)
                    : Enumerable.Empty<SaveMeta>();
                userSaves.AddRange(deviceSaves);

                var bcatSaves = saveOptions.HasFlag(SaveOptions.Device)
                    ? GetSaveData(default, SaveDataType.Device)
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
            Dictionary<ulong, string> userFriendlyMetadataMap = new();

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
                        var completedCopies = await Task.WhenAll(tempCopyTasks);
                        if (completedCopies.Any(o => o == false))
                        {
                            // Failed to migrate intermediate path - fail or keep going?
                            continue;
                        }

                        tempCopyTasks.Clear();
                    }

                    // Resolve the titleID and add the copy task
                    if (!userFriendlyMetadataMap.TryGetValue(meta.ProgramId.Value, out var title))
                    {
                        // This is not great since it's going to incur a disk read per metadata lookup...
                        // TODO: optimize later
                        var appMeta = _applicationLibrary.LoadAndSaveMetaData(meta.ProgramId.Value.ToString("x16"));
                        title = (appMeta is not null && !string.IsNullOrWhiteSpace(appMeta.Title))
                            ? appMeta.Title
                            : "Unkown Title";

                        userFriendlyMetadataMap.Add(meta.ProgramId.Value, title);
                    }

                    tempCopyTasks.Add(CopySaveDataToIntermediateDirectory(meta, backupTempDir));
                }

                // wait for any outstanding temp copies to complete
                _ = await Task.WhenAll(tempCopyTasks);

                // finally, move the metadata tag file intol the backup dir
                await WriteMetadataFile(backupTempDir, userFriendlyMetadataMap);

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error?.Print(LogClass.Application, $"Failed to copy save data to intermediate directory - {ex.Message}");
            }

            return false;

            static async Task WriteMetadataFile(string backupTempDir, Dictionary<ulong, string> userFriendlyMetadataMap)
            {
                try
                {
                    var tagFile = Path.Combine(backupTempDir, "tag.json");

                    // TODO: serialize user info too like ID and profile name just in case, generation date? i mean file timestamps
                    var completeMeta = System.Text.Json.JsonSerializer.Serialize(userFriendlyMetadataMap);
                    await File.WriteAllTextAsync(tagFile, completeMeta);
                }
                catch (Exception ex)
                {
                    Logger.Error?.Print(LogClass.Application, $"Failed to write user friendly save metadata file - {ex.Message}");
                }
            }
        }

        private static async Task<bool> CopySaveDataToIntermediateDirectory(SaveMeta saveMeta, string destinationDir)
        {
            // Find the most recent version of the data, there is a commited (0) and working (1) paths directory
            var saveRootPath = ApplicationHelper.FindValidSaveDir(saveMeta.SaveDataId);

            // the actual title in the name would be nice but titleId is more reliable
            // [backupLocation]/[titleId]/[saveType]
            var copyDestPath = Path.Combine(destinationDir, saveMeta.ProgramId.Value.ToString(), saveMeta.Type.ToString());

            return await CopyDirectoryAsync(saveRootPath, copyDestPath);
        }
        //---//


        public async Task<bool> BackupSaveData(ulong titleId)
        {
            var userId = new LibHac.Fs.UserId((ulong)_accountManager.LastOpenedUser.UserId.High, (ulong)_accountManager.LastOpenedUser.UserId.Low);

            // /backup/user/[userid]/[titleId]/[saveType]_backup.zip
            // /backup/[titleid]/[userid]/
            var backupRootDirectory = Path.Combine(AppDataManager.BackupDirPath, titleId.ToString(), userId.ToString());

            // Temp is where all save data will be moved to as an intermediate directory before beign zipped and cleaned up
            // /backup/[titleid]/[userid]/[date]/temp
            var currDate = DateTime.UtcNow.ToString("yyyy-MM-dd");
            var backupTempDir = Path.Combine(backupRootDirectory, currDate, "temp");
            if (Directory.Exists(backupTempDir))
            {
                Directory.Delete(backupTempDir, true);
            }

            Directory.CreateDirectory(backupTempDir);

            // Move all application save data for the user, account, and device to the temp folder
            // TODO: use cancellation token for better bail out since they're running in parallel
            var copyBackupFiles = await Task.WhenAll(
                CopySaveDataToTemp(titleId, userId, SaveDataType.Account, backupTempDir),
                // always use default uid for bcat and device data -- there's only one instance per combination of application and device
                CopySaveDataToTemp(titleId, userId: default, SaveDataType.Bcat, backupTempDir),
                CopySaveDataToTemp(titleId, userId: default, SaveDataType.Device, backupTempDir));

            if (copyBackupFiles.Any(outcome => outcome is false))
            {
                Directory.Delete(Path.Combine(backupTempDir, ".."), true);
                return false;
            }

            // Zip up the save data and delete the temp
            var backupFile = Path.Combine(backupRootDirectory, $"{currDate}_{titleId}_save.zip");
            var result = CreateOrReplaceZipFile(backupTempDir, backupFile);
            Directory.Delete(Path.Combine(backupTempDir, ".."), true);

            if (result)
            {
                OpenHelper.OpenFolder(backupRootDirectory);
            }

            return result;
        }

        private async Task<bool> CopySaveDataToTemp(ulong titleId,
            LibHac.Fs.UserId userId,
            SaveDataType saveType,
            string backupTempDirectory)
        {
            var saveDataFilter = SaveDataFilter.Make(titleId,
                saveType,
                userId,
                saveDataId: default,
                index: default);

            var result = _horizonClient.Fs.FindSaveDataWithFilter(out var saveDataInfo, SaveDataSpaceId.User, in saveDataFilter);
            if (result.IsFailure())
            {
                //if (result.ErrorCode is "2002-1002"
                //    && saveType is SaveDataType.Device or SaveDataType.Bcat)
                //{
                //    Logger.Debug?.Print(LogClass.Application, $"Title {titleId} does not have {saveType} data.");
                //    return true;
                //}

                return false;
            }

            // Find the most recent version of the data, there is a commited (0) and working (1) paths directory
            string saveRootPath = ApplicationHelper.FindValidSaveDir(saveDataInfo.SaveDataId);
            var copyDestPath = Path.Combine(backupTempDirectory, saveType.ToString());

            return await CopyDirectoryAsync(saveRootPath, copyDestPath);
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
                    break;
                }
            }

            return result;

            static async Task<bool> CopyFileAsync(string source, string destination)
            {
                try
                {
                    using FileStream sourceStream = File.Open(source, FileMode.Open);
                    using FileStream destinationStream = File.Create(destination);

                    await sourceStream.CopyToAsync(destinationStream);
                    return true;
                }
                catch (Exception ex)
                {
                    Logger.Error?.Print(LogClass.Application, $"Failed to copy file {source} - {ex.Message}");
                    return false;
                }
            }
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
