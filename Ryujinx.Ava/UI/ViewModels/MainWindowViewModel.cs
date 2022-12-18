using ARMeilleure.Translation.PTC;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using DynamicData;
using DynamicData.Binding;
using LibHac.Fs;
using LibHac.FsSystem;
using LibHac.Ncm;
using Ryujinx.Ava.Common;
using Ryujinx.Ava.Common.Locale;
using Ryujinx.Ava.Input;
using Ryujinx.Ava.UI.Controls;
using Ryujinx.Ava.UI.Helpers;
using Ryujinx.Ava.UI.Models;
using Ryujinx.Ava.UI.Windows;
using Ryujinx.Common;
using Ryujinx.Common.Configuration;
using Ryujinx.Common.Logging;
using Ryujinx.HLE;
using Ryujinx.HLE.FileSystem;
using Ryujinx.HLE.HOS;
using Ryujinx.HLE.HOS.Services.Account.Acc;
using Ryujinx.HLE.Ui;
using Ryujinx.Ui.App.Common;
using Ryujinx.Ui.Common;
using Ryujinx.Ui.Common.Configuration;
using Ryujinx.Ui.Common.Helper;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Path = System.IO.Path;
using ShaderCacheLoadingState = Ryujinx.Graphics.Gpu.Shader.ShaderCacheState;
using UserId = LibHac.Fs.UserId;

namespace Ryujinx.Ava.UI.ViewModels
{
    public class MainWindowViewModel : BaseModel
    {
        public const int HotKeyPressDelayMs = 500;

        private ObservableCollection<ApplicationData> _applications;
        private string _aspectStatusText;

        private string _loadHeading;
        private string _cacheLoadStatus;
        private string _searchText;
        private Timer _searchTimer;
        private string _dockedStatusText;
        private string _fifoStatusText;
        private string _gameStatusText;
        private string _volumeStatusText;
        private string _gpuStatusText;
        private bool _isAmiiboRequested;
        private bool _isGameRunning;
        private bool _isFullScreen;
        private int _progressMaximum;
        private int _progressValue;
        private long _lastFullscreenToggle = Environment.TickCount64;
        private bool _showLoadProgress;
        private bool _showMenuAndStatusBar = true;
        private bool _showStatusSeparator;
        private Brush _progressBarForegroundColor;
        private Brush _progressBarBackgroundColor;
        private Brush _vsyncColor;
        private byte[] _selectedIcon;
        private bool _isAppletMenuActive;
        private int _statusBarProgressMaximum;
        private int _statusBarProgressValue;
        private bool _isPaused;
        private bool _showContent = true;
        private bool _isLoadingIndeterminate = true;
        private bool _showAll;
        private string _lastScannedAmiiboId;
        private ReadOnlyObservableCollection<ApplicationData> _appsObservableList;

        public string TitleName { get; internal set; }
        internal AppHost AppHost { get; set; }

        public MainWindowViewModel()
        {
            Applications = new ObservableCollection<ApplicationData>();

            Applications.ToObservableChangeSet()
                .Filter(Filter)
                .Sort(GetComparer())
                .Bind(out _appsObservableList).AsObservableList();

            _rendererWaitEvent = new AutoResetEvent(false);

            if (Program.PreviewerDetached)
            {
                LoadConfigurableHotKeys();

                Volume = ConfigurationState.Instance.System.AudioVolume;
            }
        }

        public void Initialize(
            ContentManager contentManager,
            IStorageProvider storageProvider,
            ApplicationLibrary applicationLibrary,
            VirtualFileSystem virtualFileSystem,
            AccountManager accountManager,
            Ryujinx.Input.HLE.InputManager inputManager,
            UserChannelPersistence userChannelPersistence,
            LibHacHorizonManager libHacHorizonManager,
            IHostUiHandler uiHandler,
            Action<bool> showLoading,
            Action<bool> switchToGameControl,
            Action<Control> setMainContent,
            Func<TopLevel> getTopLevel)
        {
            ContentManager = contentManager;
            StorageProvider = storageProvider;
            ApplicationLibrary = applicationLibrary;
            VirtualFileSystem = virtualFileSystem;
            AccountManager = accountManager;
            InputManager = inputManager;
            UserChannelPersistence = userChannelPersistence;
            LibHacHorizonManager = libHacHorizonManager;
            UiHandler = uiHandler;

            ShowLoading = showLoading;
            SwitchToGameControl = switchToGameControl;
            SetMainContent = setMainContent;
            GetTopLevel = getTopLevel;

            Ptc.PtcStateChanged -= ProgressHandler;
            Ptc.PtcStateChanged += ProgressHandler;
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;

                _searchTimer?.Dispose();

                _searchTimer = new Timer(TimerCallback, null, 1000, 0);
            }
        }

        private void TimerCallback(object obj)
        {
            RefreshView();

            _searchTimer.Dispose();
            _searchTimer = null;
        }

        public bool CanUpdate
        {
            get => _canUpdate;
            set
            {
                _canUpdate = value;

                OnPropertyChanged();
            }
        }

        public Cursor Cursor
        {
            get => _cursor;
            set
            {
                _cursor = value;
                OnPropertyChanged();
            }
        }

        public ReadOnlyObservableCollection<ApplicationData> AppsObservableList
        {
            get => _appsObservableList;
            set
            {
                _appsObservableList = value;

                OnPropertyChanged();
            }
        }

        public bool IsPaused
        {
            get => _isPaused;
            set
            {
                _isPaused = value;

                OnPropertyChanged();
            }
        }

        public long LastFullscreenToggle
        {
            get => _lastFullscreenToggle;
            set
            {
                _lastFullscreenToggle = value;

                OnPropertyChanged();
            }
        }



        public bool EnableNonGameRunningControls => !IsGameRunning;

        public bool ShowFirmwareStatus => !ShowLoadProgress;

        public bool IsGameRunning
        {
            get => _isGameRunning;
            set
            {
                _isGameRunning = value;

                if (!value)
                {
                    ShowMenuAndStatusBar = false;
                }

                OnPropertyChanged();
                OnPropertyChanged(nameof(EnableNonGameRunningControls));
                OnPropertyChanged(nameof(ShowFirmwareStatus));
            }
        }

        public bool IsAmiiboRequested
        {
            get => _isAmiiboRequested && _isGameRunning;
            set
            {
                _isAmiiboRequested = value;

                OnPropertyChanged();
            }
        }

        public bool ShowLoadProgress
        {
            get => _showLoadProgress;
            set
            {
                _showLoadProgress = value;

                OnPropertyChanged();
                OnPropertyChanged(nameof(ShowFirmwareStatus));
            }
        }

        public string GameStatusText
        {
            get => _gameStatusText;
            set
            {
                _gameStatusText = value;

                OnPropertyChanged();
            }
        }

