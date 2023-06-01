using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Threading;
using DiscordRPC;
using LibHac;
using LibHac.Account;
using LibHac.Bcat;
using LibHac.Common;
using LibHac.Fs;
using LibHac.Fs.Fsa;
using LibHac.Fs.Shim;
using LibHac.FsSystem;
using LibHac.Ns;
using LibHac.Tools.Fs;
using LibHac.Tools.FsSystem;
using LibHac.Tools.FsSystem.NcaUtils;
using Ryujinx.Ava.Common.Locale;
using Ryujinx.Ava.UI.Controls;
using Ryujinx.Ava.UI.Helpers;
using Ryujinx.Ava.UI.Windows;
using Ryujinx.Common.Configuration;
using Ryujinx.Common.Logging;
using Ryujinx.HLE.FileSystem;
using Ryujinx.HLE.HOS.Services.Account.Acc;
using Ryujinx.Ui.App.Common;
using Ryujinx.Ui.Common.Helper;
using System;
using System.Buffers;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Path = System.IO.Path;

namespace Ryujinx.Ava.Common
{
    internal static class ApplicationHelper
    {
        private static HorizonClient _horizonClient;
        private static AccountManager _accountManager;
        private static VirtualFileSystem _virtualFileSystem;
        private static StyleableWindow _owner;

        public static void Initialize(VirtualFileSystem virtualFileSystem, AccountManager accountManager, HorizonClient horizonClient, StyleableWindow owner)
        {
            _owner = owner;
            _virtualFileSystem = virtualFileSystem;
            _horizonClient = horizonClient;
            _accountManager = accountManager;
        }

        private static bool TryFindSaveData(string titleName, ulong titleId, BlitStruct<ApplicationControlProperty> controlHolder, in SaveDataFilter filter, out ulong saveDataId)
        {
            saveDataId = default;

            Result result = _horizonClient.Fs.FindSaveDataWithFilter(out SaveDataInfo saveDataInfo, SaveDataSpaceId.User, in filter);
            if (ResultFs.TargetNotFound.Includes(result))
            {
                ref ApplicationControlProperty control = ref controlHolder.Value;

                Logger.Info?.Print(LogClass.Application, $"Creating save directory for Title: {titleName} [{titleId:x16}]");

                if (Utilities.IsZeros(controlHolder.ByteSpan))
                {
                    // If the current application doesn't have a loaded control property, create a dummy one
                    // and set the savedata sizes so a user savedata will be created.
                    control = ref new BlitStruct<ApplicationControlProperty>(1).Value;

                    // The set sizes don't actually matter as long as they're non-zero because we use directory savedata.
                    control.UserAccountSaveDataSize = 0x4000;
                    control.UserAccountSaveDataJournalSize = 0x4000;

                    Logger.Warning?.Print(LogClass.Application, "No control file was found for this game. Using a dummy one instead. This may cause inaccuracies in some games.");
                }

                Uid user = new((ulong)_accountManager.LastOpenedUser.UserId.High, (ulong)_accountManager.LastOpenedUser.UserId.Low);

                result = _horizonClient.Fs.EnsureApplicationSaveData(out _, new LibHac.Ncm.ApplicationId(titleId), in control, in user);
                if (result.IsFailure())
                {
                    Dispatcher.UIThread.InvokeAsync(async () =>
                    {
                        await ContentDialogHelper.CreateErrorDialog(LocaleManager.Instance.UpdateAndGetDynamicValue(LocaleKeys.DialogMessageCreateSaveErrorMessage, result.ToStringWithName()));
                    });

                    return false;
                }

                // Try to find the savedata again after creating it
                result = _horizonClient.Fs.FindSaveDataWithFilter(out saveDataInfo, SaveDataSpaceId.User, in filter);
            }

            if (result.IsSuccess())
            {
                saveDataId = saveDataInfo.SaveDataId;

                return true;
            }

            Dispatcher.UIThread.InvokeAsync(async () =>
            {
                await ContentDialogHelper.CreateErrorDialog(LocaleManager.Instance.UpdateAndGetDynamicValue(LocaleKeys.DialogMessageFindSaveErrorMessage, result.ToStringWithName()));
            });

            return false;
        }

        public static void OpenSaveDir(in SaveDataFilter saveDataFilter, ulong titleId, BlitStruct<ApplicationControlProperty> controlData, string titleName)
        {
            if (!TryFindSaveData(titleName, titleId, controlData, in saveDataFilter, out ulong saveDataId))
            {
                return;
            }

            OpenSaveDir(saveDataId);
        }

