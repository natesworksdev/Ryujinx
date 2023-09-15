using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using LibHac.Fs;
using LibHac.Tools.FsSystem.NcaUtils;
using Ryujinx.Ava.Common;
using Ryujinx.Ava.Common.Locale;
using Ryujinx.Ava.UI.Helpers;
using Ryujinx.Ava.UI.ViewModels;
using Ryujinx.Ava.UI.Windows;
using Ryujinx.Common.Configuration;
using Ryujinx.HLE.FileSystem;
using Ryujinx.HLE.HOS;
using Ryujinx.Ui.App.Common;
using Ryujinx.Ui.Common.Helper;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using Path = System.IO.Path;

namespace Ryujinx.Ava.UI.Controls
{
    public class ApplicationContextMenu : MenuFlyout
    {
        public ApplicationContextMenu()
        {
            InitializeComponent();
        }
        private ApplicationLibrary _applicationLibrary;
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public void ToggleFavorite_Click(object sender, RoutedEventArgs args)
        {
            var viewModel = (sender as MenuItem)?.DataContext as MainWindowViewModel;

            if (viewModel?.SelectedApplication != null)
            {
                viewModel.SelectedApplication.Favorite = !viewModel.SelectedApplication.Favorite;

                ApplicationLibrary.LoadAndSaveMetaData(viewModel.SelectedApplication.TitleId, appMetadata =>
                {
                    appMetadata.Favorite = viewModel.SelectedApplication.Favorite;
                });

                viewModel.RefreshView();
            }
        }

        public void OpenUserSaveDirectory_Click(object sender, RoutedEventArgs args)
        {
            if (sender is MenuItem { DataContext: MainWindowViewModel viewModel })
            {
                OpenSaveDirectory(viewModel, SaveDataType.Account, userId: new UserId((ulong)viewModel.AccountManager.LastOpenedUser.UserId.High, (ulong)viewModel.AccountManager.LastOpenedUser.UserId.Low));
            }
        }

        public void OpenDeviceSaveDirectory_Click(object sender, RoutedEventArgs args)
        {
            var viewModel = (sender as MenuItem)?.DataContext as MainWindowViewModel;

            OpenSaveDirectory(viewModel, SaveDataType.Device, userId: default);
        }

        public void OpenBcatSaveDirectory_Click(object sender, RoutedEventArgs args)
        {
            var viewModel = (sender as MenuItem)?.DataContext as MainWindowViewModel;

            OpenSaveDirectory(viewModel, SaveDataType.Bcat, userId: default);
        }

        private static void OpenSaveDirectory(MainWindowViewModel viewModel, SaveDataType saveDataType, UserId userId)
        {
            if (viewModel?.SelectedApplication != null)
            {
                if (!ulong.TryParse(viewModel.SelectedApplication.TitleId, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ulong titleIdNumber))
                {
                    Dispatcher.UIThread.InvokeAsync(async () =>
                    {
                        await ContentDialogHelper.CreateErrorDialog(LocaleManager.Instance[LocaleKeys.DialogRyujinxErrorMessage], LocaleManager.Instance[LocaleKeys.DialogInvalidTitleIdErrorMessage]);
                    });

                    return;
                }

                var saveDataFilter = SaveDataFilter.Make(titleIdNumber, saveDataType, userId, saveDataId: default, index: default);

                ApplicationHelper.OpenSaveDir(in saveDataFilter, titleIdNumber, viewModel.SelectedApplication.ControlHolder, viewModel.SelectedApplication.TitleName);
            }
        }

        public async void OpenTitleUpdateManager_Click(object sender, RoutedEventArgs args)
        {
            var viewModel = (sender as MenuItem)?.DataContext as MainWindowViewModel;

            if (viewModel?.SelectedApplication != null)
            {
                await TitleUpdateWindow.Show(viewModel.VirtualFileSystem, ulong.Parse(viewModel.SelectedApplication.TitleId, NumberStyles.HexNumber), viewModel.SelectedApplication.TitleName);
            }
        }

