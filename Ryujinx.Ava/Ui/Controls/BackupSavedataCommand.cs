
using Avalonia.Controls;
using Ryujinx.Ava.Common.Locale;
using Ryujinx.Ava.Ui.Windows;
using Ryujinx.Common.Configuration;
using Ryujinx.Common.Logging;
using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Windows.Input;
using Logger = Ryujinx.Common.Logging.Logger;
using Path = System.IO.Path;

namespace Ryujinx.Ava.Ui.Controls
{
    internal class BackupSavedataCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;

        private readonly IControl parentControl;

        public BackupSavedataCommand(IControl parentControl)
        {
            this.parentControl = parentControl;
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            SaveUserSaveDirectoryAsZip();
        }

        public async void SaveUserSaveDirectoryAsZip()
        {
            CreateBackupZip(await GetAndPrepareBackupPath());
        }

        private async Task<string> GetAndPrepareBackupPath()
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog()
            {
                Title = LocaleManager.Instance["CreateZipFileDialogTitle"],
                InitialFileName = "Ryujinx_backup.zip",
                Filters = new System.Collections.Generic.List<FileDialogFilter>(new[] { new FileDialogFilter() { Extensions = new System.Collections.Generic.List<string>() { "zip" } } })
            };

            string zipPath = await saveFileDialog.ShowAsync(parentControl.VisualRoot as MainWindow);

            if (File.Exists(zipPath))
            {
                File.Delete(zipPath);
            }

            return zipPath;
        }

        private void CreateBackupZip(string userBackupPath)
        {
            if (!string.IsNullOrWhiteSpace(userBackupPath) && Directory.Exists(Directory.GetParent(userBackupPath).FullName))
            {
                string saveDir = Path.Combine(AppDataManager.BaseDirPath, AppDataManager.DefaultNandDir, "user", "save");

                try
                {
                    Logger.Info.Value.Print(LogClass.Application, $"Start creating backup...", nameof(BackupSavedataCommand));

                    ZipFile.CreateFromDirectory(saveDir, userBackupPath);

                    Logger.Info.Value.Print(LogClass.Application, $"Backup done. Zip is locate under {userBackupPath}", nameof(BackupSavedataCommand));
                }
                catch (Exception)
                {
                    Logger.Error.Value.Print(LogClass.Application, $"Could not create backup zip file.", nameof(BackupSavedataCommand));
                }
            }
        }
    }
}