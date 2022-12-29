using LibHac;
using LibHac.Account;
using LibHac.Common;
using LibHac.Fs;
using LibHac.Fs.Fsa;
using LibHac.Fs.Shim;
using LibHac.Ns;
using LibHac.Tools.Fs;
using LibHac.Tools.FsSystem;
using Ryujinx.Ava.Ui.Controls;
using Ryujinx.Ava.Ui.Models;
using Ryujinx.Common.Logging;
using Ryujinx.HLE.FileSystem;
using Ryujinx.Ui.App.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using Logger = Ryujinx.Common.Logging.Logger;
using Path = System.IO.Path;
using Result = LibHac.Result;
using UserId = LibHac.Fs.UserId;
using UserProfile = Ryujinx.Ava.Ui.Models.UserProfile;

namespace Ryujinx.Ava.Common
{
    internal class SaveDataFileManager
    {
        private readonly UserProfile _userProfile;
        private readonly HorizonClient _horizonClient;
        private readonly VirtualFileSystem _virtualFileSystem;
        private readonly UserId _userId;

        private readonly List<ApplicationData> _applications;

        public readonly string mountName = "save";
        public readonly string outputMountName = "output";

        public SaveDataFileManager(List<ApplicationData> applications, UserProfile userProfile, HorizonClient horizonClient, VirtualFileSystem virtualFileSystem, UserId userId)
        {
            _userProfile = userProfile;
            _horizonClient = horizonClient;
            _virtualFileSystem = virtualFileSystem;
            _applications = applications;
            _userId = userId;
        }


        #region Export

        public void SaveUserSaveDirectoryAsZip(string backupFolder, List<SaveModel> saves)
        {
            CreateSaveDataBackup(backupFolder, saves);

            ZipFile.CreateFromDirectory(backupFolder, backupFolder + ".zip");
            Directory.Delete(backupFolder, true);
        }

        private void CreateSaveDataBackup(string backupPath, List<SaveModel> saves)
        {
            U8Span mountNameU8 = mountName.ToU8Span();
            U8Span outputMountNameU8 = outputMountName.ToU8Span();

            foreach (ApplicationData application in _applications)
            {
                try
                {
                    //Register destination folder as output and mount output
                    Result registerOutpDirResult = RegisterOutputDirectory(Path.Combine(backupPath, application.TitleId.ToUpper()), outputMountNameU8);
                    if (registerOutpDirResult.IsFailure())
                    {
                        Logger.Error.Value.Print(LogClass.Application, $"Could not register and mount output directory.");
                    }

                    //Mount SaveData as save, opens the saveDataIterators and starts reading saveDataInfo
                    Result openAndReadSaveDataResult = OpenSaveDataIteratorAndReadSaveData(mountNameU8, application, out SaveDataInfo saveDataInfo);

                    if (openAndReadSaveDataResult.IsFailure())
                    {
                        Logger.Error.Value.Print(LogClass.Application, $"Could not open save Iterator and start reading for application: {application.TitleName}");
                    }
                    else
                    {
                        //Copies the whole directory from save mount to output mount
                        Result copyDirResult = CopySaveDataDirectory(mountName, outputMountName);
                        Logger.Info.Value.Print(LogClass.Application, $"Successfuly created backup for {application.TitleName}.");
                    }

                    //Unmount save and output
                    UnmountDirectory(mountNameU8);
                    UnmountDirectory(outputMountNameU8);
                }
                catch (Exception)
                {
                    continue;
                }
            }
        }


        #endregion



        #region Import

        public void RestoreSavedataBackup(string backupZipFile)
        {
            ExtractBackupToSaveDirectory(backupZipFile);

            Logger.Info.Value.Print(LogClass.Application, $"Done extracting savedata backup!", nameof(SaveDataImporter));
        }