        public async void OpenDownloadableContentManager_Click(object sender, RoutedEventArgs args)
        {
            var viewModel = (sender as MenuItem)?.DataContext as MainWindowViewModel;

            if (viewModel?.SelectedApplication != null)
            {
                await DownloadableContentManagerWindow.Show(viewModel.VirtualFileSystem, ulong.Parse(viewModel.SelectedApplication.TitleId, NumberStyles.HexNumber), viewModel.SelectedApplication.TitleName);
            }
        }

        public async void OpenCheatManager_Click(object sender, RoutedEventArgs args)
        {
            var viewModel = (sender as MenuItem)?.DataContext as MainWindowViewModel;

            if (viewModel?.SelectedApplication != null)
            {
                await new CheatWindow(
                    viewModel.VirtualFileSystem,
                    viewModel.SelectedApplication.TitleId,
                    viewModel.SelectedApplication.TitleName,
                    viewModel.SelectedApplication.Path).ShowDialog(viewModel.TopLevel as Window);
            }
        }

        public void OpenModsDirectory_Click(object sender, RoutedEventArgs args)
        {
            var viewModel = (sender as MenuItem)?.DataContext as MainWindowViewModel;

            if (viewModel?.SelectedApplication != null)
            {
                string modsBasePath = ModLoader.GetModsBasePath();
                string titleModsPath = ModLoader.GetTitleDir(modsBasePath, viewModel.SelectedApplication.TitleId);

                OpenHelper.OpenFolder(titleModsPath);
            }
        }

        public void OpenSdModsDirectory_Click(object sender, RoutedEventArgs args)
        {
            var viewModel = (sender as MenuItem)?.DataContext as MainWindowViewModel;

            if (viewModel?.SelectedApplication != null)
            {
                string sdModsBasePath = ModLoader.GetSdModsBasePath();
                string titleModsPath = ModLoader.GetTitleDir(sdModsBasePath, viewModel.SelectedApplication.TitleId);

                OpenHelper.OpenFolder(titleModsPath);
            }
        }

        public async void PurgePtcCache_Click(object sender, RoutedEventArgs args)
        {
            var viewModel = (sender as MenuItem)?.DataContext as MainWindowViewModel;

            if (viewModel?.SelectedApplication != null)
            {
                UserResult result = await ContentDialogHelper.CreateConfirmationDialog(LocaleManager.Instance[LocaleKeys.DialogWarning],
                                                                                       LocaleManager.Instance.UpdateAndGetDynamicValue(LocaleKeys.DialogPPTCDeletionMessage, viewModel.SelectedApplication.TitleName),
                                                                                       LocaleManager.Instance[LocaleKeys.InputDialogYes],
                                                                                       LocaleManager.Instance[LocaleKeys.InputDialogNo],
                                                                                       LocaleManager.Instance[LocaleKeys.RyujinxConfirm]);

                if (result == UserResult.Yes)
                {
                    DirectoryInfo mainDir = new(Path.Combine(AppDataManager.GamesDirPath, viewModel.SelectedApplication.TitleId, "cache", "cpu", "0"));
                    DirectoryInfo backupDir = new(Path.Combine(AppDataManager.GamesDirPath, viewModel.SelectedApplication.TitleId, "cache", "cpu", "1"));

                    List<FileInfo> cacheFiles = new();

                    if (mainDir.Exists)
                    {
                        cacheFiles.AddRange(mainDir.EnumerateFiles("*.cache"));
                    }

                    if (backupDir.Exists)
                    {
                        cacheFiles.AddRange(backupDir.EnumerateFiles("*.cache"));
                    }

                    if (cacheFiles.Count > 0)
                    {
                        foreach (FileInfo file in cacheFiles)
                        {
                            try
                            {
                                file.Delete();
                            }
                            catch (Exception ex)
                            {
                                await ContentDialogHelper.CreateErrorDialog(LocaleManager.Instance.UpdateAndGetDynamicValue(LocaleKeys.DialogPPTCDeletionErrorMessage, file.Name, ex));
                            }
                        }
                    }
                }
            }
        }

