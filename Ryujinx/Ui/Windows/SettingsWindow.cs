using Gtk;
using Ryujinx.Audio.Backends.OpenAL;
using Ryujinx.Audio.Backends.SoundIo;
using Ryujinx.Common;
using Ryujinx.Common.Configuration;
using Ryujinx.Common.Configuration.Hid;
using Ryujinx.Common.Logging;
using Ryujinx.Configuration;
using Ryujinx.Configuration.System;
using Ryujinx.HLE.FileSystem;
using Ryujinx.HLE.HOS.Services.Time.TimeZone;
using Ryujinx.Ui.Helper;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

using GUI = Gtk.Builder.ObjectAttribute;

namespace Ryujinx.Ui.Windows
{
    public class SettingsWindow : Window
    {
        private readonly MainWindow             _parent;
        private readonly ListStore              _gameDirsBoxStore;
        private readonly ListStore              _audioBackendStore;
        private readonly TimeZoneContentManager _timeZoneContentManager;
        private readonly HashSet<string>        _validTzRegions;
        private readonly string                 _gameId;
        private readonly string                 _gameTitle;

        private long _systemTimeOffset;

#pragma warning disable CS0649, IDE0044
        [GUI] CheckButton     _errorLogToggle;
        [GUI] CheckButton     _warningLogToggle;
        [GUI] CheckButton     _infoLogToggle;
        [GUI] CheckButton     _stubLogToggle;
        [GUI] CheckButton     _debugLogToggle;
        [GUI] CheckButton     _fileLogToggle;
        [GUI] CheckButton     _guestLogToggle;
        [GUI] CheckButton     _fsAccessLogToggle;
        [GUI] Adjustment      _fsLogSpinAdjustment;
        [GUI] ComboBoxText    _graphicsDebugLevel;
        [GUI] CheckButton     _dockedModeToggle;
        [GUI] CheckButton     _discordToggle;
        [GUI] CheckButton     _checkUpdatesToggle;
        [GUI] CheckButton     _showConfirmExitToggle;
        [GUI] CheckButton     _hideCursorOnIdleToggle;
        [GUI] CheckButton     _vSyncToggle;
        [GUI] CheckButton     _shaderCacheToggle;
        [GUI] CheckButton     _ptcToggle;
        [GUI] CheckButton     _fsicToggle;
        [GUI] CheckButton     _ignoreToggle;
        [GUI] CheckButton     _directKeyboardAccess;
        [GUI] ComboBoxText    _systemLanguageSelect;
        [GUI] ComboBoxText    _systemRegionSelect;
        [GUI] Entry           _systemTimeZoneEntry;
        [GUI] EntryCompletion _systemTimeZoneCompletion;
        [GUI] Box             _audioBackendBox;
        [GUI] ComboBox        _audioBackendSelect;
        [GUI] SpinButton      _systemTimeYearSpin;
        [GUI] SpinButton      _systemTimeMonthSpin;
        [GUI] SpinButton      _systemTimeDaySpin;
        [GUI] SpinButton      _systemTimeHourSpin;
        [GUI] SpinButton      _systemTimeMinuteSpin;
        [GUI] Adjustment      _systemTimeYearSpinAdjustment;
        [GUI] Adjustment      _systemTimeMonthSpinAdjustment;
        [GUI] Adjustment      _systemTimeDaySpinAdjustment;
        [GUI] Adjustment      _systemTimeHourSpinAdjustment;
        [GUI] Adjustment      _systemTimeMinuteSpinAdjustment;
        [GUI] CheckButton     _custThemeToggle;
        [GUI] Entry           _custThemePath;
        [GUI] ToggleButton    _browseThemePath;
        [GUI] Label           _custThemePathLabel;
        [GUI] TreeView        _gameDirsBox;
        [GUI] Entry           _addGameDirBox;
        [GUI] Entry           _graphicsShadersDumpPath;
        [GUI] ComboBoxText    _anisotropy;
        [GUI] ComboBoxText    _aspectRatio;
        [GUI] ComboBoxText    _resScaleCombo;
        [GUI] Entry           _resScaleText;
        [GUI] ToggleButton    _resetToggle;
        [GUI] ToggleButton    _configureController1;
        [GUI] ToggleButton    _configureController2;
        [GUI] ToggleButton    _configureController3;
        [GUI] ToggleButton    _configureController4;
        [GUI] ToggleButton    _configureController5;
        [GUI] ToggleButton    _configureController6;
        [GUI] ToggleButton    _configureController7;
        [GUI] ToggleButton    _configureController8;
        [GUI] ToggleButton    _configureControllerH;

#pragma warning restore CS0649, IDE0044

        public SettingsWindow(MainWindow parent, VirtualFileSystem virtualFileSystem, HLE.FileSystem.Content.ContentManager contentManager, String gameTitle = null, String gameId = null) : this(parent, new Builder("Ryujinx.Ui.Windows.SettingsWindow.glade"), virtualFileSystem, contentManager, gameTitle, gameId) { }