        public static void OpenSaveDir(ulong saveDataId)
        {
            OpenHelper.OpenFolder(FindValidSaveDir(saveDataId));
        }

        public static string FindValidSaveDir(ulong saveDataId)
        {
            string saveRootPath = Path.Combine(_virtualFileSystem.GetNandPath(), $"user/save/{saveDataId:x16}");

            if (!Directory.Exists(saveRootPath))
            {
                // Inconsistent state. Create the directory
                Directory.CreateDirectory(saveRootPath);
            }

            // commited expected to be at /0, otherwise working is /1
            string attemptPath = Path.Combine(saveRootPath, "0");

            // If the committed directory exists, that path will be loaded the next time the savedata is mounted
            if (Directory.Exists(attemptPath))
            {
                return attemptPath;
            }
            
            // If the working directory exists and the committed directory doesn't,
            // the working directory will be loaded the next time the savedata is mounted
            attemptPath = Path.Combine(saveRootPath, "1");
            if (!Directory.Exists(attemptPath))
            {
                Directory.CreateDirectory(attemptPath);
            }

            return attemptPath;
        }

        public static async Task ExtractSection(NcaSectionType ncaSectionType, string titleFilePath, string titleName, int programIndex = 0)
        {
            OpenFolderDialog folderDialog = new()
            {
                Title = LocaleManager.Instance[LocaleKeys.FolderDialogExtractTitle]
            };

            string destination = await folderDialog.ShowAsync(_owner);
            var cancellationToken = new CancellationTokenSource();

            UpdateWaitWindow waitingDialog = new(
                LocaleManager.Instance[LocaleKeys.DialogNcaExtractionTitle],
                LocaleManager.Instance.UpdateAndGetDynamicValue(LocaleKeys.DialogNcaExtractionMessage, ncaSectionType, Path.GetFileName(titleFilePath)),
                cancellationToken);

            if (!string.IsNullOrWhiteSpace(destination))
            {
                Thread extractorThread = new(() =>
                {
                    Dispatcher.UIThread.Post(waitingDialog.Show);

                    using FileStream file = new(titleFilePath, FileMode.Open, FileAccess.Read);

                    Nca mainNca = null;
                    Nca patchNca = null;

                    string extension = Path.GetExtension(titleFilePath).ToLower();
                    if (extension == ".nsp" || extension == ".pfs0" || extension == ".xci")
                    {
                        PartitionFileSystem pfs;

                        if (extension == ".xci")
                        {
                            pfs = new Xci(_virtualFileSystem.KeySet, file.AsStorage()).OpenPartition(XciPartitionType.Secure);
                        }
                        else
                        {
                            pfs = new PartitionFileSystem(file.AsStorage());
                        }

                        foreach (DirectoryEntryEx fileEntry in pfs.EnumerateEntries("/", "*.nca"))
                        {
                            using var ncaFile = new UniqueRef<IFile>();

                            pfs.OpenFile(ref ncaFile.Ref, fileEntry.FullPath.ToU8Span(), OpenMode.Read).ThrowIfFailure();

                            Nca nca = new(_virtualFileSystem.KeySet, ncaFile.Get.AsStorage());
                            if (nca.Header.ContentType == NcaContentType.Program)
                            {
                                int dataIndex = Nca.GetSectionIndexFromType(NcaSectionType.Data, NcaContentType.Program);
                                if (nca.SectionExists(NcaSectionType.Data) && nca.Header.GetFsHeader(dataIndex).IsPatchSection())
                                {
                                    patchNca = nca;
                                }
                                else
                                {
                                    mainNca = nca;
                                }
                            }
                        }
                    }
                    else if (extension == ".nca")
                    {
                        mainNca = new Nca(_virtualFileSystem.KeySet, file.AsStorage());
                    }

                    if (mainNca == null)
                    {
                        Logger.Error?.Print(LogClass.Application, "Extraction failure. The main NCA was not present in the selected file");

                        Dispatcher.UIThread.InvokeAsync(async () =>
                        {
                            waitingDialog.Close();

                            await ContentDialogHelper.CreateErrorDialog(LocaleManager.Instance[LocaleKeys.DialogNcaExtractionMainNcaNotFoundErrorMessage]);
                        });

                        return;
                    }

                    (Nca updatePatchNca, _) = ApplicationLibrary.GetGameUpdateData(_virtualFileSystem, mainNca.Header.TitleId.ToString("x16"), programIndex, out _);
                    if (updatePatchNca != null)
                    {
                        patchNca = updatePatchNca;
                    }

                    int index = Nca.GetSectionIndexFromType(ncaSectionType, mainNca.Header.ContentType);

                    try
                    {
                        bool sectionExistsInPatch = false;
                        if (patchNca != null)
                        {
                            sectionExistsInPatch = patchNca.CanOpenSection(index);
                        }

                        IFileSystem ncaFileSystem = sectionExistsInPatch ? mainNca.OpenFileSystemWithPatch(patchNca, index, IntegrityCheckLevel.ErrorOnInvalid)
                                                                         : mainNca.OpenFileSystem(index, IntegrityCheckLevel.ErrorOnInvalid);

                        FileSystemClient fsClient = _horizonClient.Fs;

                        string source = DateTime.Now.ToFileTime().ToString()[10..];
                        string output = DateTime.Now.ToFileTime().ToString()[10..];

                        using var uniqueSourceFs = new UniqueRef<IFileSystem>(ncaFileSystem);
                        using var uniqueOutputFs = new UniqueRef<IFileSystem>(new LocalFileSystem(destination));

                        fsClient.Register(source.ToU8Span(), ref uniqueSourceFs.Ref);
                        fsClient.Register(output.ToU8Span(), ref uniqueOutputFs.Ref);

                        (Result? resultCode, bool canceled) = CopyDirectory(fsClient, $"{source}:/", $"{output}:/", cancellationToken.Token);

                        if (!canceled)
                        {
                            if (resultCode.Value.IsFailure())
                            {
                                Logger.Error?.Print(LogClass.Application, $"LibHac returned error code: {resultCode.Value.ErrorCode}");

                                Dispatcher.UIThread.InvokeAsync(async () =>
                                {
                                    waitingDialog.Close();

                                    await ContentDialogHelper.CreateErrorDialog(LocaleManager.Instance[LocaleKeys.DialogNcaExtractionCheckLogErrorMessage]);
                                });
                            }
                            else if (resultCode.Value.IsSuccess())
                            {
                                Dispatcher.UIThread.Post(waitingDialog.Close);

                                NotificationHelper.Show(
                                    LocaleManager.Instance[LocaleKeys.DialogNcaExtractionTitle],
                                    $"{titleName}\n\n{LocaleManager.Instance[LocaleKeys.DialogNcaExtractionSuccessMessage]}",
                                    NotificationType.Information);
                            }
                        }

                        fsClient.Unmount(source.ToU8Span());
                        fsClient.Unmount(output.ToU8Span());
                    }
                    catch (ArgumentException ex)
                    {
                        Logger.Error?.Print(LogClass.Application, $"{ex.Message}");

                        Dispatcher.UIThread.InvokeAsync(async () =>
                        {
                            waitingDialog.Close();

                            await ContentDialogHelper.CreateErrorDialog(ex.Message);
                        });
                    }
                });

                extractorThread.Name = "GUI.NcaSectionExtractorThread";
                extractorThread.IsBackground = true;
                extractorThread.Start();
            }
        }