        public async void PurgeShaderCache_Click(object sender, RoutedEventArgs args)
        {
            var viewModel = (sender as MenuItem)?.DataContext as MainWindowViewModel;

            if (viewModel?.SelectedApplication != null)
            {
                UserResult result = await ContentDialogHelper.CreateConfirmationDialog(LocaleManager.Instance[LocaleKeys.DialogWarning],
                                                                                       LocaleManager.Instance.UpdateAndGetDynamicValue(LocaleKeys.DialogShaderDeletionMessage, viewModel.SelectedApplication.TitleName),
                                                                                       LocaleManager.Instance[LocaleKeys.InputDialogYes],
                                                                                       LocaleManager.Instance[LocaleKeys.InputDialogNo],
                                                                                       LocaleManager.Instance[LocaleKeys.RyujinxConfirm]);

                if (result == UserResult.Yes)
                {
                    DirectoryInfo shaderCacheDir = new(Path.Combine(AppDataManager.GamesDirPath, viewModel.SelectedApplication.TitleId, "cache", "shader"));

                    List<DirectoryInfo> oldCacheDirectories = new();
                    List<FileInfo> newCacheFiles = new();

                    if (shaderCacheDir.Exists)
                    {
                        oldCacheDirectories.AddRange(shaderCacheDir.EnumerateDirectories("*"));
                        newCacheFiles.AddRange(shaderCacheDir.GetFiles("*.toc"));
                        newCacheFiles.AddRange(shaderCacheDir.GetFiles("*.data"));
                    }

                    if ((oldCacheDirectories.Count > 0 || newCacheFiles.Count > 0))
                    {
                        foreach (DirectoryInfo directory in oldCacheDirectories)
                        {
                            try
                            {
                                directory.Delete(true);
                            }
                            catch (Exception ex)
                            {
                                await ContentDialogHelper.CreateErrorDialog(LocaleManager.Instance.UpdateAndGetDynamicValue(LocaleKeys.DialogPPTCDeletionErrorMessage, directory.Name, ex));
                            }
                        }

                        foreach (FileInfo file in newCacheFiles)
                        {
                            try
                            {
                                file.Delete();
                            }
                            catch (Exception ex)
                            {
                                await ContentDialogHelper.CreateErrorDialog(LocaleManager.Instance.UpdateAndGetDynamicValue(LocaleKeys.ShaderCachePurgeError, file.Name, ex));
                            }
                        }
                    }
                }
            }
        }

        public void OpenPtcDirectory_Click(object sender, RoutedEventArgs args)
        {
            var viewModel = (sender as MenuItem)?.DataContext as MainWindowViewModel;

            if (viewModel?.SelectedApplication != null)
            {
                string ptcDir = Path.Combine(AppDataManager.GamesDirPath, viewModel.SelectedApplication.TitleId, "cache", "cpu");
                string mainDir = Path.Combine(ptcDir, "0");
                string backupDir = Path.Combine(ptcDir, "1");

                if (!Directory.Exists(ptcDir))
                {
                    Directory.CreateDirectory(ptcDir);
                    Directory.CreateDirectory(mainDir);
                    Directory.CreateDirectory(backupDir);
                }

                OpenHelper.OpenFolder(ptcDir);
            }
        }

        public void OpenShaderCacheDirectory_Click(object sender, RoutedEventArgs args)
        {
            var viewModel = (sender as MenuItem)?.DataContext as MainWindowViewModel;

            if (viewModel?.SelectedApplication != null)
            {
                string shaderCacheDir = Path.Combine(AppDataManager.GamesDirPath, viewModel.SelectedApplication.TitleId, "cache", "shader");

                if (!Directory.Exists(shaderCacheDir))
                {
                    Directory.CreateDirectory(shaderCacheDir);
                }

                OpenHelper.OpenFolder(shaderCacheDir);
            }
        }

        public async void ExtractApplicationExeFs_Click(object sender, RoutedEventArgs args)
        {
            var viewModel = (sender as MenuItem)?.DataContext as MainWindowViewModel;

            if (viewModel?.SelectedApplication != null)
            {
                await ApplicationHelper.ExtractSection(
                    viewModel.StorageProvider,
                    NcaSectionType.Code,
                    viewModel.SelectedApplication.Path,
                    viewModel.SelectedApplication.TitleName);
            }
        }

