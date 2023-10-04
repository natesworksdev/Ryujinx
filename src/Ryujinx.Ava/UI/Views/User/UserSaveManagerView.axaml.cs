using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;
using DynamicData;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Navigation;
using LibHac;
using LibHac.Common;
using LibHac.Fs;
using LibHac.Fs.Shim;
using Ryujinx.Ava.Common;
using Ryujinx.Ava.Common.Locale;
using Ryujinx.Ava.Common.SaveManager;
using Ryujinx.Ava.UI.Controls;
using Ryujinx.Ava.UI.Helpers;
using Ryujinx.Ava.UI.Models;
using Ryujinx.Ava.UI.ViewModels;
using Ryujinx.HLE.FileSystem;
using Ryujinx.HLE.HOS.Services.Account.Acc;
using Ryujinx.Ui.Common.Helper;
using Ryujinx.Ui.Common.SaveManager;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Button = Avalonia.Controls.Button;
using UserId = LibHac.Fs.UserId;

namespace Ryujinx.Ava.UI.Views.User
{
    public partial class UserSaveManagerView : UserControl
    {
        internal UserSaveManagerViewModel ViewModel { get; private set; }

        private AccountManager _accountManager;
        private HorizonClient _horizonClient;
        private NavigationDialogHost _parent;
        private SaveManager _saveManager;

        public UserSaveManagerView()
        {
            InitializeComponent();
            AddHandler(Frame.NavigatedToEvent, (s, e) =>
            {
                NavigatedTo(e);
            }, RoutingStrategies.Direct);
        }

        private void NavigatedTo(NavigationEventArgs arg)
        {
            if (!Program.PreviewerDetached)
            {
                return;
            }

            switch (arg.NavigationMode)
            {
                case NavigationMode.New:
                    (_parent, _accountManager, _horizonClient, _) =
                        ((NavigationDialogHost parent, AccountManager accountManager, HorizonClient client, VirtualFileSystem virtualFileSystem))arg.Parameter;

                    _saveManager = new SaveManager(_horizonClient, _accountManager);
                    _saveManager.BackupProgressUpdated += BackupManager_ProgressUpdate;
                    _saveManager.BackupImportSave += BackupManager_ImportSave;

                    break;
            }
            DataContext = ViewModel = new UserSaveManagerViewModel(_accountManager);
            ((ContentDialog)_parent.Parent).Title = $"{LocaleManager.Instance[LocaleKeys.UserProfileWindowTitle]} - {ViewModel.SaveManagerHeading}";

            _ = Task.Run(LoadSaves);
        }

        public void LoadSaves()
        {
            ViewModel.Saves.Clear();
            var saves = new ObservableCollection<SaveModel>();
            var saveDataFilter = SaveDataFilter.Make(
                default,
                SaveDataType.Account,
                new UserId((ulong)_accountManager.LastOpenedUser.UserId.High, (ulong)_accountManager.LastOpenedUser.UserId.Low),
                default,
                default);

            using var saveDataIterator = new UniqueRef<SaveDataIterator>();

            _horizonClient.Fs.OpenSaveDataIterator(ref saveDataIterator.Ref, SaveDataSpaceId.User, in saveDataFilter).ThrowIfFailure();

            Span<SaveDataInfo> saveDataInfo = stackalloc SaveDataInfo[10];

            while (true)
            {
                saveDataIterator.Get.ReadSaveDataInfo(out long readCount, saveDataInfo).ThrowIfFailure();

                if (readCount == 0)
                {
                    break;
                }

                for (int i = 0; i < readCount; i++)
                {
                    var save = saveDataInfo[i];
                    if (save.ProgramId.Value != 0)
                    {
                        var saveModel = new SaveModel(save);
                        saves.Add(saveModel);
                    }
                }
            }

            Dispatcher.UIThread.Post(() =>
            {
                ViewModel.Saves = saves;
                ViewModel.Sort();
            });
        }

        private void GoBack(object sender, RoutedEventArgs e)
        {
            if (ViewModel.IsGoBackEnabled)
            {
                _parent?.GoBack();
            }
        }

        private void OpenLocation(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                if (button.DataContext is SaveModel saveModel)
                {
                    ApplicationHelper.OpenSaveDir(saveModel.SaveId);
                }
            }
        }