        public bool IsFullScreen
        {
            get => _isFullScreen;
            set
            {
                _isFullScreen = value;

                OnPropertyChanged();
            }
        }

        public bool ShowAll
        {
            get => _showAll;
            set
            {
                _showAll = value;

                OnPropertyChanged();
            }
        }

        public string LastScannedAmiiboId
        {
            get => _lastScannedAmiiboId;
            set
            {
                _lastScannedAmiiboId = value;

                OnPropertyChanged();
            }
        }

        private string _showUikey = "F4";
        private string _pauseKey = "F5";
        private string _screenshotkey = "F8";
        private float _volume;
        private string _backendText;

        public ApplicationData ListSelectedApplication;
        public ApplicationData GridSelectedApplication;
        private bool _canUpdate;
        private Cursor _cursor;
        private string _title;
        private string _currentEmulatedGamePath;
        private AutoResetEvent _rendererWaitEvent;
        private WindowState _windowState;
        private bool _isActive;

        public ApplicationData SelectedApplication
        {
            get
            {
                return Glyph switch
                {
                    Glyph.List => ListSelectedApplication,
                    Glyph.Grid => GridSelectedApplication,
                    _ => null,
                };
            }
        }

        public string LoadHeading
        {
            get => _loadHeading;
            set
            {
                _loadHeading = value;

                OnPropertyChanged();
            }
        }

        public string CacheLoadStatus
        {
            get => _cacheLoadStatus;
            set
            {
                _cacheLoadStatus = value;

                OnPropertyChanged();
            }
        }

        public Brush ProgressBarBackgroundColor
        {
            get => _progressBarBackgroundColor;
            set
            {
                _progressBarBackgroundColor = value;

                OnPropertyChanged();
            }
        }

        public Brush ProgressBarForegroundColor
        {
            get => _progressBarForegroundColor;
            set
            {
                _progressBarForegroundColor = value;

                OnPropertyChanged();
            }
        }

        public Brush VsyncColor
        {
            get => _vsyncColor;
            set
            {
                _vsyncColor = value;

                OnPropertyChanged();
            }
        }

        public byte[] SelectedIcon
        {
            get => _selectedIcon;
            set
            {
                _selectedIcon = value;

                OnPropertyChanged();
            }
        }

        public int ProgressMaximum
        {
            get => _progressMaximum;
            set
            {
                _progressMaximum = value;

                OnPropertyChanged();
            }
        }

        public int ProgressValue
        {
            get => _progressValue;
            set
            {
                _progressValue = value;

                OnPropertyChanged();
            }
        }

        public int StatusBarProgressMaximum
        {
            get => _statusBarProgressMaximum;
            set
            {
                _statusBarProgressMaximum = value;

                OnPropertyChanged();
            }
        }

        public int StatusBarProgressValue
        {
            get => _statusBarProgressValue;
            set
            {
                _statusBarProgressValue = value;

                OnPropertyChanged();
            }
        }

        public string FifoStatusText
        {
            get => _fifoStatusText;
            set
            {
                _fifoStatusText = value;

                OnPropertyChanged();
            }
        }

        public string GpuNameText
        {
            get => _gpuStatusText;
            set
            {
                _gpuStatusText = value;

                OnPropertyChanged();
            }
        }

        public string BackendText
        {
            get => _backendText;
            set
            {
                _backendText = value;

                OnPropertyChanged();
            }
        }

        public string DockedStatusText
        {
            get => _dockedStatusText;
            set
            {
                _dockedStatusText = value;

                OnPropertyChanged();
            }
        }

        public string AspectRatioStatusText
        {
            get => _aspectStatusText;
            set
            {
                _aspectStatusText = value;

                OnPropertyChanged();
            }
        }

        public string VolumeStatusText
        {
            get => _volumeStatusText;
            set
            {
                _volumeStatusText = value;

                OnPropertyChanged();
            }
        }

        public bool VolumeMuted => _volume == 0;

        public float Volume
        {
            get => _volume;
            set
            {
                _volume = value;

                if (_isGameRunning)
                {
                    AppHost.Device.SetVolume(_volume);
                }

                OnPropertyChanged(nameof(VolumeStatusText));
                OnPropertyChanged(nameof(VolumeMuted));
                OnPropertyChanged();
            }
        }

        public bool ShowStatusSeparator
        {
            get => _showStatusSeparator;
            set
            {
                _showStatusSeparator = value;

                OnPropertyChanged();
            }
        }

        public bool ShowMenuAndStatusBar
        {
            get => _showMenuAndStatusBar;
            set
            {
                _showMenuAndStatusBar = value;

                OnPropertyChanged();
            }
        }

        public bool IsLoadingIndeterminate
        {
            get => _isLoadingIndeterminate;
            set
            {
                _isLoadingIndeterminate = value;

                OnPropertyChanged();
            }
        }
        public bool IsActive
        {
            get => _isActive; set
            {
                _isActive = value;

                OnPropertyChanged();
            }
        }


        public bool ShowContent
        {
            get => _showContent;
            set
            {
                _showContent = value;

                OnPropertyChanged();
            }
        }

        public bool IsAppletMenuActive
        {
            get => _isAppletMenuActive && EnableNonGameRunningControls;
            set
            {
                _isAppletMenuActive = value;

                OnPropertyChanged();
            }
        }
        public WindowState WindowState
        {
            get => _windowState; internal set
            {
                _windowState = value;

                OnPropertyChanged();
            }
        }

        public bool IsGrid => Glyph == Glyph.Grid;
        public bool IsList => Glyph == Glyph.List;

        internal void Sort(bool isAscending)
        {
            IsAscending = isAscending;

            RefreshView();
        }

        internal void Sort(ApplicationSort sort)
        {
            SortMode = sort;

            RefreshView();
        }