        private void ExtractBackupToSaveDirectory(string backupZipFile)
        {
            if (!string.IsNullOrWhiteSpace(backupZipFile) && File.Exists(backupZipFile))
            {
                string extractedZipFolders = ExtractZip(backupZipFile);

                Logger.Info.Value.Print(LogClass.Application, $"Extracted Backup zip to temp path: {extractedZipFolders}", nameof(SaveDataImporter));

                U8Span mountNameU8 = mountName.ToU8Span();
                U8Span outputMountNameU8 = outputMountName.ToU8Span();


                foreach (ApplicationData application in _applications)
                {
                    try
                    {
                        string backupTitleSaveDataFolder = Path.Combine(Directory.GetParent(extractedZipFolders).FullName, application.TitleId.ToUpper());

                        //Register destination folder as output and mount output
                        Result registerSaveDataBackupFolderResult = RegisterOutputDirectory(backupTitleSaveDataFolder, outputMountNameU8);
                        if (registerSaveDataBackupFolderResult.IsFailure())
                        {
                            Logger.Error.Value.Print(LogClass.Application, $"Could not register and mount output directory.");
                        }

                        //Mount SaveData as save, opens the saveDataIterators and starts reading saveDataInfo
                        Result openAndReadSaveDataResult = OpenSaveDataIteratorAndReadSaveData(mountNameU8, application, out SaveDataInfo saveDataInfo);

                        if (openAndReadSaveDataResult.IsFailure())
                        {
                            Logger.Error.Value.Print(LogClass.Application, $"Could not open save Iterator and start reading for application: {application.TitleName}");
                        }
                        else
                        {
                            //Copies the whole directory from backup mount to saveData mount
                            Result copyDirResult = CopySaveDataDirectory(outputMountName, mountName);
                            Logger.Info.Value.Print(LogClass.Application, $"Successfuly restored backup for: {application.TitleName}.");
                        }

                        Result commitOuptut = _horizonClient.Fs.Commit(outputMountNameU8);

                        //Unmount save and output
                        UnmountDirectory(mountNameU8);
                        UnmountDirectory(outputMountNameU8);
                    }
                    catch (Exception)
                    {
                        continue;
                    }
                }
            }
        }

        private string ExtractZip(string backupZipFile)
        {
            string tempZipExtractionPath = Path.GetTempPath();
            ZipFile.ExtractToDirectory(backupZipFile, tempZipExtractionPath, true);

            return tempZipExtractionPath;
        }


        #endregion


        #region Horizon OS Open/Register/Read/Mount Stuff

        private Result RegisterOutputDirectory(string backupPath, U8Span mountName)
        {
            using UniqueRef<IFileSystem> outputFileSystem = new UniqueRef<IFileSystem>(new LibHac.FsSystem.LocalFileSystem(backupPath));

            Result registerResult = _horizonClient.Fs.Register(mountName, ref outputFileSystem.Ref());
            if (registerResult.IsFailure()) return registerResult.Miss();

            return registerResult;
        }

        private Result GetSaveDataIterator(out SaveDataInfo saveDataInfo, ApplicationData application)
        {
            return _horizonClient.Fs.FindSaveDataWithFilter(out saveDataInfo,
                SaveDataSpaceId.User,
                SaveDataFilter.Make(ulong.Parse(application.TitleId, NumberStyles.HexNumber),
                saveType: default,
                _userId,
                saveDataId: default,
                index: default));
        }

        private Result MountSaveDataDirectory(ulong programId, U8Span mountName)
        {
            if (!_horizonClient.Fs.IsMounted(mountName))
            {
                return _horizonClient.Fs.MountSaveData(mountName, ConvertProgramIdToApplicationId(programId), _userId);
            }

            return Result.Success;
        }

        private Result UnmountDirectory(U8Span mountName)
        {
            if (_horizonClient.Fs.IsMounted(mountName))
            {
                Result commitFilesResult = _horizonClient.Fs.CommitSaveData(mountName);
                if (commitFilesResult.IsFailure()) return commitFilesResult;

                _horizonClient.Fs.Unmount(mountName);
            }

            return Result.Success;
        }

        private bool CreateSaveData(ApplicationData app)
        {
            ref ApplicationControlProperty control = ref app.ControlHolder.Value;

            Logger.Info?.Print(LogClass.Application, $"Creating save directory for Title: {app.TitleName} [{app.TitleId:x16}]");

            if (Utilities.IsZeros(app.ControlHolder.ByteSpan))
            {
                // If the current application doesn't have a loaded control property, create a dummy one
                // and set the savedata sizes so a user savedata will be created.
                control = ref new BlitStruct<ApplicationControlProperty>(1).Value;

                // The set sizes don't actually matter as long as they're non-zero because we use directory savedata.
                control.UserAccountSaveDataSize = 0x4000;
                control.UserAccountSaveDataJournalSize = 0x4000;

                Logger.Warning?.Print(LogClass.Application,
                    "No control file was found for this game. Using a dummy one instead. This may cause inaccuracies in some games.");
            }

            Uid user = new Uid(ulong.Parse(_userProfile.UserId.High.ToString(), NumberStyles.HexNumber),
                               ulong.Parse(_userProfile.UserId.Low.ToString(), NumberStyles.HexNumber));

            Result findSaveDataResult = _horizonClient.Fs.EnsureApplicationSaveData(out _, new LibHac.Ncm.ApplicationId(ulong.Parse(app.TitleId, NumberStyles.HexNumber)), in control, in user);

            return findSaveDataResult.IsSuccess();
        }

