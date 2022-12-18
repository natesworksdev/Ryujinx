using Avalonia.Controls;
using LibHac;
using LibHac.Fs;
using Ryujinx.Ava.Common;
using Ryujinx.Ava.Common.Locale;
using Ryujinx.Ava.Ui.Models;
using Ryujinx.Ava.Ui.Windows;
using Ryujinx.HLE.FileSystem;
using Ryujinx.Ui.App.Common;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using UserProfile = Ryujinx.Ava.Ui.Models.UserProfile;

namespace Ryujinx.Ava.Ui.Controls
{
    internal class SaveDataExporter
    {
        private readonly SaveDataFileManager saveDataFileManager;

        public SaveDataExporter(List<ApplicationData> applications, UserProfile userProfile, HorizonClient horizonClient, VirtualFileSystem virtualFileSystem)
        {
            UserId userId = new UserId(ulong.Parse(userProfile.UserId.High.ToString(), NumberStyles.HexNumber),
                                       ulong.Parse(userProfile.UserId.Low.ToString(), NumberStyles.HexNumber));

            saveDataFileManager = new SaveDataFileManager(applications, userProfile, horizonClient, virtualFileSystem, userId);
        }

        public async void SaveUserSaveDirectoryAsZip(MainWindow mainWindow, List<SaveModel> saves)
        {
            string backupFolder = await GetAndPrepareBackupPath(mainWindow);
            saveDataFileManager.SaveUserSaveDirectoryAsZip(backupFolder, saves);
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