        public static (Result? result, bool canceled) CopyDirectory(FileSystemClient fs, string sourcePath, string destPath, CancellationToken token)
        {
            Result rc = fs.OpenDirectory(out DirectoryHandle sourceHandle, sourcePath.ToU8Span(), OpenDirectoryMode.All);
            if (rc.IsFailure())
            {
                return (rc, false);
            }

            using (sourceHandle)
            {
                foreach (DirectoryEntryEx entry in fs.EnumerateEntries(sourcePath, "*", SearchOptions.Default))
                {
                    if (token.IsCancellationRequested)
                    {
                        return (null, true);
                    }

                    string subSrcPath = PathTools.Normalize(PathTools.Combine(sourcePath, entry.Name));
                    string subDstPath = PathTools.Normalize(PathTools.Combine(destPath, entry.Name));

                    if (entry.Type == DirectoryEntryType.Directory)
                    {
                        fs.EnsureDirectoryExists(subDstPath);

                        (Result? result, bool canceled) = CopyDirectory(fs, subSrcPath, subDstPath, token);
                        if (canceled || result.Value.IsFailure())
                        {
                            return (result, canceled);
                        }
                    }

                    if (entry.Type == DirectoryEntryType.File)
                    {
                        fs.CreateOrOverwriteFile(subDstPath, entry.Size);

                        rc = CopyFile(fs, subSrcPath, subDstPath);
                        if (rc.IsFailure())
                        {
                            return (rc, false);
                        }
                    }
                }
            }

            return (Result.Success, false);
        }