        private IComparer<ApplicationData> GetComparer()
        {
            return SortMode switch
            {
                ApplicationSort.LastPlayed => new Models.Generic.LastPlayedSortComparer(IsAscending),
                ApplicationSort.FileSize => IsAscending ? SortExpressionComparer<ApplicationData>.Ascending(app => app.FileSizeBytes)
                                                                : SortExpressionComparer<ApplicationData>.Descending(app => app.FileSizeBytes),
                ApplicationSort.TotalTimePlayed => IsAscending ? SortExpressionComparer<ApplicationData>.Ascending(app => app.TimePlayedNum)
                                                                : SortExpressionComparer<ApplicationData>.Descending(app => app.TimePlayedNum),
                ApplicationSort.Title => IsAscending ? SortExpressionComparer<ApplicationData>.Ascending(app => app.TitleName)
                                                                : SortExpressionComparer<ApplicationData>.Descending(app => app.TitleName),
                ApplicationSort.Favorite => !IsAscending ? SortExpressionComparer<ApplicationData>.Ascending(app => app.Favorite)
                                                                : SortExpressionComparer<ApplicationData>.Descending(app => app.Favorite),
                ApplicationSort.Developer => IsAscending ? SortExpressionComparer<ApplicationData>.Ascending(app => app.Developer)
                                                                : SortExpressionComparer<ApplicationData>.Descending(app => app.Developer),
                ApplicationSort.FileType => IsAscending ? SortExpressionComparer<ApplicationData>.Ascending(app => app.FileExtension)
                                                                : SortExpressionComparer<ApplicationData>.Descending(app => app.FileExtension),
                ApplicationSort.Path => IsAscending ? SortExpressionComparer<ApplicationData>.Ascending(app => app.Path)
                                                                : SortExpressionComparer<ApplicationData>.Descending(app => app.Path),
                _ => null,
            };
        }

        public void RefreshView()
        {
            RefreshGrid();
        }

        private void RefreshGrid()
        {
            Applications.ToObservableChangeSet()
                .Filter(Filter)
                .Sort(GetComparer())
                .Bind(out _appsObservableList).AsObservableList();

            OnPropertyChanged(nameof(AppsObservableList));
        }

        public bool StartGamesInFullscreen
        {
            get => ConfigurationState.Instance.Ui.StartFullscreen;
            set
            {
                ConfigurationState.Instance.Ui.StartFullscreen.Value = value;

                ConfigurationState.Instance.ToFileFormat().SaveConfig(Program.ConfigurationPath);

                OnPropertyChanged();
            }
        }

        public bool ShowConsole
        {
            get => ConfigurationState.Instance.Ui.ShowConsole;
            set
            {
                ConfigurationState.Instance.Ui.ShowConsole.Value = value;

                ConfigurationState.Instance.ToFileFormat().SaveConfig(Program.ConfigurationPath);

                OnPropertyChanged();
            }
        }
        public string Title
        {
            get => _title;
            set
            {
                _title = value;

                OnPropertyChanged();
            }
        }

        public bool ShowConsoleVisible
        {
            get => ConsoleHelper.SetConsoleWindowStateSupported;
        }

        public ObservableCollection<ApplicationData> Applications
        {
            get => _applications;
            set
            {
                _applications = value;

                OnPropertyChanged();
            }
        }

        public Glyph Glyph
        {
            get => (Glyph)ConfigurationState.Instance.Ui.GameListViewMode.Value;
            set
            {
                ConfigurationState.Instance.Ui.GameListViewMode.Value = (int)value;

                OnPropertyChanged();
                OnPropertyChanged(nameof(IsGrid));
                OnPropertyChanged(nameof(IsList));

                ConfigurationState.Instance.ToFileFormat().SaveConfig(Program.ConfigurationPath);
            }
        }

        public bool ShowNames
        {
            get => ConfigurationState.Instance.Ui.ShowNames && ConfigurationState.Instance.Ui.GridSize > 1; set
            {
                ConfigurationState.Instance.Ui.ShowNames.Value = value;

                OnPropertyChanged();
                OnPropertyChanged(nameof(GridSizeScale));

                ConfigurationState.Instance.ToFileFormat().SaveConfig(Program.ConfigurationPath);
            }
        }

        internal ApplicationSort SortMode
        {
            get => (ApplicationSort)ConfigurationState.Instance.Ui.ApplicationSort.Value;
            private set
            {
                ConfigurationState.Instance.Ui.ApplicationSort.Value = (int)value;

                OnPropertyChanged();
                OnPropertyChanged(nameof(SortName));

                ConfigurationState.Instance.ToFileFormat().SaveConfig(Program.ConfigurationPath);
            }
        }

        public bool IsSortedByFavorite => SortMode == ApplicationSort.Favorite;
        public bool IsSortedByTitle => SortMode == ApplicationSort.Title;
        public bool IsSortedByDeveloper => SortMode == ApplicationSort.Developer;
        public bool IsSortedByLastPlayed => SortMode == ApplicationSort.LastPlayed;
        public bool IsSortedByTimePlayed => SortMode == ApplicationSort.TotalTimePlayed;
        public bool IsSortedByType => SortMode == ApplicationSort.FileType;
        public bool IsSortedBySize => SortMode == ApplicationSort.FileSize;
        public bool IsSortedByPath => SortMode == ApplicationSort.Path;

        public string SortName
        {
            get
            {
                return SortMode switch
                {
                    ApplicationSort.Title => LocaleManager.Instance["GameListHeaderApplication"],
                    ApplicationSort.Developer => LocaleManager.Instance["GameListHeaderDeveloper"],
                    ApplicationSort.LastPlayed => LocaleManager.Instance["GameListHeaderLastPlayed"],
                    ApplicationSort.TotalTimePlayed => LocaleManager.Instance["GameListHeaderTimePlayed"],
                    ApplicationSort.FileType => LocaleManager.Instance["GameListHeaderFileExtension"],
                    ApplicationSort.FileSize => LocaleManager.Instance["GameListHeaderFileSize"],
                    ApplicationSort.Path => LocaleManager.Instance["GameListHeaderPath"],
                    ApplicationSort.Favorite => LocaleManager.Instance["CommonFavorite"],
                    _ => string.Empty,
                };
            }
        }

        public bool IsAscending
        {
            get => ConfigurationState.Instance.Ui.IsAscendingOrder;
            private set
            {
                ConfigurationState.Instance.Ui.IsAscendingOrder.Value = value;

                OnPropertyChanged();
                OnPropertyChanged(nameof(SortMode));
                OnPropertyChanged(nameof(SortName));

                ConfigurationState.Instance.ToFileFormat().SaveConfig(Program.ConfigurationPath);
            }
        }

        public KeyGesture ShowUiKey
        {
            get => KeyGesture.Parse(_showUikey); set
            {
                _showUikey = value.ToString();

                OnPropertyChanged();
            }
        }

        public KeyGesture ScreenshotKey
        {
            get => KeyGesture.Parse(_screenshotkey); set
            {
                _screenshotkey = value.ToString();

                OnPropertyChanged();
            }
        }

        public KeyGesture PauseKey
        {
            get => KeyGesture.Parse(_pauseKey); set
            {
                _pauseKey = value.ToString();

                OnPropertyChanged();
            }
        }

        public bool IsGridSmall => ConfigurationState.Instance.Ui.GridSize == 1;
        public bool IsGridMedium => ConfigurationState.Instance.Ui.GridSize == 2;
        public bool IsGridLarge => ConfigurationState.Instance.Ui.GridSize == 3;
        public bool IsGridHuge => ConfigurationState.Instance.Ui.GridSize == 4;

