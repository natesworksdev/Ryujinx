
using LibHac.Fs;
using Ryujinx.Ava.UI.ViewModels;
using Ryujinx.Ui.Common.Helper;
using Ryujinx.Ui.Common.SaveManager;
using System;
using System.Threading.Tasks;

namespace Ryujinx.Ava.Common.SaveManager
{
    public interface ISaveManager
    {
        public event EventHandler<LoadingBarEventArgs> BackupProgressUpdated;
        public event EventHandler<ImportSaveEventArgs> BackupImportSave;

        #region Backup
        public Task<BackupRequestOutcome> BackupUserSaveDataToZip(UserId userId,
            Uri savePath,
            SaveOptions saveOptions = SaveOptions.Default);

        public Task<BackupRequestOutcome> BackupUserTitleSaveDataToZip(UserId userId,
            ulong titleId,
            string location,
            SaveOptions saveOptions = SaveOptions.Default);
        #endregion

        #region Restore
        public Task<BackupRequestOutcome> RestoreUserSaveDataFromZip(UserId userId,
            string sourceDataPath);

        public Task<BackupRequestOutcome> RestoreUserTitleSaveFromZip(UserId userId,
            ulong titleId,
            string sourceDataPath);
        #endregion
    }
}
