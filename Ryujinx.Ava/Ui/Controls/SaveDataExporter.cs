using Avalonia.Controls;
using LibHac;
using LibHac.Common;
using LibHac.Fs;
using LibHac.Fs.Fsa;
using LibHac.Fs.Shim;
using LibHac.Tools.Fs;
using LibHac.Tools.FsSystem;
using Ryujinx.Ava.Common.Locale;
using Ryujinx.Ava.Ui.Models;
using Ryujinx.Ava.Ui.Windows;
using Ryujinx.Common.Logging;
using Ryujinx.HLE.FileSystem;
using Ryujinx.Ui.App.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Logger = Ryujinx.Common.Logging.Logger;
using Path = System.IO.Path;

namespace Ryujinx.Ava.Ui.Controls
{
    internal class SaveDataExporter
    {
        private readonly UserProfile _userProfile;
        private readonly HorizonClient _horizonClient;
        private readonly VirtualFileSystem _virtualFileSystem;
        private readonly UserId _userId;

        public SaveDataExporter(UserProfile userProfile, HorizonClient horizonClient, VirtualFileSystem virtualFileSystem)
        {
            _userProfile = userProfile;
            _horizonClient = horizonClient;
            _virtualFileSystem = virtualFileSystem;
            _userId = new UserId(
                ulong.Parse(_userProfile.UserId.High.ToString(), System.Globalization.NumberStyles.HexNumber),
                ulong.Parse(_userProfile.UserId.Low.ToString(), System.Globalization.NumberStyles.HexNumber)
                );
        }

        public async void SaveUserSaveDirectoryAsZip(MainWindow mainWindow, List<SaveModel> saves, List<ApplicationData> applications)
        {
            string backupFolder = await GetAndPrepareBackupPath(mainWindow);
            CreateBackup(backupFolder, saves, applications);

            ZipFile.CreateFromDirectory(backupFolder, backupFolder + ".zip");
            Directory.Delete(backupFolder, true);
        }

        private void CreateBackup(string backupPath, List<SaveModel> saves, List<ApplicationData> applications)
        {
            string mountName = "save";
            string outputMountName = "output";

            foreach (ApplicationData application in applications)
            {
                try
                {
                    //Register destination folder as output and mount output
                    Result registerOutpDirResult = RegisterOutputDirectory(Path.Combine(backupPath, application.TitleId), outputMountName);
                    if (registerOutpDirResult.IsFailure())
                    {
                        Logger.Error.Value.Print(LogClass.Application, $"Could not register and mount output directory.");

                    }

                    //Mount SaveData as save, opens the saveDataIterators and starts reading saveDataInfo
                    Result openAndReadSaveDataResult = OpenSaveDataIteratorAndReadSaveData(mountName, application, out SaveDataInfo saveDataInfo);

                    if(openAndReadSaveDataResult.IsFailure())
                    {
                        Logger.Error.Value.Print(LogClass.Application, $"Could not open save Iterator and start reading for application: {application.TitleName}");
                    }
                    else
                    {
                        //Copies the whole directory from save mount to output mount
                        Result copyDirResult = CopySaveDataDirectory(mountName, outputMountName);
                    }


                    //Unmount save and output
                    UnmountDirectory(saveDataInfo.ProgramId.Value, mountName);
                    UnmountDirectory(saveDataInfo.ProgramId.Value, outputMountName);
                }
                catch (Exception)
                {
                    continue;
                }
            }
        }

        private Result RegisterOutputDirectory(string backupPath, string mountName)
        {
            using UniqueRef<IFileSystem> outputFileSystem = new UniqueRef<IFileSystem>(new LibHac.FsSystem.LocalFileSystem(backupPath));

            Result registerResult = _horizonClient.Fs.Register(mountName.ToU8Span(), ref outputFileSystem.Ref());
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

        private Result MountSaveDataDirectory(ulong programId, string mountName)
        {
            U8Span mountNameu8 = mountName.ToU8Span();

            if (!_horizonClient.Fs.IsMounted(mountNameu8))
            {
                return _horizonClient.Fs.MountSaveData(mountNameu8, ConvertProgramIdToApplicationId(programId), _userId);
            }

            return Result.Success;
        }

        private Result UnmountDirectory(ulong programId, string mountName)
        {
            U8Span mountNameu8 = mountName.ToU8Span();

            if (_horizonClient.Fs.IsMounted(mountNameu8))
            {
                _horizonClient.Fs.Unmount(mountNameu8);
            }

            return Result.Success;
        }

        private Result OpenSaveDataIteratorAndReadSaveData(string mountName, ApplicationData application, out SaveDataInfo saveDataInfo)
        {
            Result getSvDataIteratorResult = GetSaveDataIterator(out saveDataInfo, application);
            if (getSvDataIteratorResult.IsFailure()) return getSvDataIteratorResult;

            Result mountSvDataResult = MountSaveDataDirectory(saveDataInfo.ProgramId.Value, mountName);
            if (mountSvDataResult.IsFailure()) return mountSvDataResult;

            UniqueRef<SaveDataIterator> saveDataIterator = new UniqueRef<SaveDataIterator>();
            Result openSvDataIteratorResult = _horizonClient.Fs.OpenSaveDataIterator(ref saveDataIterator, SaveDataSpaceId.User);
            if (openSvDataIteratorResult.IsFailure()) return openSvDataIteratorResult;

            Result readSvDataInfoResult = saveDataIterator.Get.ReadSaveDataInfo(out long readCount, new Span<SaveDataInfo>(ref saveDataInfo));
            if (readSvDataInfoResult.IsFailure()) return readSvDataInfoResult;

            return Result.Success;
        }

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

        private LibHac.Ncm.ApplicationId ConvertProgramIdToApplicationId(ulong programId)
        {
            return new LibHac.Ncm.ApplicationId(programId);
        }

        private async Task<string> GetAndPrepareBackupPath(MainWindow mainWindow)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog()
            {
                Title = LocaleManager.Instance["CreateZipFileDialogTitle"],
                InitialFileName = "ryujinx_savedata_backup"
            };

            string zipPath = await saveFileDialog.ShowAsync(mainWindow);

            if (File.Exists(zipPath))
            {
                File.Delete(zipPath);
            }

            return zipPath;
        }
    }
}