        public async void ExtractApplicationRomFs_Click(object sender, RoutedEventArgs args)
        {
            var viewModel = (sender as MenuItem)?.DataContext as MainWindowViewModel;

            if (viewModel?.SelectedApplication != null)
            {
                await ApplicationHelper.ExtractSection(
                    viewModel.StorageProvider,
                    NcaSectionType.Data,
                    viewModel.SelectedApplication.Path,
                    viewModel.SelectedApplication.TitleName);
            }
        }

        public async void ExtractApplicationLogo_Click(object sender, RoutedEventArgs args)
        {
            var viewModel = (sender as MenuItem)?.DataContext as MainWindowViewModel;

            if (viewModel?.SelectedApplication != null)
            {
                await ApplicationHelper.ExtractSection(
                    viewModel.StorageProvider,
                    NcaSectionType.Logo,
                    viewModel.SelectedApplication.Path,
                    viewModel.SelectedApplication.TitleName);
            }
        }

        public void RunApplication_Click(object sender, RoutedEventArgs args)
        {
            var viewModel = (sender as MenuItem)?.DataContext as MainWindowViewModel;

            if (viewModel?.SelectedApplication != null)
            {
                viewModel.LoadApplication(viewModel.SelectedApplication.Path);
            }
        }

        public void CreateDesktopShortcutMenuItem_Click(object sender, RoutedEventArgs args)
        {
            var viewModel = (sender as MenuItem)?.DataContext as MainWindowViewModel;

            // Get the path to the user's desktop
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            // Get the path to the current executable
            string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;

            string invalidChars = new string(System.IO.Path.GetInvalidFileNameChars()) + new string(System.IO.Path.GetInvalidPathChars());
            string titleName = viewModel.SelectedApplication.TitleName;

            foreach (char c in invalidChars)
            {
                titleName = titleName.Replace(c.ToString(), " ");
            }

            //shortcut final path
            string shortcutLocation = System.IO.Path.Combine(desktopPath, titleName + ".lnk");

            _applicationLibrary = new ApplicationLibrary(viewModel.VirtualFileSystem);
            byte[] imagemembyte = _applicationLibrary.GetApplicationIcon(viewModel.SelectedApplication.Path);

            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string ryujinxPath = System.IO.Path.Combine(appDataPath, "Ryujinx");
            string icoDirectoryPath = System.IO.Path.Combine(ryujinxPath, "ico");

            // Checks if the directory exists
            if (!Directory.Exists(icoDirectoryPath))
            {
                // Create the directory if it does not exist
                Directory.CreateDirectory(icoDirectoryPath);
            }

            string icoFilePath = System.IO.Path.Combine(icoDirectoryPath, titleName + ".ico");
            //string icoFilePath = System.IO.Path.Combine(desktopPath, titleName + ".ico");

            // Convert jpeg to ico
            ImageCodecInfo[] imf = ImageCodecInfo.GetImageEncoders();
            using (MemoryStream ms = new MemoryStream(imagemembyte))
            {
                Bitmap bitmap = new Bitmap(ms);
                using (FileStream stream = File.OpenWrite(icoFilePath))
                {
                    System.Drawing.Icon.FromHandle(bitmap.GetHicon()).Save(stream);
                }
            }

            IWshRuntimeLibrary.WshShell wsh = new IWshRuntimeLibrary.WshShell();
            IWshRuntimeLibrary.IWshShortcut shortcut = wsh.CreateShortcut(shortcutLocation) as IWshRuntimeLibrary.IWshShortcut;

            shortcut.Description = $"Start {titleName} with the Ryujix Emulator";   // The description of the shortcut
            shortcut.IconLocation = icoFilePath;                // The icon of the shortcut
            shortcut.TargetPath = exePath;                      // The path of the file that will launch when the shortcut is run
            shortcut.Arguments = $"\"{viewModel.SelectedApplication.Path}\"";       // Defines the arguments for the shortcut, which is the game file path enclosed in double quotes
            shortcut.Save();                                    // Save the shortcut

            ContentDialogHelper.CreateWarningDialog("Create Desktop Shortcut", "Shortcut was created for " + titleName);            
        }
    }
}