        private async void Delete(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                if (button.DataContext is SaveModel saveModel)
                {
                    var result = await ContentDialogHelper.CreateConfirmationDialog(
                        LocaleManager.Instance[LocaleKeys.DeleteUserSave],
                        LocaleManager.Instance[LocaleKeys.IrreversibleActionNote],
                        LocaleManager.Instance[LocaleKeys.InputDialogYes],
                        LocaleManager.Instance[LocaleKeys.InputDialogNo], "");

                    if (result == UserResult.Yes)
                    {
                        _horizonClient.Fs.DeleteSaveData(SaveDataSpaceId.User, saveModel.SaveId);
                        ViewModel.Saves.Remove(saveModel);
                        ViewModel.Sort();
                    }
                }
            }
        }

        private async void GenerateProfileSaveBackup(object sender, RoutedEventArgs e)
        {
            var window = ((TopLevel)_parent.GetVisualRoot()) as Window;
            var currDate = DateTime.UtcNow.ToString("yyyy-MM-dd");
            var profileName = _accountManager.LastOpenedUser.Name;
            var fileName = $"{profileName}_{currDate}_saves";

            var file = await window.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = LocaleManager.Instance[LocaleKeys.SaveManagerChooseBackupFolderTitle],
                DefaultExtension = "zip",
                SuggestedFileName = fileName,
                FileTypeChoices = new List<FilePickerFileType>
                {
                    new("zip")
                    {
                        Patterns = new[] { "*.zip" },
                        AppleUniformTypeIdentifiers = new[] { "public.zip-archive" },
                        MimeTypes = new[] { "application/zip" }
                    }
                }
            });

            // Disable the user from doing anything until we complete
            ViewModel.IsGoBackEnabled = false;

            try
            {
                // Could potentially seed with existing saves already enumerated but we still need bcat and device data
                var result = await _saveManager.BackupUserSaveDataToZip(
                    _accountManager.LastOpenedUser.UserId.ToLibHacUserId(),
                    file.Path);

                var notificationType = result.DidFail
                    ? NotificationType.Error
                    : NotificationType.Success;

                var message = result.DidFail
                    ? LocaleManager.Instance[LocaleKeys.SaveManagerBackupFailed]
                    : LocaleManager.Instance[LocaleKeys.SaveManagerBackupComplete];

                NotificationHelper.Show(
                    LocaleManager.Instance[LocaleKeys.NotificationBackupTitle],
                    message,
                    notificationType);
            }
            catch (Exception ex)
            {
                await ContentDialogHelper.CreateErrorDialog($"Failed to generate backup - {ex.Message}");
            }
            finally
            {
                ViewModel.LoadingBarData = new();
                ViewModel.IsGoBackEnabled = true;
            }
        }

        private async void ImportSaveBackup(object sender, RoutedEventArgs e)
        {
            bool userConfirmation = await ContentDialogHelper.CreateChoiceDialog(
                LocaleManager.Instance[LocaleKeys.SaveManagerConfirmRestoreTitle],
                LocaleManager.Instance[LocaleKeys.SaveManagerChooseRestoreZipPrimaryMessage],
                LocaleManager.Instance[LocaleKeys.SaveManagerChooseRestoreZipSecondaryMessage],
                LocaleKeys.SaveMangerRestoreUserConfirm,
                LocaleKeys.SaveMangerRestoreUserCancel);

            if (!userConfirmation)
            {
                return;
            }

            var window = ((TopLevel)_parent.GetVisualRoot()) as Window;

            var fileResult = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = LocaleManager.Instance[LocaleKeys.SaveManagerChooseRestoreZipTitle],
                AllowMultiple = false,
                FileTypeFilter = new List<FilePickerFileType>
                {
                    new("zip")
                    {
                        Patterns = new[] { "*.zip" },
                        AppleUniformTypeIdentifiers = new[] { "public.zip-archive" },
                        MimeTypes = new[] { "application/zip" }
                    }
                }
            });

            if (fileResult.Count <= 0)
            {
                return;
            }

            var saveBackupZip = fileResult[0].Path.LocalPath;

            // Disable the user from doing anything until we complete
            ViewModel.IsGoBackEnabled = false;

            try
            {
                // Could potentially seed with existing saves already enumerated but we still need bcat and device data
                var result = await _saveManager.RestoreUserSaveDataFromZip(
                    _accountManager.LastOpenedUser.UserId.ToLibHacUserId(),
                    saveBackupZip);

                var notificationType = result.DidFail
                    ? NotificationType.Error
                    : NotificationType.Success;

                var message = result.DidFail
                    ? LocaleManager.Instance[LocaleKeys.SaveManagerRestoreFailed]
                    : LocaleManager.Instance[LocaleKeys.SaveManagerRestoreComplete];

                if (!string.IsNullOrWhiteSpace(ViewModel.Search))
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        ViewModel.Sort();
                    });
                }

                NotificationHelper.Show(
                    LocaleManager.Instance[LocaleKeys.NotificationBackupTitle],
                    message,
                    notificationType);
            }
            catch (Exception ex)
            {
                await ContentDialogHelper.CreateErrorDialog($"Failed to import backup saves - {ex.Message}");
            }
            finally
            {
                ViewModel.LoadingBarData = new();
                ViewModel.IsGoBackEnabled = true;
            }
        }

        private void BackupManager_ProgressUpdate(object sender, LoadingBarEventArgs e)
        {
            Dispatcher.UIThread.Post(() =>
            {
                ViewModel.LoadingBarData = new()
                {
                    Curr = e.Curr,
                    Max = e.Max
                };
            });
        }

        private void BackupManager_ImportSave(object sender, ImportSaveEventArgs e)
        {
            var existingSave = ViewModel.Saves.FirstOrDefault(s => s.TitleId == e.SaveInfo.ProgramId);

            if (existingSave == default)
            {
                ViewModel.AddNewSaveEntry(new SaveModel(e.SaveInfo));
            }
            else
            {
                ViewModel.Saves.Replace(existingSave, new SaveModel(e.SaveInfo));
            }
        }
    }
}