        private Result OpenSaveDataIteratorAndReadSaveData(U8Span mountName, ApplicationData application, out SaveDataInfo saveDataInfo)
        {
            Result getSvDataIteratorResult = GetSaveDataIterator(out saveDataInfo, application);
            if (getSvDataIteratorResult.IsFailure())
            {
                bool createdSaveData = CreateSaveData(application);

                if (!createdSaveData)
                {
                    Logger.Warning?.Print(LogClass.Application, "Could not create saveData for " + application.TitleName);
                    return getSvDataIteratorResult;
                }

                getSvDataIteratorResult = GetSaveDataIterator(out saveDataInfo, application);
                if (getSvDataIteratorResult.IsFailure()) return getSvDataIteratorResult;
            }

            Result mountSvDataResult = MountSaveDataDirectory(saveDataInfo.ProgramId.Value, mountName);
            if (mountSvDataResult.IsFailure()) return mountSvDataResult;

            UniqueRef<SaveDataIterator> saveDataIterator = new UniqueRef<SaveDataIterator>();
            Result openSvDataIteratorResult = _horizonClient.Fs.OpenSaveDataIterator(ref saveDataIterator, SaveDataSpaceId.User);
            if (openSvDataIteratorResult.IsFailure()) return openSvDataIteratorResult;

            Result readSvDataInfoResult = saveDataIterator.Get.ReadSaveDataInfo(out _, new Span<SaveDataInfo>(ref saveDataInfo));
            if (openSvDataIteratorResult.IsFailure()) return readSvDataInfoResult;

            return Result.Success;
        }


        #endregion


        #region Copy Directory/File
        private Result CopySaveDataDirectory(string sourcePath, string destPath)
        {
            Result openDir = _horizonClient.Fs.OpenDirectory(out DirectoryHandle dirHandle, $"{sourcePath}:/".ToU8Span(), OpenDirectoryMode.All);
            if (openDir.IsFailure()) return openDir;

            using (dirHandle)
            {
                sourcePath = $"{sourcePath}:/";
                destPath = $"{destPath}:/";
                foreach (DirectoryEntryEx entry in _horizonClient.Fs.EnumerateEntries(sourcePath, "*", SearchOptions.Default))
                {
                    string subSrcPath = PathTools.Normalize(PathTools.Combine(sourcePath, entry.Name));
                    string subDstPath = PathTools.Normalize(PathTools.Combine(destPath, entry.Name));

                    if (entry.Type == DirectoryEntryType.Directory)
                    {
                        Result copyDirResult = CopyDirectory(subSrcPath, subDstPath);
                    }
                    if (entry.Type == DirectoryEntryType.File)
                    {
                        Result copyFinalDir = CopyFile(subSrcPath, subDstPath, entry.Size);
                    }
                }

                _horizonClient.Fs.CloseDirectory(dirHandle);

                return Result.Success;
            }
        }

        private Result CopyDirectory(string subSrcPath, string subDstPath)
        {
            _horizonClient.Fs.EnsureDirectoryExists(subDstPath);

            Result copyDirResult = _horizonClient.Fs.CopyDirectory(subSrcPath, subDstPath);

            if (copyDirResult.IsFailure())
            {
                Logger.Error.Value.Print(LogClass.Application, $"Could not copy directory: \n{subSrcPath} to destination {subDstPath}");
            }

            Logger.Info.Value.Print(LogClass.Application, $"Successfully copied directory: \n{subSrcPath} to destination {subDstPath}");

            return copyDirResult;
        }

        private Result CopyFile(string subSrcPath, string subDstPath, long entrySize)
        {
            _horizonClient.Fs.CreateOrOverwriteFile(subDstPath, entrySize);

            Result copyFileResult = _horizonClient.Fs.CopyFile(subSrcPath, subDstPath);
            if (copyFileResult.IsFailure())
            {
                Logger.Error.Value.Print(LogClass.Application, $"Could not copy file: \n{subSrcPath} to destination '{subDstPath}");
            }

            Logger.Info.Value.Print(LogClass.Application, $"Successfully copied file: \n{subSrcPath} to destination {subDstPath}");

            return copyFileResult;
        }

        #endregion

        private LibHac.Ncm.ApplicationId ConvertProgramIdToApplicationId(ulong programId)
        {
            return new LibHac.Ncm.ApplicationId(programId);
        }
    }
}