        public static Result CopyFile(FileSystemClient fs, string sourcePath, string destPath)
        {
            Result rc = fs.OpenFile(out FileHandle sourceHandle, sourcePath.ToU8Span(), OpenMode.Read);
            if (rc.IsFailure())
            {
                return rc;
            }

            using (sourceHandle)
            {
                rc = fs.OpenFile(out FileHandle destHandle, destPath.ToU8Span(), OpenMode.Write | OpenMode.AllowAppend);
                if (rc.IsFailure())
                {
                    return rc;
                }

                using (destHandle)
                {
                    const int MaxBufferSize = 1024 * 1024;

                    rc = fs.GetFileSize(out long fileSize, sourceHandle);
                    if (rc.IsFailure())
                    {
                        return rc;
                    }

                    int bufferSize = (int)Math.Min(MaxBufferSize, fileSize);

                    byte[] buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
                    try
                    {
                        for (long offset = 0; offset < fileSize; offset += bufferSize)
                        {
                            int toRead = (int)Math.Min(fileSize - offset, bufferSize);
                            Span<byte> buf = buffer.AsSpan(0, toRead);

                            rc = fs.ReadFile(out long _, sourceHandle, offset, buf);
                            if (rc.IsFailure())
                            {
                                return rc;
                            }

                            rc = fs.WriteFile(destHandle, offset, buf, WriteOption.None);
                            if (rc.IsFailure())
                            {
                                return rc;
                            }
                        }
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(buffer);
                    }

                    rc = fs.FlushFile(destHandle);
                    if (rc.IsFailure())
                    {
                        return rc;
                    }
                }
            }

            return Result.Success;
        }

        public static async Task<bool> BackupSaveData(ulong titleId)
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
            var result = CreateApplicationSaveBackupZip(titleId, backupTempDir, backupFile);
            Directory.Delete(Path.Combine(backupTempDir, ".."), true);

            if (result)
            {
                OpenHelper.OpenFolder(backupRootDirectory);
            }

            return result;
        }

        public static async Task<bool> CopySaveDataToTemp(ulong titleId, LibHac.Fs.UserId userId, SaveDataType saveType, string backupTempDirectory)
        {
            // save with the user metadata to avoid having to do lookups with libhac?
            var saveDataFilter = SaveDataFilter.Make(titleId, saveType, userId, saveDataId: default, index: default);

            var result = _horizonClient.Fs.FindSaveDataWithFilter(out var saveDataInfo, SaveDataSpaceId.User, in saveDataFilter);
            if (result.IsFailure())
            {
                if (result.ErrorCode is "2002-1002"
                    && saveType is SaveDataType.Device or SaveDataType.Bcat)
                {
                    Logger.Debug?.Print(LogClass.Application, $"Title {titleId} does not have {saveType} data.");
                    return true;
                }

                // Postback instead of throwing UI error?
                _ = Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    await ContentDialogHelper.CreateErrorDialog(LocaleManager.Instance.UpdateAndGetDynamicValue(LocaleKeys.DialogMessageFindSaveErrorMessage, "No save data found"));
                });

                return false;
            }

            // Find the most recent version of the data, there is a commited (0) and working (1) paths directory
            string saveRootPath = FindValidSaveDir(saveDataInfo.SaveDataId);
            var copyDestPath = Path.Combine(backupTempDirectory, saveType.ToString());

            return await CopyDirectoryAsync(saveRootPath, copyDestPath);
        }

        public static async Task<bool> CopyDirectoryAsync(string sourceDirectory, string destDirectory)
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
                    // TODO: log
                    return false;
                }
            }
        }

        public static bool CreateApplicationSaveBackupZip(ulong titleId, string sourceDataPath, string backupDestinationFullPath)
        {
            try
            {
                if (File.Exists(backupDestinationFullPath)) 
                { 
                    File.Delete(backupDestinationFullPath);
                }

                ZipFile.CreateFromDirectory(sourceDataPath, backupDestinationFullPath, CompressionLevel.SmallestSize, false);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error?.Print(LogClass.Application, $"Failed to backup save data for {titleId}.\n{ex.Message}");
                return false;
            }
        }
    }
}