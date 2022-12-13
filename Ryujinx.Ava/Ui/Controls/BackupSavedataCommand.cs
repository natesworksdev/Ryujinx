using Avalonia.Controls;
using Avalonia.Logging;
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
            CreateBackupZip(await OpenFolderDialog());
        }

        private async Task<string> OpenFolderDialog()
        {
            OpenFolderDialog dialog = new()
            {
                Title = LocaleManager.Instance["OpenFolderDialogTitle"]
            };

            return await dialog.ShowAsync(parentControl.VisualRoot as MainWindow);
        }

        private void CreateBackupZip(string directoryPath)
        {
            if (!string.IsNullOrWhiteSpace(directoryPath) && Directory.Exists(directoryPath))
            {
                string saveDir = Path.Combine(AppDataManager.BaseDirPath, AppDataManager.DefaultNandDir, "user", "save");

                string zipFolderPath = Path.Combine(directoryPath, "Ryujinx_backup.zip");

                Logger.Info.Value.Print(LogClass.Application, $"Start creating backup...", nameof(BackupSavedataCommand));

                if (File.Exists(zipFolderPath))
                {
                    File.Delete(zipFolderPath);
                }
                
                ZipFile.CreateFromDirectory(saveDir, zipFolderPath);

                Logger.Info.Value.Print(LogClass.Application, $"Backup done. Zip is locate under {directoryPath}", nameof(BackupSavedataCommand));
            }
        }
    }
}