        public int ListItemSelectorSize
        {
            get
            {
                if (IsGridSmall)
                {
                    return 78;
                }

                if (IsGridMedium)
                {
                    return 100;
                }

                if (IsGridLarge)
                {
                    return 120;
                }

                if (IsGridHuge)
                {
                    return 140;
                }

                return 16;
            }
        }

        public int GridItemSelectorSize
        {
            get
            {
                if (IsGridSmall)
                {
                    return 120;
                }

                if (IsGridMedium)
                {
                    return 150;
                }

                if (IsGridLarge)
                {
                    return 180;
                }

                if (IsGridHuge)
                {
                    return 220;
                }

                return 16;
            }
        }
        
        public int GridSizeScale
        {
            get => ConfigurationState.Instance.Ui.GridSize;
            set
            {
                ConfigurationState.Instance.Ui.GridSize.Value = value;

                if (value < 2)
                {
                    ShowNames = false;
                }

                OnPropertyChanged();
                OnPropertyChanged(nameof(IsGridSmall));
                OnPropertyChanged(nameof(IsGridMedium));
                OnPropertyChanged(nameof(IsGridLarge));
                OnPropertyChanged(nameof(IsGridHuge));
                OnPropertyChanged(nameof(ListItemSelectorSize));
                OnPropertyChanged(nameof(GridItemSelectorSize));
                OnPropertyChanged(nameof(ShowNames));

                ConfigurationState.Instance.ToFileFormat().SaveConfig(Program.ConfigurationPath);
            }
        }

        public ContentManager ContentManager { get; private set; }
        public IStorageProvider StorageProvider { get; private set; }
        public ApplicationLibrary ApplicationLibrary { get; private set; }
        public VirtualFileSystem VirtualFileSystem { get; private set; }
        public AccountManager AccountManager { get; private set; }
        public Ryujinx.Input.HLE.InputManager InputManager { get; private set; }
        public UserChannelPersistence UserChannelPersistence { get; private set; }
        public Action<bool> ShowLoading { get; private set; }
        public Action<bool> SwitchToGameControl { get; private set; }
        public Action<Control> SetMainContent { get; private set; }
        public Func<TopLevel> GetTopLevel { get; private set; }
        public RendererHost RendererControl { get; private set; }
        public bool IsClosing { get; set; }
        public LibHacHorizonManager LibHacHorizonManager { get; internal set; }
        public IHostUiHandler UiHandler { get; internal set; }

        public void HandleShaderProgress(Switch emulationContext)
        {
            emulationContext.Gpu.ShaderCacheStateChanged -= ProgressHandler;
            emulationContext.Gpu.ShaderCacheStateChanged += ProgressHandler;
        }

        private bool Filter(object arg)
        {
            if (arg is ApplicationData app)
            {
                return string.IsNullOrWhiteSpace(_searchText) || app.TitleName.ToLower().Contains(_searchText.ToLower());
            }

            return false;
        }

        public void LoadConfigurableHotKeys()
        {
            if (AvaloniaKeyboardMappingHelper.TryGetAvaKey((Ryujinx.Input.Key)ConfigurationState.Instance.Hid.Hotkeys.Value.ShowUi, out var showUiKey))
            {
                ShowUiKey = new KeyGesture(showUiKey, KeyModifiers.None);
            }

            if (AvaloniaKeyboardMappingHelper.TryGetAvaKey((Ryujinx.Input.Key)ConfigurationState.Instance.Hid.Hotkeys.Value.Screenshot, out var screenshotKey))
            {
                ScreenshotKey = new KeyGesture(screenshotKey, KeyModifiers.None);
            }

            if (AvaloniaKeyboardMappingHelper.TryGetAvaKey((Ryujinx.Input.Key)ConfigurationState.Instance.Hid.Hotkeys.Value.Pause, out var pauseKey))
            {
                PauseKey = new KeyGesture(pauseKey, KeyModifiers.None);
            }
        }

        public void TakeScreenshot()
        {
            AppHost.ScreenshotRequested = true;
        }

        public void HideUi()
        {
            ShowMenuAndStatusBar = false;
        }

        public void SetListMode()
        {
            Glyph = Glyph.List;
        }

        public void SetGridMode()
        {
            Glyph = Glyph.Grid;
        }