        private SettingsWindow(MainWindow parent, Builder builder, VirtualFileSystem virtualFileSystem, HLE.FileSystem.Content.ContentManager contentManager, String gameTitle, String gameId) : base(builder.GetObject("_settingsWin").Handle)
        {
            Icon = new Gdk.Pixbuf(Assembly.GetExecutingAssembly(), "Ryujinx.Ui.Resources.Logo_Ryujinx.png");

            _parent    = parent;
            _gameId    = gameId;
            _gameTitle = gameTitle;

            builder.Autoconnect(this);

            _timeZoneContentManager = new TimeZoneContentManager();
            _timeZoneContentManager.InitializeInstance(virtualFileSystem, contentManager, LibHac.FsSystem.IntegrityCheckLevel.None);

            _validTzRegions = new HashSet<string>(_timeZoneContentManager.LocationNameCache.Length, StringComparer.Ordinal); // Zone regions are identifiers. Must match exactly.

            // Bind Events.
            _configureController1.Pressed += (sender, args) => ConfigureController_Pressed(sender, PlayerIndex.Player1);
            _configureController2.Pressed += (sender, args) => ConfigureController_Pressed(sender, PlayerIndex.Player2);
            _configureController3.Pressed += (sender, args) => ConfigureController_Pressed(sender, PlayerIndex.Player3);
            _configureController4.Pressed += (sender, args) => ConfigureController_Pressed(sender, PlayerIndex.Player4);
            _configureController5.Pressed += (sender, args) => ConfigureController_Pressed(sender, PlayerIndex.Player5);
            _configureController6.Pressed += (sender, args) => ConfigureController_Pressed(sender, PlayerIndex.Player6);
            _configureController7.Pressed += (sender, args) => ConfigureController_Pressed(sender, PlayerIndex.Player7);
            _configureController8.Pressed += (sender, args) => ConfigureController_Pressed(sender, PlayerIndex.Player8);
            _configureControllerH.Pressed += (sender, args) => ConfigureController_Pressed(sender, PlayerIndex.Handheld);
            _systemTimeZoneEntry.FocusOutEvent += TimeZoneEntry_FocusOut;

            _resScaleCombo.Changed += (sender, args) => _resScaleText.Visible = _resScaleCombo.ActiveId == "-1";

            // Game-specific configuration
            LoadGameSpecificConfiguration();

            bool enableFileLog     = GameConfigurationState.Instance.Overrides(nameof(GameConfigurationState.Instance.Logger.EnableFileLog)) ? GameConfigurationState.Instance.Logger.EnableFileLog : GlobalConfigurationState.Instance.Logger.EnableFileLog;
            bool enableErrorLog    = GameConfigurationState.Instance.Overrides(nameof(GameConfigurationState.Instance.Logger.EnableError)) ? GameConfigurationState.Instance.Logger.EnableError : GlobalConfigurationState.Instance.Logger.EnableError;
            bool enableWarnLog     = GameConfigurationState.Instance.Overrides(nameof(GameConfigurationState.Instance.Logger.EnableWarn)) ? GameConfigurationState.Instance.Logger.EnableWarn : GlobalConfigurationState.Instance.Logger.EnableWarn;
            bool enableInfoLog     = GameConfigurationState.Instance.Overrides(nameof(GameConfigurationState.Instance.Logger.EnableInfo)) ? GameConfigurationState.Instance.Logger.EnableInfo : GlobalConfigurationState.Instance.Logger.EnableInfo;
            bool enableStubLog     = GameConfigurationState.Instance.Overrides(nameof(GameConfigurationState.Instance.Logger.EnableStub)) ? GameConfigurationState.Instance.Logger.EnableStub : GlobalConfigurationState.Instance.Logger.EnableStub;
            bool enableDebugLog    = GameConfigurationState.Instance.Overrides(nameof(GameConfigurationState.Instance.Logger.EnableDebug)) ? GameConfigurationState.Instance.Logger.EnableDebug : GlobalConfigurationState.Instance.Logger.EnableDebug;
            bool enableGuestog     = GameConfigurationState.Instance.Overrides(nameof(GameConfigurationState.Instance.Logger.EnableGuest)) ? GameConfigurationState.Instance.Logger.EnableGuest : GlobalConfigurationState.Instance.Logger.EnableGuest;
            bool enableFsAccessLog = GameConfigurationState.Instance.Overrides(nameof(GameConfigurationState.Instance.Logger.EnableFsAccessLog)) ? GameConfigurationState.Instance.Logger.EnableFsAccessLog : GlobalConfigurationState.Instance.Logger.EnableFsAccessLog;

            _fileLogToggle.Active = enableFileLog;
            _errorLogToggle.Active = enableErrorLog;
            _warningLogToggle.Active = enableWarnLog;
            _infoLogToggle.Active = enableInfoLog;
            _stubLogToggle.Active = enableStubLog;
            _debugLogToggle.Active = enableDebugLog;
            _guestLogToggle.Active = enableGuestog;
            _fsAccessLogToggle.Active = enableFsAccessLog;
            
            foreach (GraphicsDebugLevel level in Enum.GetValues(typeof(GraphicsDebugLevel)))
            {
                _graphicsDebugLevel.Append(level.ToString(), level.ToString());
            }

            string graphicsDebugLevel    = GameConfigurationState.Instance.Overrides(nameof(GameConfigurationState.Instance.Logger.GraphicsDebugLevel)) ? GameConfigurationState.Instance.Logger.GraphicsDebugLevel.Value.ToString() : GlobalConfigurationState.Instance.Logger.GraphicsDebugLevel.Value.ToString();
            bool enableDockedMode        = GameConfigurationState.Instance.Overrides(nameof(GameConfigurationState.Instance.System.EnableDockedMode)) ? GameConfigurationState.Instance.System.EnableDockedMode : GlobalConfigurationState.Instance.System.EnableDockedMode;
            bool enablePtc               = GameConfigurationState.Instance.Overrides(nameof(GameConfigurationState.Instance.System.EnablePtc)) ? GameConfigurationState.Instance.System.EnablePtc : GlobalConfigurationState.Instance.System.EnablePtc;
            bool enableFsIntegrityChecks = GameConfigurationState.Instance.Overrides(nameof(GameConfigurationState.Instance.System.EnableFsIntegrityChecks)) ? GameConfigurationState.Instance.System.EnableFsIntegrityChecks : GlobalConfigurationState.Instance.System.EnableFsIntegrityChecks;
            bool enableVsync             = GameConfigurationState.Instance.Overrides(nameof(GameConfigurationState.Instance.Graphics.EnableVsync)) ? GameConfigurationState.Instance.Graphics.EnableVsync : GlobalConfigurationState.Instance.Graphics.EnableVsync;
            bool enableShaderCache       = GameConfigurationState.Instance.Overrides(nameof(GameConfigurationState.Instance.Graphics.EnableShaderCache)) ? GameConfigurationState.Instance.Graphics.EnableShaderCache : GlobalConfigurationState.Instance.Graphics.EnableShaderCache;
            bool ignoreMissingServices   = GameConfigurationState.Instance.Overrides(nameof(GameConfigurationState.Instance.System.IgnoreMissingServices)) ? GameConfigurationState.Instance.System.IgnoreMissingServices : GlobalConfigurationState.Instance.System.IgnoreMissingServices;
            bool enableKeyboard          = GameConfigurationState.Instance.Overrides(nameof(GameConfigurationState.Instance.Hid.EnableKeyboard)) ? GameConfigurationState.Instance.Hid.EnableKeyboard : GlobalConfigurationState.Instance.Hid.EnableKeyboard;

            _graphicsDebugLevel.SetActiveId(graphicsDebugLevel);

            _dockedModeToggle.Active = enableDockedMode;
            _vSyncToggle.Active = enableVsync;
            _shaderCacheToggle.Active = enableShaderCache;
            _ptcToggle.Active = enablePtc;
            _fsicToggle.Active = enableFsIntegrityChecks;
            _ignoreToggle.Active = ignoreMissingServices;
            _directKeyboardAccess.Active = enableKeyboard;

            _discordToggle.Active          = GlobalConfigurationState.Instance.EnableDiscordIntegration;
            _checkUpdatesToggle.Active     = GlobalConfigurationState.Instance.CheckUpdatesOnStart;
            _showConfirmExitToggle.Active  = GlobalConfigurationState.Instance.ShowConfirmExit;
            _hideCursorOnIdleToggle.Active = GlobalConfigurationState.Instance.HideCursorOnIdle;
            _custThemeToggle.Active        = GlobalConfigurationState.Instance.Ui.EnableCustomTheme;

            // Custom EntryCompletion Columns. If added to glade, need to override more signals
            ListStore tzList = new ListStore(typeof(string), typeof(string), typeof(string));
            _systemTimeZoneCompletion.Model = tzList;

            CellRendererText offsetCol = new CellRendererText();
            CellRendererText abbrevCol = new CellRendererText();

            _systemTimeZoneCompletion.PackStart(offsetCol, false);
            _systemTimeZoneCompletion.AddAttribute(offsetCol, "text", 0);
            _systemTimeZoneCompletion.TextColumn = 1; // Regions Column
            _systemTimeZoneCompletion.PackStart(abbrevCol, false);
            _systemTimeZoneCompletion.AddAttribute(abbrevCol, "text", 2);

            int maxLocationLength = 0;

            foreach (var (offset, location, abbr) in _timeZoneContentManager.ParseTzOffsets())
            {
                var hours = Math.DivRem(offset, 3600, out int seconds);
                var minutes = Math.Abs(seconds) / 60;

                var abbr2 = (abbr.StartsWith('+') || abbr.StartsWith('-')) ? string.Empty : abbr;

                tzList.AppendValues($"UTC{hours:+0#;-0#;+00}:{minutes:D2} ", location, abbr2);
                _validTzRegions.Add(location);

                maxLocationLength = Math.Max(maxLocationLength, location.Length);
            }

            _systemTimeZoneEntry.WidthChars = Math.Max(20, maxLocationLength + 1); // Ensure minimum Entry width
            _systemTimeZoneEntry.Text = _timeZoneContentManager.SanityCheckDeviceLocationName();

            _systemTimeZoneCompletion.MatchFunc = TimeZoneMatchFunc;

            string systemLanguage = GameConfigurationState.Instance.Overrides(nameof(GameConfigurationState.Instance.System.Language)) ? GameConfigurationState.Instance.System.Language.Value.ToString() : GlobalConfigurationState.Instance.System.Language.Value.ToString();
            string systemRegion = GameConfigurationState.Instance.Overrides(nameof(GameConfigurationState.Instance.System.Region)) ? GameConfigurationState.Instance.System.Region.Value.ToString() : GlobalConfigurationState.Instance.System.Region.Value.ToString();
            string resScale = GameConfigurationState.Instance.Overrides(nameof(GameConfigurationState.Instance.Graphics.ResScale)) ? GameConfigurationState.Instance.Graphics.ResScale.Value.ToString() : GlobalConfigurationState.Instance.Graphics.ResScale.Value.ToString();
            string anisotropy = GameConfigurationState.Instance.Overrides(nameof(GameConfigurationState.Instance.Graphics.MaxAnisotropy)) ? GameConfigurationState.Instance.Graphics.MaxAnisotropy.Value.ToString() : GlobalConfigurationState.Instance.Graphics.MaxAnisotropy.Value.ToString();
            int aspectRatio = GameConfigurationState.Instance.Overrides(nameof(GameConfigurationState.Instance.Graphics.AspectRatio)) ? (int)GameConfigurationState.Instance.Graphics.AspectRatio.Value: (int)GlobalConfigurationState.Instance.Graphics.AspectRatio.Value;
            
            string resScaleCustom = GameConfigurationState.Instance.Overrides(nameof(GameConfigurationState.Instance.Graphics.ResScaleCustom)) ? GameConfigurationState.Instance.Graphics.ResScaleCustom.Value.ToString() : GlobalConfigurationState.Instance.Graphics.ResScaleCustom.Value.ToString();
            ReactiveObject<string> shadersDumpPath = GameConfigurationState.Instance.Overrides(nameof(GameConfigurationState.Instance.Graphics.ShadersDumpPath)) ? GameConfigurationState.Instance.Graphics.ShadersDumpPath : GlobalConfigurationState.Instance.Graphics.ShadersDumpPath;
            ReactiveObject<int> fsGlobalAccessLogMode = GameConfigurationState.Instance.Overrides(nameof(GameConfigurationState.Instance.System.FsGlobalAccessLogMode)) ? GameConfigurationState.Instance.System.FsGlobalAccessLogMode : GlobalConfigurationState.Instance.System.FsGlobalAccessLogMode;
            ReactiveObject<long> systemTimeOffset = GameConfigurationState.Instance.Overrides(nameof(GameConfigurationState.Instance.System.SystemTimeOffset)) ? GameConfigurationState.Instance.System.SystemTimeOffset : GlobalConfigurationState.Instance.System.SystemTimeOffset;

            _systemLanguageSelect.SetActiveId(systemLanguage);
            _systemRegionSelect.SetActiveId(systemRegion);
            _resScaleCombo.SetActiveId(resScale);
            _anisotropy.SetActiveId(anisotropy);
            _aspectRatio.SetActiveId(aspectRatio.ToString());

            _custThemePath.Buffer.Text           = GlobalConfigurationState.Instance.Ui.CustomThemePath;
            _resScaleText.Buffer.Text            = resScaleCustom;
            _resScaleText.Visible                = _resScaleCombo.ActiveId == "-1";
            _graphicsShadersDumpPath.Buffer.Text = shadersDumpPath;
            _fsLogSpinAdjustment.Value           = fsGlobalAccessLogMode;
            _systemTimeOffset                    = systemTimeOffset;

            _gameDirsBox.AppendColumn("", new CellRendererText(), "text", 0);
            _gameDirsBoxStore  = new ListStore(typeof(string));
            _gameDirsBox.Model = _gameDirsBoxStore;

            foreach (string gameDir in GlobalConfigurationState.Instance.Ui.GameDirs.Value)
            {
                _gameDirsBoxStore.AppendValues(gameDir);
            }

            if (_custThemeToggle.Active == false)
            {
                _custThemePath.Sensitive      = false;
                _custThemePathLabel.Sensitive = false;
                _browseThemePath.Sensitive    = false;
            }

            //Setup system time spinners
            UpdateSystemTimeSpinners();

            _audioBackendStore = new ListStore(typeof(string), typeof(AudioBackend));

            TreeIter openAlIter  = _audioBackendStore.AppendValues("OpenAL", AudioBackend.OpenAl);
            TreeIter soundIoIter = _audioBackendStore.AppendValues("SoundIO", AudioBackend.SoundIo);
            TreeIter dummyIter   = _audioBackendStore.AppendValues("Dummy", AudioBackend.Dummy);

            _audioBackendSelect = ComboBox.NewWithModelAndEntry(_audioBackendStore);
            _audioBackendSelect.EntryTextColumn = 0;
            _audioBackendSelect.Entry.IsEditable = false;

            AudioBackend audioBackend = GameConfigurationState.Instance.Overrides(nameof(GameConfigurationState.Instance.System.AudioBackend)) ? GameConfigurationState.Instance.System.AudioBackend.Value : GlobalConfigurationState.Instance.System.AudioBackend.Value;
            switch (audioBackend)
            {
                case AudioBackend.OpenAl:
                    _audioBackendSelect.SetActiveIter(openAlIter);
                    break;
                case AudioBackend.SoundIo:
                    _audioBackendSelect.SetActiveIter(soundIoIter);
                    break;
                case AudioBackend.Dummy:
                    _audioBackendSelect.SetActiveIter(dummyIter);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            _audioBackendBox.Add(_audioBackendSelect);
            _audioBackendSelect.Show();

            bool openAlIsSupported  = false;
            bool soundIoIsSupported = false;

            Task.Run(() =>
            {
                openAlIsSupported  = OpenALHardwareDeviceDriver.IsSupported;
                soundIoIsSupported = SoundIoHardwareDeviceDriver.IsSupported;
            });

            // This function runs whenever the dropdown is opened
            _audioBackendSelect.SetCellDataFunc(_audioBackendSelect.Cells[0], (layout, cell, model, iter) =>
            {
                cell.Sensitive = ((AudioBackend)_audioBackendStore.GetValue(iter, 1)) switch
                {
                    AudioBackend.OpenAl  => openAlIsSupported,
                    AudioBackend.SoundIo => soundIoIsSupported,
                    AudioBackend.Dummy   => true,
                    _ => throw new ArgumentOutOfRangeException()
                };
            });

            if (gameTitle != null && gameId != null)
            {
                // Setup Override Event Listeners
                SetupOverrideEventListeners();
            }
        }

        private void UpdateSystemTimeSpinners()
        {
            //Bind system time events
            _systemTimeYearSpin.ValueChanged   -= SystemTimeSpin_ValueChanged;
            _systemTimeMonthSpin.ValueChanged  -= SystemTimeSpin_ValueChanged;
            _systemTimeDaySpin.ValueChanged    -= SystemTimeSpin_ValueChanged;
            _systemTimeHourSpin.ValueChanged   -= SystemTimeSpin_ValueChanged;
            _systemTimeMinuteSpin.ValueChanged -= SystemTimeSpin_ValueChanged;

            //Apply actual system time + SystemTimeOffset to system time spin buttons
            DateTime systemTime = DateTime.Now.AddSeconds(_systemTimeOffset);

            _systemTimeYearSpinAdjustment.Value   = systemTime.Year;
            _systemTimeMonthSpinAdjustment.Value  = systemTime.Month;
            _systemTimeDaySpinAdjustment.Value    = systemTime.Day;
            _systemTimeHourSpinAdjustment.Value   = systemTime.Hour;
            _systemTimeMinuteSpinAdjustment.Value = systemTime.Minute;

            //Format spin buttons text to include leading zeros
            _systemTimeYearSpin.Text   = systemTime.Year.ToString("0000");
            _systemTimeMonthSpin.Text  = systemTime.Month.ToString("00");
            _systemTimeDaySpin.Text    = systemTime.Day.ToString("00");
            _systemTimeHourSpin.Text   = systemTime.Hour.ToString("00");
            _systemTimeMinuteSpin.Text = systemTime.Minute.ToString("00");

            //Bind system time events
            _systemTimeYearSpin.ValueChanged   += SystemTimeSpin_ValueChanged;
            _systemTimeMonthSpin.ValueChanged  += SystemTimeSpin_ValueChanged;
            _systemTimeDaySpin.ValueChanged    += SystemTimeSpin_ValueChanged;
            _systemTimeHourSpin.ValueChanged   += SystemTimeSpin_ValueChanged;
            _systemTimeMinuteSpin.ValueChanged += SystemTimeSpin_ValueChanged;
        }

        private void SetupOverrideEventListeners()
        {
            _errorLogToggle.Clicked += (sender, EventArgs) => { AddOverride(nameof(GameConfigurationState.Instance.Logger.EnableError)); };
            _warningLogToggle.Clicked += (sender, EventArgs) => { AddOverride(nameof(GameConfigurationState.Instance.Logger.EnableWarn)); };
            _infoLogToggle.Clicked += (sender, EventArgs) => { AddOverride(nameof(GameConfigurationState.Instance.Logger.EnableInfo)); };
            _stubLogToggle.Clicked += (sender, EventArgs) => { AddOverride(nameof(GameConfigurationState.Instance.Logger.EnableStub)); };
            _debugLogToggle.Clicked += (sender, EventArgs) => { AddOverride(nameof(GameConfigurationState.Instance.Logger.EnableDebug)); };
            _fileLogToggle.Clicked += (sender, EventArgs) => { AddOverride(nameof(GameConfigurationState.Instance.Logger.EnableFileLog)); };
            _guestLogToggle.Clicked += (sender, EventArgs) => { AddOverride(nameof(GameConfigurationState.Instance.Logger.EnableGuest)); };
            _fsAccessLogToggle.Clicked += (sender, EventArgs) => { AddOverride(nameof(GameConfigurationState.Instance.Logger.EnableFsAccessLog)); };
            _dockedModeToggle.Clicked += (sender, EventArgs) => { AddOverride(nameof(GameConfigurationState.Instance.System.EnableDockedMode)); };
            _vSyncToggle.Clicked += (sender, EventArgs) => { AddOverride(nameof(GameConfigurationState.Instance.Graphics.EnableVsync)); };
            _shaderCacheToggle.Clicked += (sender, EventArgs) => { AddOverride(nameof(GameConfigurationState.Instance.Graphics.EnableShaderCache)); };
            _ptcToggle.Clicked += (sender, EventArgs) => { AddOverride(nameof(GameConfigurationState.Instance.System.EnablePtc)); };
            _fsicToggle.Clicked += (sender, EventArgs) => { AddOverride(nameof(GameConfigurationState.Instance.System.EnableFsIntegrityChecks)); };
            _ignoreToggle.Clicked += (sender, EventArgs) => { AddOverride(nameof(GameConfigurationState.Instance.System.IgnoreMissingServices)); };
            _directKeyboardAccess.Clicked += (sender, EventArgs) => { AddOverride(nameof(GameConfigurationState.Instance.Hid.EnableKeyboard)); };
            _systemLanguageSelect.Changed += (sender, EventArgs) => { AddOverride(nameof(GameConfigurationState.Instance.System.Language)); };
            _systemRegionSelect.Changed += (sender, EventArgs) => { AddOverride(nameof(GameConfigurationState.Instance.System.Region)); };
            _systemTimeZoneEntry.Changed += (sender, EventArgs) => { AddOverride(nameof(GameConfigurationState.Instance.System.TimeZone)); };
            _audioBackendSelect.Changed += (sender, EventArgs) => { AddOverride(nameof(GameConfigurationState.Instance.System.AudioBackend)); };
            _systemTimeYearSpin.Changed += (sender, EventArgs) => { AddOverride(nameof(GameConfigurationState.Instance.System.SystemTimeOffset)); }; 
            _systemTimeMonthSpin.Changed += (sender, EventArgs) => { AddOverride(nameof(GameConfigurationState.Instance.System.SystemTimeOffset)); }; 
            _systemTimeDaySpin.Changed += (sender, EventArgs) => { AddOverride(nameof(GameConfigurationState.Instance.System.SystemTimeOffset)); }; 
            _systemTimeHourSpin.Changed += (sender, EventArgs) => { AddOverride(nameof(GameConfigurationState.Instance.System.SystemTimeOffset)); };
            _systemTimeMinuteSpin.Changed += (sender, EventArgs) => { AddOverride(nameof(GameConfigurationState.Instance.System.SystemTimeOffset)); };
            _systemTimeYearSpinAdjustment.Changed += (sender, EventArgs) => { AddOverride(nameof(GameConfigurationState.Instance.System.SystemTimeOffset)); };
            _systemTimeMonthSpinAdjustment.Changed += (sender, EventArgs) => { AddOverride(nameof(GameConfigurationState.Instance.System.SystemTimeOffset)); };
            _systemTimeDaySpinAdjustment.Changed += (sender, EventArgs) => { AddOverride(nameof(GameConfigurationState.Instance.System.SystemTimeOffset)); };
            _systemTimeHourSpinAdjustment.Changed += (sender, EventArgs) => { AddOverride(nameof(GameConfigurationState.Instance.System.SystemTimeOffset)); };
            _systemTimeMinuteSpinAdjustment.Changed += (sender, EventArgs) => { AddOverride(nameof(GameConfigurationState.Instance.System.SystemTimeOffset)); };
            _graphicsShadersDumpPath.Changed += (sender, EventArgs) => { AddOverride(nameof(GameConfigurationState.Instance.Graphics.ShadersDumpPath)); };
            _anisotropy.Changed += (sender, EventArgs) => { AddOverride(nameof(GameConfigurationState.Instance.Graphics.MaxAnisotropy)); };
            _aspectRatio.Changed += (sender, EventArgs) => { AddOverride(nameof(GameConfigurationState.Instance.Graphics.AspectRatio)); };
            _resScaleCombo.Changed += (sender, EventArgs) => { AddOverride(nameof(GameConfigurationState.Instance.Graphics.ResScale)); };
            _resScaleText.Changed += (sender, EventArgs) => { AddOverride(nameof(GameConfigurationState.Instance.Graphics.ResScale)); };
        }

        private static void AddOverride(string configName)
        {
            GameConfigurationState.Instance.Override(configName);
        }

        private void LoadGameSpecificConfiguration()
        {
            GameConfigurationState.Instance.LoadDefault();

            // Game-Specific Configurations
            if (_gameTitle != null && _gameId != null)
            {
                Title += $" - {_gameTitle} ({_gameId})";

                GameConfigurationState.Load(_gameId);

                Widget[] disabledWidgets = new Widget[] { _discordToggle, _checkUpdatesToggle, _showConfirmExitToggle, _hideCursorOnIdleToggle, _gameDirsBox, _addGameDirBox, _custThemePath, _custThemeToggle};
                foreach(Widget widget in disabledWidgets)
                {
                    widget.Sensitive = false;
                }

                _resetToggle.Visible = true;
            }
        }

        private void SaveSettings()
        {
            if (_gameId != null)
            {
                if (!float.TryParse(_resScaleText.Buffer.Text, out float resScaleCustom) || resScaleCustom <= 0.0f)
                {
                    resScaleCustom = 1.0f;
                }

                if (_validTzRegions.Contains(_systemTimeZoneEntry.Text))
                {
                    GameConfigurationState.Instance.System.TimeZone.Value = _systemTimeZoneEntry.Text;
                }

                GameConfigurationState.Instance.Logger.EnableError.Value = _errorLogToggle.Active;
                GameConfigurationState.Instance.Logger.EnableWarn.Value = _warningLogToggle.Active;
                GameConfigurationState.Instance.Logger.EnableInfo.Value = _infoLogToggle.Active;
                GameConfigurationState.Instance.Logger.EnableStub.Value = _stubLogToggle.Active;
                GameConfigurationState.Instance.Logger.EnableDebug.Value = _debugLogToggle.Active;
                GameConfigurationState.Instance.Logger.EnableGuest.Value = _guestLogToggle.Active;
                GameConfigurationState.Instance.Logger.EnableFsAccessLog.Value = _fsAccessLogToggle.Active;
                GameConfigurationState.Instance.Logger.EnableFileLog.Value = _fileLogToggle.Active;
                GameConfigurationState.Instance.Logger.GraphicsDebugLevel.Value = Enum.Parse<GraphicsDebugLevel>(_graphicsDebugLevel.ActiveId);
                GameConfigurationState.Instance.System.EnableDockedMode.Value = _dockedModeToggle.Active;
                GameConfigurationState.Instance.Graphics.EnableVsync.Value = _vSyncToggle.Active;
                GameConfigurationState.Instance.Graphics.EnableShaderCache.Value = _shaderCacheToggle.Active;
                GameConfigurationState.Instance.System.EnablePtc.Value = _ptcToggle.Active;
                GameConfigurationState.Instance.System.EnableFsIntegrityChecks.Value = _fsicToggle.Active;
                GameConfigurationState.Instance.System.IgnoreMissingServices.Value = _ignoreToggle.Active;
                GameConfigurationState.Instance.Hid.EnableKeyboard.Value = _directKeyboardAccess.Active;
                GameConfigurationState.Instance.System.Language.Value = Enum.Parse<Language>(_systemLanguageSelect.ActiveId);
                GameConfigurationState.Instance.System.Region.Value = Enum.Parse<Configuration.System.Region>(_systemRegionSelect.ActiveId);
                GameConfigurationState.Instance.System.SystemTimeOffset.Value = _systemTimeOffset;
                GameConfigurationState.Instance.Graphics.ShadersDumpPath.Value = _graphicsShadersDumpPath.Buffer.Text;
                GameConfigurationState.Instance.System.FsGlobalAccessLogMode.Value = (int)_fsLogSpinAdjustment.Value;
                GameConfigurationState.Instance.Graphics.MaxAnisotropy.Value = float.Parse(_anisotropy.ActiveId, CultureInfo.InvariantCulture);
                GameConfigurationState.Instance.Graphics.AspectRatio.Value = Enum.Parse<AspectRatio>(_aspectRatio.ActiveId);
                GameConfigurationState.Instance.Graphics.ResScale.Value = int.Parse(_resScaleCombo.ActiveId);
                GameConfigurationState.Instance.Graphics.ResScaleCustom.Value = resScaleCustom;

                if (_audioBackendSelect.GetActiveIter(out TreeIter activeIter))
                {
                    AudioBackend audioBackend = (AudioBackend)_audioBackendStore.GetValue(activeIter, 1);
                    if (audioBackend != GameConfigurationState.Instance.System.AudioBackend.Value)
                    {
                        GameConfigurationState.Instance.System.AudioBackend.Value = audioBackend;

                        Logger.Info?.Print(LogClass.Application, $"AudioBackend toggled to: {audioBackend}");
                    }
                }

                GameConfigurationState.Save(_gameId);

                _parent.UpdateGraphicsConfig();
            }
            else
            {
                List<string> gameDirs = new List<string>();

                _gameDirsBoxStore.GetIterFirst(out TreeIter treeIter);
                for (int i = 0; i < _gameDirsBoxStore.IterNChildren(); i++)
                {
                    gameDirs.Add((string)_gameDirsBoxStore.GetValue(treeIter, 0));

                    _gameDirsBoxStore.IterNext(ref treeIter);
                }

                if (!float.TryParse(_resScaleText.Buffer.Text, out float resScaleCustom) || resScaleCustom <= 0.0f)
                {
                    resScaleCustom = 1.0f;
                }

                if (_validTzRegions.Contains(_systemTimeZoneEntry.Text))
                {
                    GlobalConfigurationState.Instance.System.TimeZone.Value = _systemTimeZoneEntry.Text;
                }

                GlobalConfigurationState.Instance.Logger.EnableError.Value = _errorLogToggle.Active;
                GlobalConfigurationState.Instance.Logger.EnableWarn.Value = _warningLogToggle.Active;
                GlobalConfigurationState.Instance.Logger.EnableInfo.Value = _infoLogToggle.Active;
                GlobalConfigurationState.Instance.Logger.EnableStub.Value = _stubLogToggle.Active;
                GlobalConfigurationState.Instance.Logger.EnableDebug.Value = _debugLogToggle.Active;
                GlobalConfigurationState.Instance.Logger.EnableGuest.Value = _guestLogToggle.Active;
                GlobalConfigurationState.Instance.Logger.EnableFsAccessLog.Value = _fsAccessLogToggle.Active;
                GlobalConfigurationState.Instance.Logger.EnableFileLog.Value = _fileLogToggle.Active;
                GlobalConfigurationState.Instance.Logger.GraphicsDebugLevel.Value = Enum.Parse<GraphicsDebugLevel>(_graphicsDebugLevel.ActiveId);
                GlobalConfigurationState.Instance.System.EnableDockedMode.Value = _dockedModeToggle.Active;
                GlobalConfigurationState.Instance.EnableDiscordIntegration.Value = _discordToggle.Active;
                GlobalConfigurationState.Instance.CheckUpdatesOnStart.Value = _checkUpdatesToggle.Active;
                GlobalConfigurationState.Instance.ShowConfirmExit.Value = _showConfirmExitToggle.Active;
                GlobalConfigurationState.Instance.HideCursorOnIdle.Value = _hideCursorOnIdleToggle.Active;
                GlobalConfigurationState.Instance.Graphics.EnableVsync.Value = _vSyncToggle.Active;
                GlobalConfigurationState.Instance.Graphics.EnableShaderCache.Value = _shaderCacheToggle.Active;
                GlobalConfigurationState.Instance.System.EnablePtc.Value = _ptcToggle.Active;
                GlobalConfigurationState.Instance.System.EnableFsIntegrityChecks.Value = _fsicToggle.Active;
                GlobalConfigurationState.Instance.System.IgnoreMissingServices.Value = _ignoreToggle.Active;
                GlobalConfigurationState.Instance.Hid.EnableKeyboard.Value = _directKeyboardAccess.Active;
                GlobalConfigurationState.Instance.Ui.EnableCustomTheme.Value = _custThemeToggle.Active;
                GlobalConfigurationState.Instance.System.Language.Value = Enum.Parse<Language>(_systemLanguageSelect.ActiveId);
                GlobalConfigurationState.Instance.System.Region.Value = Enum.Parse<Configuration.System.Region>(_systemRegionSelect.ActiveId);
                GlobalConfigurationState.Instance.System.SystemTimeOffset.Value = _systemTimeOffset;
                GlobalConfigurationState.Instance.Ui.CustomThemePath.Value = _custThemePath.Buffer.Text;
                GlobalConfigurationState.Instance.Graphics.ShadersDumpPath.Value = _graphicsShadersDumpPath.Buffer.Text;
                GlobalConfigurationState.Instance.Ui.GameDirs.Value = gameDirs;
                GlobalConfigurationState.Instance.System.FsGlobalAccessLogMode.Value = (int)_fsLogSpinAdjustment.Value;
                GlobalConfigurationState.Instance.Graphics.MaxAnisotropy.Value = float.Parse(_anisotropy.ActiveId, CultureInfo.InvariantCulture);
                GlobalConfigurationState.Instance.Graphics.AspectRatio.Value = Enum.Parse<AspectRatio>(_aspectRatio.ActiveId);
                GlobalConfigurationState.Instance.Graphics.ResScale.Value = int.Parse(_resScaleCombo.ActiveId);
                GlobalConfigurationState.Instance.Graphics.ResScaleCustom.Value = resScaleCustom;

                if (_audioBackendSelect.GetActiveIter(out TreeIter activeIter))
                {
                    GlobalConfigurationState.Instance.System.AudioBackend.Value = (AudioBackend)_audioBackendStore.GetValue(activeIter, 1);
                }

                GlobalConfigurationState.Instance.ToFileFormat().SaveConfig(Program.ConfigurationPath);
                _parent.UpdateGraphicsConfig();
                ThemeHelper.ApplyTheme();
            }
        }

        //
        // Events
        //
        private void TimeZoneEntry_FocusOut(object sender, FocusOutEventArgs e)
        {
            if (!_validTzRegions.Contains(_systemTimeZoneEntry.Text))
            {
                _systemTimeZoneEntry.Text = _timeZoneContentManager.SanityCheckDeviceLocationName();
            }
        }

        private bool TimeZoneMatchFunc(EntryCompletion compl, string key, TreeIter iter)
        {
            key = key.Trim().Replace(' ', '_');

            return ((string)compl.Model.GetValue(iter, 1)).Contains(key, StringComparison.OrdinalIgnoreCase) || // region
                   ((string)compl.Model.GetValue(iter, 2)).StartsWith(key, StringComparison.OrdinalIgnoreCase) || // abbr
                   ((string)compl.Model.GetValue(iter, 0))[3..].StartsWith(key); // offset
        }

        private void SystemTimeSpin_ValueChanged(object sender, EventArgs e)
        {
            int year   = _systemTimeYearSpin.ValueAsInt;
            int month  = _systemTimeMonthSpin.ValueAsInt;
            int day    = _systemTimeDaySpin.ValueAsInt;
            int hour   = _systemTimeHourSpin.ValueAsInt;
            int minute = _systemTimeMinuteSpin.ValueAsInt;

            if (!DateTime.TryParse(year + "-" + month + "-" + day + " " + hour + ":" + minute, out DateTime newTime))
            {
                UpdateSystemTimeSpinners();

                return;
            }

            newTime = newTime.AddSeconds(DateTime.Now.Second).AddMilliseconds(DateTime.Now.Millisecond);

            long systemTimeOffset = (long)Math.Ceiling((newTime - DateTime.Now).TotalMinutes) * 60L;

            if (_systemTimeOffset != systemTimeOffset)
            {
                _systemTimeOffset = systemTimeOffset;
                UpdateSystemTimeSpinners();
            }
        }

        private void AddDir_Pressed(object sender, EventArgs args)
        {
            if (Directory.Exists(_addGameDirBox.Buffer.Text))
            {
                _gameDirsBoxStore.AppendValues(_addGameDirBox.Buffer.Text);
            }
            else
            {
                FileChooserDialog fileChooser = new FileChooserDialog("Choose the game directory to add to the list", this, FileChooserAction.SelectFolder, "Cancel", ResponseType.Cancel, "Add", ResponseType.Accept)
                {
                    SelectMultiple = true
                };

                if (fileChooser.Run() == (int)ResponseType.Accept)
                {
                    foreach (string directory in fileChooser.Filenames)
                    {
                        bool directoryAdded = false;

                        if (_gameDirsBoxStore.GetIterFirst(out TreeIter treeIter))
                        {
                            do
                            {
                                if (directory.Equals((string)_gameDirsBoxStore.GetValue(treeIter, 0)))
                                {
                                    directoryAdded = true;
                                    break;
                                }
                            } while(_gameDirsBoxStore.IterNext(ref treeIter));
                        }

                        if (!directoryAdded)
                        {
                            _gameDirsBoxStore.AppendValues(directory);
                        }
                    }
                }

                fileChooser.Dispose();
            }

            _addGameDirBox.Buffer.Text = "";

            ((ToggleButton)sender).SetStateFlags(StateFlags.Normal, true);
        }

        private void RemoveDir_Pressed(object sender, EventArgs args)
        {
            TreeSelection selection = _gameDirsBox.Selection;

            if (selection.GetSelected(out TreeIter treeIter))
            {
                _gameDirsBoxStore.Remove(ref treeIter);
            }

            ((ToggleButton)sender).SetStateFlags(StateFlags.Normal, true);
        }

        private void CustThemeToggle_Activated(object sender, EventArgs args)
        {
            _custThemePath.Sensitive      = _custThemeToggle.Active;
            _custThemePathLabel.Sensitive = _custThemeToggle.Active;
            _browseThemePath.Sensitive    = _custThemeToggle.Active;
        }

        private void BrowseThemeDir_Pressed(object sender, EventArgs args)
        {
            using (FileChooserDialog fileChooser = new FileChooserDialog("Choose the theme to load", this, FileChooserAction.Open, "Cancel", ResponseType.Cancel, "Select", ResponseType.Accept))
            {
                fileChooser.Filter = new FileFilter();
                fileChooser.Filter.AddPattern("*.css");

                if (fileChooser.Run() == (int)ResponseType.Accept)
                {
                    _custThemePath.Buffer.Text = fileChooser.Filename;
                }
            }

            _browseThemePath.SetStateFlags(StateFlags.Normal, true);
        }

        private void ConfigureController_Pressed(object sender, PlayerIndex playerIndex)
        {
            ((ToggleButton)sender).SetStateFlags(StateFlags.Normal, true);

            ControllerWindow controllerWindow = new ControllerWindow(playerIndex, _gameTitle, _gameId);

            controllerWindow.SetSizeRequest((int)(controllerWindow.DefaultWidth * Program.WindowScaleFactor), (int)(controllerWindow.DefaultHeight * Program.WindowScaleFactor));
            controllerWindow.Show();
        }

        private void SaveToggle_Activated(object sender, EventArgs args)
        {
            SaveSettings();
            Dispose();
        }

        private void ApplyToggle_Activated(object sender, EventArgs args)
        {
            SaveSettings();
        }

        private void ResetToggle_Activated(object sender, EventArgs args)
        {
            GameConfigurationState.Instance.LoadDefault();
            GameConfigurationState.Save(_gameId);

            Dispose();
        }

        private void CloseToggle_Activated(object sender, EventArgs args)
        {
            Dispose();
        }
    }
}
