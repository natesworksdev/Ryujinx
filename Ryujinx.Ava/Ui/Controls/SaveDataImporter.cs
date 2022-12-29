using Avalonia.Controls;
using LibHac;
using Ryujinx.Ava.Common;
using Ryujinx.Ava.Common.Locale;
using Ryujinx.Ava.Ui.Windows;
using Ryujinx.Common.Logging;
using Ryujinx.HLE.FileSystem;
using Ryujinx.Ui.App.Common;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using UserId = LibHac.Fs.UserId;
using UserProfile = Ryujinx.Ava.Ui.Models.UserProfile;

namespace Ryujinx.Ava.Ui.Controls
{
    internal class SaveDataImporter
    {
        private readonly SaveDataFileManager saveDataFileManager;

        public SaveDataImporter(List<ApplicationData> applications, UserProfile userProfile, HorizonClient horizonClient, VirtualFileSystem virtualFileSystem)
        {
            UserId userId = new UserId(
               ulong.Parse(userProfile.UserId.High.ToString(), NumberStyles.HexNumber),
               ulong.Parse(userProfile.UserId.Low.ToString(), NumberStyles.HexNumber));

            saveDataFileManager = new SaveDataFileManager(applications, userProfile, horizonClient, virtualFileSystem, userId);
        }

        public async void RestoreSavedataBackup(MainWindow mainWindow)
        {
            string[] backupZipFiles = await ShowFolderDialog(mainWindow);

            //Single because we set AllowMultiple = False in folder dialog options and want only one backup file. => Our export
            saveDataFileManager.RestoreSavedataBackup(backupZipFiles.Single());

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
    }
}