using Avalonia.Controls;
using Ryujinx.Ava.Common.Locale;
using Ryujinx.Ava.Ui.Windows;
using Ryujinx.Common.Configuration;
using Ryujinx.Common.Logging;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Path = System.IO.Path;

namespace Ryujinx.Ava.Ui.Controls
{
    internal class RestoreSavedataCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;

        private readonly IControl parentControl;

        public RestoreSavedataCommand(IControl parentControl)
        {
            this.parentControl = parentControl;
        }

        private async Task<bool> ShowConditionMessage()
        {
            return await ContentDialogHelper.CreateChoiceDialog("Restore Backup",
               "You have to start every game at least once to create a save directory for the game before you can Restore the backup save data!",
               "Do you want to continue?");
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            RestoreSavedataBackup();
        }

        public async void RestoreSavedataBackup()
        {
            if (!(await ShowConditionMessage())) return;

            string[] backupZipFiles = await ShowFolderDialog();

            ExtractBackupToSaveDirectory(backupZipFiles);

            Logger.Info.Value.Print(LogClass.Application, $"Done extracting savedata backup!", nameof(RestoreSavedataCommand));
        }

        private async Task<string[]> ShowFolderDialog()
        {
            OpenFileDialog dialog = new()
            {
                Title = LocaleManager.Instance["OpenFileDialogTitle"],
                AllowMultiple = false,
            };

            return await dialog.ShowAsync(parentControl.VisualRoot as MainWindow);
        }

        private void ExtractBackupToSaveDirectory(string[] backupZipFiles)
        {
            if (!string.IsNullOrWhiteSpace(backupZipFiles.First()) && File.Exists(backupZipFiles.First()))
            {
                string tempZipExtractionPath = Path.GetTempPath();
                ZipFile.ExtractToDirectory(backupZipFiles.First(), tempZipExtractionPath, true);

                Logger.Info.Value.Print(LogClass.Application, $"Extracted Backup zip to temp path: {tempZipExtractionPath}", nameof(RestoreSavedataCommand));

                string saveDir = Path.Combine(AppDataManager.BaseDirPath, AppDataManager.DefaultNandDir, "user", "save");
                ReplaceSavedataFilesWithBackupSaveFiles(Directory.GetDirectories(tempZipExtractionPath), saveDir);
            }
        }

        private void ReplaceSavedataFilesWithBackupSaveFiles(string[] backupSavedataPath, string saveDirectory)
        {
            //All current save files for later replacement
            string[] userSaveFiles = GetSaveFilesInAllSubDirectories(saveDirectory);

            //Loops through every Title save folder and replaces all user save data files with the savedata files inside the extracted backup folder
            //Logic to decide wich file is replaces is based on der filename and parent directory
            foreach (string[] backupSaveFiles in backupSavedataPath.Select(GetSaveFilesInAllSubDirectories))
            {
                foreach (string backupSaveFile in backupSaveFiles)
                {
                    foreach (string userSaveFile in GetSaveFilesWithSameNameAndParentDir(userSaveFiles, backupSaveFile))
                    {
                        try
                        {
                            File.Copy(backupSaveFile, userSaveFile, true);
                            Logger.Info.Value.Print(LogClass.Application, $"Copied Savedata {backupSaveFile} to {userSaveFile}", nameof(RestoreSavedataCommand));
                        }
                        catch (Exception)
                        {
                            Logger.Error.Value.Print(LogClass.Application, $"Could not copy Savedata {backupSaveFile} to {userSaveFile}", nameof(RestoreSavedataCommand));
                        }
                    }
                }
            }
        }

        private string[] GetSaveFilesWithSameNameAndParentDir(string[] userSaveFiles, string backupSaveFile)
        {
            return userSaveFiles.Where(sf => Path.GetFileName(sf) == Path.GetFileName(backupSaveFile) && Directory.GetParent(sf).Name == Directory.GetParent(backupSaveFile).Name).ToArray();
        }

        private string[] GetSaveFilesInAllSubDirectories(string rootDirectory)
        {
            string[] unnecessarySaveFiles = { ".lock", "ExtraData0", "ExtraData1" };

            return Directory.GetFiles(rootDirectory, "*", SearchOption.AllDirectories).Where(file => !unnecessarySaveFiles.Any(usf => file.EndsWith(usf))).ToArray();
        }
    }
}