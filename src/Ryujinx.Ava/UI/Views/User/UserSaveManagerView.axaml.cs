using Avalonia.Controls;
using Avalonia.Interactivity;
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
using Ryujinx.Ava.UI.Controls;
using Ryujinx.Ava.UI.Helpers;
using Ryujinx.Ava.UI.Models;
using Ryujinx.Ava.UI.ViewModels;
using Ryujinx.HLE.FileSystem;
using Ryujinx.HLE.HOS.Services.Account.Acc;
using Ryujinx.Ui.App.Common;
using Ryujinx.Ui.Common.Helper;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using UserId = LibHac.Fs.UserId;

namespace Ryujinx.Ava.UI.Views.User
{
    public partial class UserSaveManagerView : UserControl
    {
        internal UserSaveManagerViewModel ViewModel { get; private set; }

        private AccountManager _accountManager;
        private HorizonClient _horizonClient;
        private VirtualFileSystem _virtualFileSystem;
        private NavigationDialogHost _parent;
        private ApplicationLibrary _appLib;
        private BackupManager _backupManager;

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
                    var args = ((NavigationDialogHost parent, AccountManager accountManager, HorizonClient client, VirtualFileSystem virtualFileSystem, ApplicationLibrary appLib))arg.Parameter;
                    _accountManager = args.accountManager;
                    _horizonClient = args.client;
                    _virtualFileSystem = args.virtualFileSystem;
                    _appLib = args.appLib;

                    _backupManager = new BackupManager(_horizonClient, _appLib, _accountManager);
                    _backupManager.BackupProgressUpdated += BackupManager_ProgressUpdate;
                    _backupManager.BackupImportSave += BackupManager_ImportSave;

                    _parent = args.parent;
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
                programId: default,
                saveType: SaveDataType.Account,
                new UserId((ulong)_accountManager.LastOpenedUser.UserId.High, (ulong)_accountManager.LastOpenedUser.UserId.Low),
                saveDataId: default,
                index: default);

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
                        var saveModel = new SaveModel(save, _virtualFileSystem);
                        saves.Add(saveModel);
                    }
                }

                IServiceProvider serviceProvider = null;
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
            if (sender is Avalonia.Controls.Button button)
            {
                if (button.DataContext is SaveModel saveModel)
                {
                    ApplicationHelper.OpenSaveDir(saveModel.SaveId);
                }
            }
        }

        private async void Delete(object sender, RoutedEventArgs e)
        {
            if (sender is Avalonia.Controls.Button button)
            {
                if (button.DataContext is SaveModel saveModel)
                {
                    var result = await ContentDialogHelper.CreateConfirmationDialog(LocaleManager.Instance[LocaleKeys.DeleteUserSave],
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
            OpenFolderDialog dialog = new()
            {
                Title = "Choose Save Backup Folder", // TODO: localize
                // LocaleManager.Instance[LocaleKeys.UserProfileWindowTitle]
            };

            var backupDir = await dialog.ShowAsync(((TopLevel)_parent.GetVisualRoot()) as Window);
            if (string.IsNullOrWhiteSpace(backupDir))
            {
                return;
            }

            // Disable the user from doing anything until we complete
            ViewModel.IsGoBackEnabled = false;

            try
            {
                // Could potentially seed with existing saves already enumerated but we still need bcat and device data
                var result = await _backupManager.BackupUserSaveData(
                    userId: _accountManager.LastOpenedUser.UserId.ToLibHacUserId(),
                    location: backupDir,
                    saveOptions: SaveOptions.Default);

                if (result.DidFail)
                {
                    await ContentDialogHelper.CreateErrorDialog(result.Message);
                    return;
                }

                // TODO: generate notification on success
                OpenHelper.OpenFolder(backupDir);
                return;
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
            // TODO: Use locales // LocaleManager.Instance[LocaleKeys.DialogUpdaterCompleteMessage]
            bool userConfirmation = await ContentDialogHelper.CreateChoiceDialog("Confirm Restore",
                "The save data in the backup will overwrite your local save files. This action is irreversible. Consider using the backup option before you continue.",
                "Are you sure you want to continue?");

            if (!userConfirmation)
            {
                return;
            }

            OpenFileDialog dialog = new()
            {
                Title = "Choose Save Backup Zip", // TODO: localize
                // LocaleManager.Instance[LocaleKeys.UserProfileWindowTitle]
                AllowMultiple = false,
                Filters = {
                    new FileDialogFilter() {
                        Extensions = { "zip" },
                    }
                }
            };

            var saveBackupZip = await dialog.ShowAsync(((TopLevel)_parent.GetVisualRoot()) as Window);
            if (saveBackupZip is null || string.IsNullOrWhiteSpace(saveBackupZip[0]))
            {
                return;
            }

            // Disable the user from doing anything until we complete
            ViewModel.IsGoBackEnabled = false;

            try
            {
                // Could potentially seed with existing saves already enumerated but we still need bcat and device data
                var result = await _backupManager.LoadSaveData(
                    userId: _accountManager.LastOpenedUser.UserId.ToLibHacUserId(),
                    sourceDataPath: saveBackupZip[0]);

                if (result.DidFail)
                {
                    await ContentDialogHelper.CreateErrorDialog(result.Message);
                    return;
                }

                // refresh the save list so it reflects in the UI -- better yet, instead of a progress bar, show the save list populating in real time
                // TODO: generate notification on success?
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
                // TODO: fix this so we can just set the properties, will require a trigger of OnPropChange
                // observable?
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

            bool added = false;
            if (existingSave == default)
            {
                ViewModel.Saves.Add(new SaveModel(e.SaveInfo, _virtualFileSystem));
                added = true;
            }
            else
            {
                ViewModel.Saves.Replace(existingSave, new SaveModel(e.SaveInfo, _virtualFileSystem));
            }

            if (added)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    ViewModel.Sort();
                });
            }
        }
    }
}