        public async void InstallFirmwareFromFile()
        {
            var result = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                AllowMultiple = false,
                FileTypeFilter = new List<FilePickerFileType>
                {
                    new(LocaleManager.Instance["FileDialogAllTypes"])
                    {
                        Patterns = new[] { "*.xci", "*.zip" },
                        AppleUniformTypeIdentifiers = new[] { "com.ryujinx.Ryujinx-xci", "public.zip-archive" },
                        MimeTypes = new[] { "application/x-nx-xci", "application/zip" }
                    }
                }
            });

            if (result.Count > 0)
            {
                if (result[0].TryGetUri(out Uri uri))
                {
                    await HandleFirmwareInstallation(uri.LocalPath);
                }
            }
        }

        public async void InstallFirmwareFromFolder()
        {
            var result = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                AllowMultiple = false
            });

            if (result.Count > 0)
            {
                if (result[0].TryGetUri(out Uri uri))
                {
                    await HandleFirmwareInstallation(uri.LocalPath);
                }
            }
        }

        private async Task HandleFirmwareInstallation(string filename)
        {
            try
            {
                SystemVersion firmwareVersion = ContentManager.VerifyFirmwarePackage(filename);

                if (firmwareVersion == null)
                {
                    await ContentDialogHelper.CreateErrorDialog(string.Format(LocaleManager.Instance["DialogFirmwareInstallerFirmwareNotFoundErrorMessage"], filename));

                    return;
                }

                string dialogTitle = string.Format(LocaleManager.Instance["DialogFirmwareInstallerFirmwareInstallTitle"], firmwareVersion.VersionString);

                SystemVersion currentVersion = ContentManager.GetCurrentFirmwareVersion();

                string dialogMessage = string.Format(LocaleManager.Instance["DialogFirmwareInstallerFirmwareInstallMessage"], firmwareVersion.VersionString);

                if (currentVersion != null)
                {
                    dialogMessage += string.Format(LocaleManager.Instance["DialogFirmwareInstallerFirmwareInstallSubMessage"], currentVersion.VersionString);
                }

                dialogMessage += LocaleManager.Instance["DialogFirmwareInstallerFirmwareInstallConfirmMessage"];

                UserResult result = await ContentDialogHelper.CreateConfirmationDialog(
                    dialogTitle,
                    dialogMessage,
                    LocaleManager.Instance["InputDialogYes"],
                    LocaleManager.Instance["InputDialogNo"],
                    LocaleManager.Instance["RyujinxConfirm"]);

                UpdateWaitWindow waitingDialog = ContentDialogHelper.CreateWaitingDialog(dialogTitle, LocaleManager.Instance["DialogFirmwareInstallerFirmwareInstallWaitMessage"]);

                if (result == UserResult.Yes)
                {
                    Logger.Info?.Print(LogClass.Application, $"Installing firmware {firmwareVersion.VersionString}");

                    Thread thread = new(() =>
                    {
                        Dispatcher.UIThread.InvokeAsync(delegate
                        {
                            waitingDialog.Show();
                        });

                        try
                        {
                            ContentManager.InstallFirmware(filename);

                            Dispatcher.UIThread.InvokeAsync(async delegate
                            {
                                waitingDialog.Close();

                                string message = string.Format(LocaleManager.Instance["DialogFirmwareInstallerFirmwareInstallSuccessMessage"], firmwareVersion.VersionString);

                                await ContentDialogHelper.CreateInfoDialog(dialogTitle, message, LocaleManager.Instance["InputDialogOk"], "", LocaleManager.Instance["RyujinxInfo"]);

                                Logger.Info?.Print(LogClass.Application, message);

                                // Purge Applet Cache.

                                DirectoryInfo miiEditorCacheFolder = new DirectoryInfo(Path.Combine(AppDataManager.GamesDirPath, "0100000000001009", "cache"));

                                if (miiEditorCacheFolder.Exists)
                                {
                                    miiEditorCacheFolder.Delete(true);
                                }
                            });
                        }
                        catch (Exception ex)
                        {
                            Dispatcher.UIThread.InvokeAsync(async () =>
                            {
                                waitingDialog.Close();

                                await ContentDialogHelper.CreateErrorDialog(ex.Message);
                            });
                        }
                        finally
                        {
                            RefreshFirmwareStatus();
                        }
                    });

                    thread.Name = "GUI.FirmwareInstallerThread";
                    thread.Start();
                }
            }
            catch (LibHac.Common.Keys.MissingKeyException ex)
            {
                Logger.Error?.Print(LogClass.Application, ex.ToString());

                Dispatcher.UIThread.Post(async () => await UserErrorDialog.ShowUserErrorDialog(UserError.NoKeys));
            }
            catch (Exception ex)
            {
                await ContentDialogHelper.CreateErrorDialog(ex.Message);
            }
        }

        public static void OpenRyujinxFolder()
        {
            OpenHelper.OpenFolder(AppDataManager.BaseDirPath);
        }

        public static void OpenLogsFolder()
        {
            string logPath = Path.Combine(ReleaseInformations.GetBaseApplicationDirectory(), "Logs");

            new DirectoryInfo(logPath).Create();

            OpenHelper.OpenFolder(logPath);
        }

        public void ToggleDockMode()
        {
            if (IsGameRunning)
            {
                ConfigurationState.Instance.System.EnableDockedMode.Value = !ConfigurationState.Instance.System.EnableDockedMode.Value;
            }
        }
        
        public async void ExitCurrentState()
        {
            if (WindowState == WindowState.FullScreen)
            {
                ToggleFullscreen();
            }
            else if (IsGameRunning)
            {
                await Task.Delay(100);

                AppHost?.ShowExitPrompt();
            }
        }

        public void ChangeLanguage(object obj)
        {
            LocaleManager.Instance.LoadDefaultLanguage();
            LocaleManager.Instance.LoadLanguage((string)obj);
        }

        public async void ManageProfiles()
        {
            await NavigationDialogHost.Show(AccountManager, ContentManager, VirtualFileSystem, LibHacHorizonManager.RyujinxClient);
        }

        private void ProgressHandler<T>(T state, int current, int total) where T : Enum
        {
            try
            {
                ProgressMaximum = total;
                ProgressValue = current;

                switch (state)
                {
                    case PtcLoadingState ptcState:
                        CacheLoadStatus = $"{current} / {total}";
                        switch (ptcState)
                        {
                            case PtcLoadingState.Start:
                            case PtcLoadingState.Loading:
                                LoadHeading = LocaleManager.Instance["CompilingPPTC"];
                                IsLoadingIndeterminate = false;
                                break;
                            case PtcLoadingState.Loaded:
                                LoadHeading = string.Format(LocaleManager.Instance["LoadingHeading"], TitleName);
                                IsLoadingIndeterminate = true;
                                CacheLoadStatus = "";
                                break;
                        }
                        break;
                    case ShaderCacheLoadingState shaderCacheState:
                        CacheLoadStatus = $"{current} / {total}";
                        switch (shaderCacheState)
                        {
                            case ShaderCacheLoadingState.Start:
                            case ShaderCacheLoadingState.Loading:
                                LoadHeading = LocaleManager.Instance["CompilingShaders"];
                                IsLoadingIndeterminate = false;
                                break;
                            case ShaderCacheLoadingState.Loaded:
                                LoadHeading = string.Format(LocaleManager.Instance["LoadingHeading"], TitleName);
                                IsLoadingIndeterminate = true;
                                CacheLoadStatus = "";
                                break;
                        }
                        break;
                    default:
                        throw new ArgumentException($"Unknown Progress Handler type {typeof(T)}");
                }
            }
            catch (Exception) { }
        }

        public void OpenPtcDirectory()
        {
            ApplicationData selection = SelectedApplication;
            if (selection != null)
            {
                string ptcDir = Path.Combine(AppDataManager.GamesDirPath, selection.TitleId, "cache", "cpu");
                string mainPath = Path.Combine(ptcDir, "0");
                string backupPath = Path.Combine(ptcDir, "1");

                if (!Directory.Exists(ptcDir))
                {
                    Directory.CreateDirectory(ptcDir);
                    Directory.CreateDirectory(mainPath);
                    Directory.CreateDirectory(backupPath);
                }

                OpenHelper.OpenFolder(ptcDir);
            }
        }

        public async void PurgePtcCache()
        {
            ApplicationData selection = SelectedApplication;
            if (selection != null)
            {
                DirectoryInfo mainDir = new(Path.Combine(AppDataManager.GamesDirPath, selection.TitleId, "cache", "cpu", "0"));
                DirectoryInfo backupDir = new(Path.Combine(AppDataManager.GamesDirPath, selection.TitleId, "cache", "cpu", "1"));

                // FIXME: Found a way to reproduce the bold effect on the title name (fork?).
                UserResult result = await ContentDialogHelper.CreateConfirmationDialog(LocaleManager.Instance["DialogWarning"],
                                                                                       string.Format(LocaleManager.Instance["DialogPPTCDeletionMessage"], selection.TitleName),
                                                                                       LocaleManager.Instance["InputDialogYes"],
                                                                                       LocaleManager.Instance["InputDialogNo"],
                                                                                       LocaleManager.Instance["RyujinxConfirm"]);

                List<FileInfo> cacheFiles = new();

                if (mainDir.Exists)
                {
                    cacheFiles.AddRange(mainDir.EnumerateFiles("*.cache"));
                }

                if (backupDir.Exists)
                {
                    cacheFiles.AddRange(backupDir.EnumerateFiles("*.cache"));
                }

                if (cacheFiles.Count > 0 && result == UserResult.Yes)
                {
                    foreach (FileInfo file in cacheFiles)
                    {
                        try
                        {
                            file.Delete();
                        }
                        catch (Exception e)
                        {
                            await ContentDialogHelper.CreateErrorDialog(string.Format(LocaleManager.Instance["DialogPPTCDeletionErrorMessage"], file.Name, e));
                        }
                    }
                }
            }
        }

        public void OpenShaderCacheDirectory()
        {
            ApplicationData selection = SelectedApplication;
            if (selection != null)
            {
                string shaderCacheDir = Path.Combine(AppDataManager.GamesDirPath, selection.TitleId, "cache", "shader");

                if (!Directory.Exists(shaderCacheDir))
                {
                    Directory.CreateDirectory(shaderCacheDir);
                }

                OpenHelper.OpenFolder(shaderCacheDir);
            }
        }

        public void SimulateWakeUpMessage()
        {
            AppHost.Device.System.SimulateWakeUpMessage();
        }

        public async void PurgeShaderCache()
        {
            ApplicationData selection = SelectedApplication;
            if (selection != null)
            {
                DirectoryInfo shaderCacheDir = new(Path.Combine(AppDataManager.GamesDirPath, selection.TitleId, "cache", "shader"));

                // FIXME: Found a way to reproduce the bold effect on the title name (fork?).
                UserResult result = await ContentDialogHelper.CreateConfirmationDialog(LocaleManager.Instance["DialogWarning"],
                                                                                       string.Format(LocaleManager.Instance["DialogShaderDeletionMessage"], selection.TitleName),
                                                                                       LocaleManager.Instance["InputDialogYes"],
                                                                                       LocaleManager.Instance["InputDialogNo"],
                                                                                       LocaleManager.Instance["RyujinxConfirm"]);

                List<DirectoryInfo> oldCacheDirectories = new();
                List<FileInfo> newCacheFiles = new();

                if (shaderCacheDir.Exists)
                {
                    oldCacheDirectories.AddRange(shaderCacheDir.EnumerateDirectories("*"));
                    newCacheFiles.AddRange(shaderCacheDir.GetFiles("*.toc"));
                    newCacheFiles.AddRange(shaderCacheDir.GetFiles("*.data"));
                }

                if ((oldCacheDirectories.Count > 0 || newCacheFiles.Count > 0) && result == UserResult.Yes)
                {
                    foreach (DirectoryInfo directory in oldCacheDirectories)
                    {
                        try
                        {
                            directory.Delete(true);
                        }
                        catch (Exception e)
                        {
                            await ContentDialogHelper.CreateErrorDialog(string.Format(LocaleManager.Instance["DialogPPTCDeletionErrorMessage"], directory.Name, e));
                        }
                    }
                }

                foreach (FileInfo file in newCacheFiles)
                {
                    try
                    {
                        file.Delete();
                    }
                    catch (Exception e)
                    {
                        await ContentDialogHelper.CreateErrorDialog(string.Format(LocaleManager.Instance["ShaderCachePurgeError"], file.Name, e));
                    }
                }
            }
        }

        public void OpenDeviceSaveDirectory()
        {
            ApplicationData selection = SelectedApplication;
            if (selection != null)
            {
                Task.Run(() =>
                {
                    if (!ulong.TryParse(selection.TitleId, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ulong titleIdNumber))
                    {
                        Dispatcher.UIThread.Post(async () =>
                        {
                            await ContentDialogHelper.CreateErrorDialog(LocaleManager.Instance["DialogRyujinxErrorMessage"], LocaleManager.Instance["DialogInvalidTitleIdErrorMessage"]);
                        });

                        return;
                    }

                    var saveDataFilter = SaveDataFilter.Make(titleIdNumber, SaveDataType.Device, userId: default, saveDataId: default, index: default);
                    OpenSaveDirectory(in saveDataFilter, selection, titleIdNumber);
                });
            }
        }

        public void OpenBcatSaveDirectory()
        {
            ApplicationData selection = SelectedApplication;
            if (selection != null)
            {
                Task.Run(() =>
                {
                    if (!ulong.TryParse(selection.TitleId, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ulong titleIdNumber))
                    {
                        Dispatcher.UIThread.Post(async () =>
                        {
                            await ContentDialogHelper.CreateErrorDialog(LocaleManager.Instance["DialogRyujinxErrorMessage"], LocaleManager.Instance["DialogInvalidTitleIdErrorMessage"]);
                        });

                        return;
                    }

                    var saveDataFilter = SaveDataFilter.Make(titleIdNumber, SaveDataType.Bcat, userId: default, saveDataId: default, index: default);
                    OpenSaveDirectory(in saveDataFilter, selection, titleIdNumber);
                });
            }
        }
        
        public void ToggleFavorite()
        {
            ApplicationData selection = SelectedApplication;
            if (selection != null)
            {
                selection.Favorite = !selection.Favorite;

                ApplicationLibrary.LoadAndSaveMetaData(selection.TitleId, appMetadata =>
                {
                    appMetadata.Favorite = selection.Favorite;
                });

                RefreshView();
            }
        }

        public void OpenUserSaveDirectory()
        {
            ApplicationData selection = SelectedApplication;
            if (selection != null)
            {
                Task.Run(() =>
                {
                    if (!ulong.TryParse(selection.TitleId, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ulong titleIdNumber))
                    {
                        Dispatcher.UIThread.Post(async () =>
                        {
                            await ContentDialogHelper.CreateErrorDialog(LocaleManager.Instance["DialogRyujinxErrorMessage"], LocaleManager.Instance["DialogInvalidTitleIdErrorMessage"]);
                        });

                        return;
                    }

                    UserId         userId         = new((ulong)AccountManager.LastOpenedUser.UserId.High, (ulong)AccountManager.LastOpenedUser.UserId.Low);
                    SaveDataFilter saveDataFilter = SaveDataFilter.Make(titleIdNumber, saveType: default, userId, saveDataId: default, index: default);
                    OpenSaveDirectory(in saveDataFilter, selection, titleIdNumber);
                });
            }
        }
        
        public void OpenModsDirectory()
        {
            ApplicationData selection = SelectedApplication;
            if (selection != null)
            {
                string modsBasePath  = VirtualFileSystem.ModLoader.GetModsBasePath();
                string titleModsPath = VirtualFileSystem.ModLoader.GetTitleDir(modsBasePath, selection.TitleId);

                OpenHelper.OpenFolder(titleModsPath);
            }
        }

        public void OpenSdModsDirectory()
        {
            ApplicationData selection = SelectedApplication;

            if (selection != null)
            {
                string sdModsBasePath = VirtualFileSystem.ModLoader.GetSdModsBasePath();
                string titleModsPath  = VirtualFileSystem.ModLoader.GetTitleDir(sdModsBasePath, selection.TitleId);

                OpenHelper.OpenFolder(titleModsPath);
            }
        }

        public async void OpenTitleUpdateManager()
        {
            ApplicationData selection = SelectedApplication;
            if (selection != null)
            {
                if (Avalonia.Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    await new TitleUpdateWindow(VirtualFileSystem, ulong.Parse(selection.TitleId, NumberStyles.HexNumber), selection.TitleName).ShowDialog(desktop.MainWindow);
                }
            }
        }

        public async void OpenDownloadableContentManager()
        {
            ApplicationData selection = SelectedApplication;
            if (selection != null)
            {
                if (Avalonia.Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    await new DownloadableContentManagerWindow(VirtualFileSystem, ulong.Parse(selection.TitleId, NumberStyles.HexNumber), selection.TitleName).ShowDialog(desktop.MainWindow);
                }
            }
        }

        public async void OpenCheatManager()
        {
            ApplicationData selection = SelectedApplication;
            if (selection != null)
            {
                if (Avalonia.Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    await new CheatWindow(VirtualFileSystem, selection.TitleId, selection.TitleName).ShowDialog(desktop.MainWindow);
                }
            }
        }

        public void OpenSaveDirectory(in SaveDataFilter filter, ApplicationData data, ulong titleId)
        {
            ApplicationHelper.OpenSaveDir(in filter, titleId, data.ControlHolder, data.TitleName);
        }

        private async void ExtractLogo()
        {
            var selection = SelectedApplication;
            if (selection != null)
            {
                await ApplicationHelper.ExtractSection(NcaSectionType.Logo, selection.Path);
            }
        }

        private async void ExtractRomFs()
        {
            var selection = SelectedApplication;
            if (selection != null)
            {
                await ApplicationHelper.ExtractSection(NcaSectionType.Data, selection.Path);
            }
        }

        private async void ExtractExeFs()
        {
            var selection = SelectedApplication;
            if (selection != null)
            {
                await ApplicationHelper.ExtractSection(NcaSectionType.Code, selection.Path);
            }
        }

        public async void OpenFile()
        {
            var result = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = LocaleManager.Instance["OpenFileDialogTitle"],
                AllowMultiple = false,
                FileTypeFilter = new List<FilePickerFileType>
                {
                    new(LocaleManager.Instance["AllSupportedFormats"])
                    {
                        Patterns = new[] { "*.nsp", "*.pfs0", "*.xci", "*.nca", "*.nro", "*.nso"},
                        AppleUniformTypeIdentifiers = new[]
                        {
                            "com.ryujinx.Ryujinx-nsp",
                            "com.ryujinx.Ryujinx-pfs0",
                            "com.ryujinx.Ryujinx-xci",
                            "com.ryujinx.Ryujinx-nca",
                            "com.ryujinx.Ryujinx-nro",
                            "com.ryujinx.Ryujinx-nso"
                        },
                        MimeTypes = new[]
                        {
                            "application/x-nx-nsp",
                            "application/x-nx-pfs0",
                            "application/x-nx-xci",
                            "application/x-nx-nca",
                            "application/x-nx-nro",
                            "application/x-nx-nso"
                        }
                    }
                }
            });

            if (result.Count > 0)
            {
                if (result[0].TryGetUri(out Uri uri))
                {
                    LoadApplication(uri.LocalPath);
                }
            }
        }

        public async void OpenFolder()
        {
            var result = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = LocaleManager.Instance["OpenFolderDialogTitle"],
                AllowMultiple = false
            });

            if (result.Count > 0)
            {
                if (result[0].TryGetUri(out Uri uri))
                {
                    LoadApplication(uri.LocalPath);
                }
            }
        }

        public async void LoadApplication(string path, bool startFullscreen = false, string titleName = "")
        {
            if (AppHost != null)
            {
                await ContentDialogHelper.CreateInfoDialog(
                    LocaleManager.Instance["DialogLoadAppGameAlreadyLoadedMessage"],
                    LocaleManager.Instance["DialogLoadAppGameAlreadyLoadedSubMessage"],
                    LocaleManager.Instance["InputDialogOk"],
                    "",
                    LocaleManager.Instance["RyujinxInfo"]);

                return;
            }

#if RELEASE
            await PerformanceCheck();
#endif

            Logger.RestartTime();

            if (SelectedIcon == null)
            {
                SelectedIcon = ApplicationLibrary.GetApplicationIcon(path);
            }

            PrepareLoadScreen();

            RendererControl = new RendererHost(ConfigurationState.Instance.Logger.GraphicsDebugLevel);
            if (ConfigurationState.Instance.Graphics.GraphicsBackend.Value == GraphicsBackend.OpenGl)
            {
                RendererControl.CreateOpenGL();
            }
            else
            {
                RendererControl.CreateVulkan();
            }

            AppHost = new AppHost(RendererControl, InputManager, path, VirtualFileSystem, ContentManager, AccountManager, UserChannelPersistence, this, GetTopLevel());

            Dispatcher.UIThread.Post(async () =>
            {
                if (!await AppHost.LoadGuestApplication())
                {
                    AppHost.DisposeContext();
                    AppHost = null;

                    return;
                }

                CanUpdate = false;
                LoadHeading = string.IsNullOrWhiteSpace(titleName) ? string.Format(LocaleManager.Instance["LoadingHeading"], AppHost.Device.Application.TitleName) : titleName;
                TitleName = string.IsNullOrWhiteSpace(titleName) ? AppHost.Device.Application.TitleName : titleName;

                SwitchToRenderer(startFullscreen);

                _currentEmulatedGamePath = path;

                Thread gameThread = new(InitializeGame)
                {
                    Name = "GUI.WindowThread"
                };
                gameThread.Start();
            });
        }

        public void SwitchToRenderer(bool startFullscreen)
        {
            Dispatcher.UIThread.Post(() =>
            {
                SwitchToGameControl(startFullscreen);

                SetMainContent(RendererControl);

                RendererControl.Focus();
            });
        }

        private void PrepareLoadScreen()
        {
            using MemoryStream stream = new MemoryStream(SelectedIcon);
            using var gameIconBmp = SixLabors.ImageSharp.Image.Load<Bgra32>(stream);

            var dominantColor = IconColorPicker.GetFilteredColor(gameIconBmp).ToPixel<Bgra32>();

            const int ColorDivisor = 4;

            Color progressFgColor = Color.FromRgb(dominantColor.R, dominantColor.G, dominantColor.B);
            Color progressBgColor = Color.FromRgb(
                (byte)(dominantColor.R / ColorDivisor),
                (byte)(dominantColor.G / ColorDivisor),
                (byte)(dominantColor.B / ColorDivisor));

            ProgressBarForegroundColor = new SolidColorBrush(progressFgColor);
            ProgressBarBackgroundColor = new SolidColorBrush(progressBgColor);
        }

        private void InitializeGame()
        {
            RendererControl.RendererInitialized += GlRenderer_Created;

            AppHost.StatusUpdatedEvent += Update_StatusBar;
            AppHost.AppExit += AppHost_AppExit;

            _rendererWaitEvent.WaitOne();

            AppHost?.Start();

            AppHost.DisposeContext();
        }

        public void UpdateGameMetadata(string titleId)
        {
            ApplicationLibrary.LoadAndSaveMetaData(titleId, appMetadata =>
            {
                if (DateTime.TryParse(appMetadata.LastPlayed, out DateTime lastPlayedDateTime))
                {
                    double sessionTimePlayed = DateTime.UtcNow.Subtract(lastPlayedDateTime).TotalSeconds;

                    appMetadata.TimePlayed += Math.Round(sessionTimePlayed, MidpointRounding.AwayFromZero);
                }
            });
        }

        public void RefreshFirmwareStatus()
        {
            SystemVersion version = null;
            try
            {
                version = ContentManager.GetCurrentFirmwareVersion();
            }
            catch (Exception) { }

            bool hasApplet = false;

            if (version != null)
            {
                LocaleManager.Instance.UpdateDynamicValue("StatusBarSystemVersion",
                    version.VersionString);

                hasApplet = version.Major > 3;
            }
            else
            {
                LocaleManager.Instance.UpdateDynamicValue("StatusBarSystemVersion", "0.0");
            }

            IsAppletMenuActive = hasApplet;
        }

        public void AppHost_AppExit(object sender, EventArgs e)
        {
            if (IsClosing)
            {
                return;
            }

            IsGameRunning = false;

            Dispatcher.UIThread.InvokeAsync(() =>
            {
                ShowMenuAndStatusBar = true;
                ShowContent = true;
                ShowLoadProgress = false;
                IsLoadingIndeterminate = false;
                CanUpdate = true;
                Cursor = Cursor.Default;

                SetMainContent(null);

                AppHost = null;

                HandleRelaunch();
            });

            RendererControl.RendererInitialized -= GlRenderer_Created;
            RendererControl = null;

            SelectedIcon = null;

            Dispatcher.UIThread.InvokeAsync(() =>
            {
                Title = $"Ryujinx {Program.Version}";
            });
        }

        public void ToggleFullscreen()
        {
            if (Environment.TickCount64 - LastFullscreenToggle < HotKeyPressDelayMs)
            {
                return;
            }

            LastFullscreenToggle = Environment.TickCount64;

            if (WindowState == WindowState.FullScreen)
            {
                WindowState = WindowState.Normal;

                if (IsGameRunning)
                {
                    ShowMenuAndStatusBar = true;
                }
            }
            else
            {
                WindowState = WindowState.FullScreen;

                if (IsGameRunning)
                {
                    ShowMenuAndStatusBar = false;
                }
            }

            IsFullScreen = WindowState == WindowState.FullScreen;
        }

        private void HandleRelaunch()
        {
            if (UserChannelPersistence.PreviousIndex != -1 && UserChannelPersistence.ShouldRestart)
            {
                UserChannelPersistence.ShouldRestart = false;

                Dispatcher.UIThread.Post(() =>
                {
                    LoadApplication(_currentEmulatedGamePath);
                });
            }
            else
            {
                // otherwise, clear state.
                UserChannelPersistence = new UserChannelPersistence();
                _currentEmulatedGamePath = null;
            }
        }

        private void Update_StatusBar(object sender, StatusUpdatedEventArgs args)
        {
            if (ShowMenuAndStatusBar && !ShowLoadProgress)
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (args.VSyncEnabled)
                    {
                        VsyncColor = new SolidColorBrush(Color.Parse("#ff2eeac9"));
                    }
                    else
                    {
                        VsyncColor = new SolidColorBrush(Color.Parse("#ffff4554"));
                    }

                    DockedStatusText = args.DockedMode;
                    AspectRatioStatusText = args.AspectRatio;
                    GameStatusText = args.GameStatus;
                    VolumeStatusText = args.VolumeStatus;
                    FifoStatusText = args.FifoStatus;
                    GpuNameText = args.GpuName;
                    BackendText = args.GpuBackend;

                    ShowStatusSeparator = true;
                });
            }
        }

        private void GlRenderer_Created(object sender, EventArgs e)
        {
            ShowLoading(false);

            _rendererWaitEvent.Set();
        }

        public static void SaveConfig()
        {
            ConfigurationState.Instance.ToFileFormat().SaveConfig(Program.ConfigurationPath);
        }

        public static async Task PerformanceCheck()
        {
            if (ConfigurationState.Instance.Logger.EnableTrace.Value)
            {
                string mainMessage = LocaleManager.Instance["DialogPerformanceCheckLoggingEnabledMessage"];
                string secondaryMessage = LocaleManager.Instance["DialogPerformanceCheckLoggingEnabledConfirmMessage"];

                UserResult result = await ContentDialogHelper.CreateConfirmationDialog(mainMessage, secondaryMessage,
                    LocaleManager.Instance["InputDialogYes"], LocaleManager.Instance["InputDialogNo"],
                    LocaleManager.Instance["RyujinxConfirm"]);

                if (result != UserResult.Yes)
                {
                    ConfigurationState.Instance.Logger.EnableTrace.Value = false;

                    SaveConfig();
                }
            }

            if (!string.IsNullOrWhiteSpace(ConfigurationState.Instance.Graphics.ShadersDumpPath.Value))
            {
                string mainMessage = LocaleManager.Instance["DialogPerformanceCheckShaderDumpEnabledMessage"];
                string secondaryMessage =
                    LocaleManager.Instance["DialogPerformanceCheckShaderDumpEnabledConfirmMessage"];

                UserResult result = await ContentDialogHelper.CreateConfirmationDialog(mainMessage, secondaryMessage,
                    LocaleManager.Instance["InputDialogYes"], LocaleManager.Instance["InputDialogNo"],
                    LocaleManager.Instance["RyujinxConfirm"]);

                if (result != UserResult.Yes)
                {
                    ConfigurationState.Instance.Graphics.ShadersDumpPath.Value = "";

                    SaveConfig();
                }
            }
        }
    }
}