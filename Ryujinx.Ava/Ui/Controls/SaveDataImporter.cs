using Avalonia.Controls;
using LibHac;
using Ryujinx.Ava.Common.Locale;
using Ryujinx.Ava.Ui.Models;
using Ryujinx.Ava.Ui.Windows;
using Ryujinx.Common.Configuration;
using Ryujinx.Common.Logging;
using Ryujinx.HLE.FileSystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Path = System.IO.Path;

namespace Ryujinx.Ava.Ui.Controls
{
    internal class SaveDataImporter
    {

        private readonly UserProfile _userProfile;
        private readonly HorizonClient _horizonClient;
        private readonly VirtualFileSystem _virtualFileSystem;

        private async Task<bool> ShowConditionMessage()
        {
            return await ContentDialogHelper.CreateChoiceDialog("Restore Backup",
               "You have to start every game at least once to create a save directory for the game before you can Restore the backup save data!",
               "Do you want to continue?");
        }

        public async void RestoreSavedataBackup(MainWindow mainWindow)
        {
            if (!(await ShowConditionMessage())) return;

            string[] backupZipFiles = await ShowFolderDialog(mainWindow);

            ExtractBackupToSaveDirectory(backupZipFiles);

            Logger.Info.Value.Print(LogClass.Application, $"Done extracting savedata backup!", nameof(SaveDataImporter));
        }

        private async Task<string[]> ShowFolderDialog(MainWindow mainWindow)
        {
            OpenFileDialog dialog = new()
            {
                Title = LocaleManager.Instance["OpenFileDialogTitle"],
                AllowMultiple = false,
                Filters = new List<FileDialogFilter>(new[] { new FileDialogFilter() { Extensions = new List<string>() { "zip" } } })
            };

            return await dialog.ShowAsync(mainWindow);
        }

        private Dictionary<string, string> GetTitleIdWithSavedataPath(string saveDirectoryPath)
        {
            Dictionary<string, string> titleIdWithSavePath = new Dictionary<string, string>();

            //Loop through all ExtraData0 files in the savedata directory and read the first 8 bytes to determine which game this belongs to
            foreach (var saveDataExtra0file in Directory.GetFiles(saveDirectoryPath, "ExtraData0*", SearchOption.AllDirectories))
            {
                try
                {
                    string hexValues = FlipHexBytes(new string(Convert.ToHexString(File.ReadAllBytes(saveDataExtra0file)).Substring(0, 16).Reverse().ToArray()));

                    if (!titleIdWithSavePath.ContainsKey(hexValues))
                    {
                        titleIdWithSavePath.Add(hexValues, saveDataExtra0file);
                    }
                }
                catch (Exception)
                {
                    Logger.Error.Value.Print(LogClass.Application, $"Could not extract hex from savedata file: {saveDataExtra0file}", nameof(SaveDataImporter));
                }
            }

            return titleIdWithSavePath;
        }

        private string FlipHexBytes(string hexString)
        {
            StringBuilder result = new StringBuilder();

            for (int i = 0; i <= hexString.Length - 2; i = i + 2)
            {
                result.Append(new StringBuilder(new string(hexString.Substring(i, 2).Reverse().ToArray())));
            }

            return result.ToString();
        }

        private void ExtractBackupToSaveDirectory(string[] backupZipFiles)
        {
            if (!string.IsNullOrWhiteSpace(backupZipFiles.First()) && File.Exists(backupZipFiles.First()))
            {
                string tempZipExtractionPath = Path.GetTempPath();
                ZipFile.ExtractToDirectory(backupZipFiles.First(), tempZipExtractionPath, true);

                Logger.Info.Value.Print(LogClass.Application, $"Extracted Backup zip to temp path: {tempZipExtractionPath}", nameof(SaveDataImporter));

                string saveDir = Path.Combine(AppDataManager.BaseDirPath, AppDataManager.DefaultNandDir, "user", "save");

                Dictionary<string, string> titleIdsAndSavePaths = GetTitleIdWithSavedataPath(saveDir);
                Dictionary<string, string> titleIdsAndBackupPaths = GetTitleIdWithSavedataPath(tempZipExtractionPath);

                ReplaceSavedataFiles(titleIdsAndSavePaths, titleIdsAndBackupPaths);
            }
        }

        private void ReplaceSavedataFiles(Dictionary<string, string> titleIdsWithSavePaths, Dictionary<string, string> titleIdsAndBackupPaths)
        {
            foreach (var titleIdAndBackupPath in titleIdsAndBackupPaths)
            {
                if (titleIdsWithSavePaths.ContainsKey(titleIdAndBackupPath.Key))
                {
                    try
                    {
                        Directory.Move(Directory.GetParent(titleIdAndBackupPath.Value).FullName, Directory.GetParent(titleIdsWithSavePaths[titleIdAndBackupPath.Key]).FullName);
                        Logger.Info.Value.Print(LogClass.Application, $"Copied Savedata {titleIdAndBackupPath.Value} to {titleIdsWithSavePaths[titleIdAndBackupPath.Key]}", nameof(SaveDataImporter));
                    }
                    catch (Exception)
                    {
                        Logger.Error.Value.Print(LogClass.Application, $"Could not copy Savedata {titleIdAndBackupPath.Value} to {titleIdsWithSavePaths[titleIdAndBackupPath.Key]}", nameof(SaveDataImporter));
                    }
                }
            }
        }
    }
}