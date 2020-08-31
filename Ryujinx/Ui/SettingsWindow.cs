using Gtk;
using Ryujinx.Audio;
using Ryujinx.Configuration;
using Ryujinx.Common.Configuration;
using Ryujinx.Common.Configuration.Hid;
using Ryujinx.Configuration.System;
using Ryujinx.HLE.HOS.Applets;
using Ryujinx.HLE.HOS.Services.Time.TimeZone;
using Ryujinx.HLE.FileSystem;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using GUI = Gtk.Builder.ObjectAttribute;

namespace Ryujinx.Ui
{
    public class SettingsWindow : Window
    {
        private readonly VirtualFileSystem      _virtualFileSystem;
        private readonly ListStore              _gameDirsBoxStore;
        private readonly ListStore              _audioBackendStore;
        private readonly TimeZoneContentManager _timeZoneContentManager;
        private readonly HashSet<string>        _validTzRegions;
        private readonly ManualResetEvent       _closeEvent;
        private readonly bool[] _enabledControllers;

        private long _systemTimeOffset;

#pragma warning disable CS0649, IDE0044
        [GUI] Notebook        _notebook;
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
        [GUI] CheckButton     _vSyncToggle;
        [GUI] CheckButton     _multiSchedToggle;
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
        [GUI] ComboBoxText    _resScaleCombo;
        [GUI] Entry           _resScaleText;
        [GUI] Label           _playerLabel1;
        [GUI] Label           _playerLabel2;
        [GUI] Label           _playerLabel3;
        [GUI] Label           _playerLabel4;
        [GUI] Label           _playerLabel5;
        [GUI] Label           _playerLabel6;
        [GUI] Label           _playerLabel7;
        [GUI] Label           _playerLabel8;
        [GUI] Label           _playerLabelH;
        [GUI] Label           _controllerLabel1;
        [GUI] Label           _controllerLabel2;
        [GUI] Label           _controllerLabel3;
        [GUI] Label           _controllerLabel4;
        [GUI] Label           _controllerLabel5;
        [GUI] Label           _controllerLabel6;
        [GUI] Label           _controllerLabel7;
        [GUI] Label           _controllerLabel8;
        [GUI] Label           _controllerLabelH;
        [GUI] ToggleButton    _playerButton1;
        [GUI] ToggleButton    _playerButton2;
        [GUI] ToggleButton    _playerButton3;
        [GUI] ToggleButton    _playerButton4;
        [GUI] ToggleButton    _playerButton5;
        [GUI] ToggleButton    _playerButton6;
        [GUI] ToggleButton    _playerButton7;
        [GUI] ToggleButton    _playerButton8;
        [GUI] ToggleButton    _playerButtonH;
        [GUI] Label           _inputHelpText;
#pragma warning restore CS0649, IDE0044

        public static bool IsOpen { get; private set; }

        public SettingsWindow(VirtualFileSystem virtualFileSystem, HLE.FileSystem.Content.ContentManager contentManager) : this(new Builder("Ryujinx.Ui.SettingsWindow.glade"), virtualFileSystem, contentManager) { }

        public SettingsWindow(VirtualFileSystem virtualFileSystem, HLE.FileSystem.Content.ContentManager contentManager, ControllerAppletUiArgs args, ManualResetEvent closeEvent) : this(new Builder("Ryujinx.Ui.SettingsWindow.glade"), virtualFileSystem, contentManager)
        {
            _closeEvent = closeEvent;

            // Select Input and disable other tabs
            for (int i = 0; i < 5; i++)
            {
                _notebook.GetNthPage(i).Sensitive = false;
            }
            _notebook.CurrentPage = 1;
            _notebook.GetNthPage(1).Sensitive = true;

            string playerCount = args.IsSinglePlayer
                ? $"<b>single-player mode</b>"
                : args.PlayerCountMin == args.PlayerCountMax
                ? $"<b>exactly {args.PlayerCountMin}</b> player(s)"
                : $"<b>{args.PlayerCountMin}-{args.PlayerCountMax}</b> player(s)";

            string exactPair = (!args.PermitJoyDual && args.SupportedStyles == (HLE.HOS.Services.Hid.ControllerType.JoyconLeft | HLE.HOS.Services.Hid.ControllerType.JoyconRight))
                ? "<tt> (only in left-right pairs)</tt>"
                : "";

            string message =
                $"Application requests {playerCount} with:\n\n"
                + $"<tt><b>TYPES:</b> {args.SupportedStyles}</tt>{exactPair}";

            _inputHelpText.Visible = true;
            _inputHelpText.Markup = message;

            Label[] labels = new Label[]
            {
                _playerLabel1, _playerLabel2, _playerLabel3, _playerLabel4,
                _playerLabel5, _playerLabel6, _playerLabel7, _playerLabel8
            };

            Button[] buttons = new Button[]
            {
                _playerButton1, _playerButton2, _playerButton3, _playerButton4,
                _playerButton5, _playerButton6, _playerButton7, _playerButton8
            };

            for (int i = 0; i < labels.Length; ++i)
            {
                string foreground = "";
                if (i < args.IdentificationColors.Length)
                {
                    uint color = args.IdentificationColors[i];
                    if ((color >> 24) != 0)
                    {
                        foreground = $"foreground='#{color & 0xff:X2}{(color >> 8) & 0xff:X2}{(color >> 16) & 0xff:X2}'";
                    }
                }

                string text = $"<span weight='bold' {foreground}>Player {i + 1}</span>";

                if (i >= args.PlayerCountMax)
                {
                    labels[i].Sensitive = false;
                    if (!_enabledControllers[i]) buttons[i].Sensitive = false;
                }

                if (i < args.ExplainTexts.Length && !string.IsNullOrWhiteSpace(args.ExplainTexts[i]))
                {
                    text += $"\n{args.ExplainTexts[i]}";
                }

                labels[i].Markup = text;
            }

            if (!args.IsSinglePlayer || args.IsDocked || (args.SupportedStyles & HLE.HOS.Services.Hid.ControllerType.Handheld) == 0)
            {
                _playerLabelH.Sensitive = false;
                if (!_enabledControllers[(int)PlayerIndex.Handheld]) _playerButtonH.Sensitive = false;
            }
        }

        private SettingsWindow(Builder builder, VirtualFileSystem virtualFileSystem, HLE.FileSystem.Content.ContentManager contentManager) : base(builder.GetObject("_settingsWin").Handle)
        {
            builder.Autoconnect(this);

            this.Icon = new Gdk.Pixbuf(Assembly.GetExecutingAssembly(), "Ryujinx.Ui.assets.Icon.png");

            _virtualFileSystem = virtualFileSystem;

            _timeZoneContentManager = new TimeZoneContentManager();
            _timeZoneContentManager.InitializeInstance(virtualFileSystem, contentManager, LibHac.FsSystem.IntegrityCheckLevel.None);

            _validTzRegions = new HashSet<string>(_timeZoneContentManager.LocationNameCache.Length, StringComparer.Ordinal); // Zone regions are identifiers. Must match exactly.

            IsOpen = true;

            //Bind Events
            DeleteEvent += (sender, args) => CloseToggle_Activated(sender, null);
            ConfigurationState.Instance.Hid.InputConfig.Event += InputConfig_Changed;

            _playerButton1.Pressed += (sender, args) => ConfigureController_Pressed(sender, args, PlayerIndex.Player1);
            _playerButton2.Pressed += (sender, args) => ConfigureController_Pressed(sender, args, PlayerIndex.Player2);
            _playerButton3.Pressed += (sender, args) => ConfigureController_Pressed(sender, args, PlayerIndex.Player3);
            _playerButton4.Pressed += (sender, args) => ConfigureController_Pressed(sender, args, PlayerIndex.Player4);
            _playerButton5.Pressed += (sender, args) => ConfigureController_Pressed(sender, args, PlayerIndex.Player5);
            _playerButton6.Pressed += (sender, args) => ConfigureController_Pressed(sender, args, PlayerIndex.Player6);
            _playerButton7.Pressed += (sender, args) => ConfigureController_Pressed(sender, args, PlayerIndex.Player7);
            _playerButton8.Pressed += (sender, args) => ConfigureController_Pressed(sender, args, PlayerIndex.Player8);
            _playerButtonH.Pressed += (sender, args) => ConfigureController_Pressed(sender, args, PlayerIndex.Handheld);
            _systemTimeZoneEntry.FocusOutEvent += TimeZoneEntry_FocusOut;

            _resScaleCombo.Changed += (sender, args) => _resScaleText.Visible = _resScaleCombo.ActiveId == "-1";

            //Setup Currents
            if (ConfigurationState.Instance.Logger.EnableFileLog)
            {
                _fileLogToggle.Click();
            }

            if (ConfigurationState.Instance.Logger.EnableError)
            {
                _errorLogToggle.Click();
            }

            if (ConfigurationState.Instance.Logger.EnableWarn)
            {
                _warningLogToggle.Click();
            }

            if (ConfigurationState.Instance.Logger.EnableInfo)
            {
                _infoLogToggle.Click();
            }

            if (ConfigurationState.Instance.Logger.EnableStub)
            {
                _stubLogToggle.Click();
            }

            if (ConfigurationState.Instance.Logger.EnableDebug)
            {
                _debugLogToggle.Click();
            }

            if (ConfigurationState.Instance.Logger.EnableGuest)
            {
                _guestLogToggle.Click();
            }

            if (ConfigurationState.Instance.Logger.EnableFsAccessLog)
            {
                _fsAccessLogToggle.Click();
            }

            foreach (GraphicsDebugLevel level in Enum.GetValues(typeof(GraphicsDebugLevel)))
            {
                _graphicsDebugLevel.Append(level.ToString(), level.ToString());
            }

            _graphicsDebugLevel.SetActiveId(ConfigurationState.Instance.Logger.GraphicsDebugLevel.Value.ToString());

            if (ConfigurationState.Instance.System.EnableDockedMode)
            {
                _dockedModeToggle.Click();
            }

            if (ConfigurationState.Instance.EnableDiscordIntegration)
            {
                _discordToggle.Click();
            }

            if (ConfigurationState.Instance.Graphics.EnableVsync)
            {
                _vSyncToggle.Click();
            }

            if (ConfigurationState.Instance.System.EnableMulticoreScheduling)
            {
                _multiSchedToggle.Click();
            }

            if (ConfigurationState.Instance.System.EnablePtc)
            {
                _ptcToggle.Click();
            }

            if (ConfigurationState.Instance.System.EnableFsIntegrityChecks)
            {
                _fsicToggle.Click();
            }

            if (ConfigurationState.Instance.System.IgnoreMissingServices)
            {
                _ignoreToggle.Click();
            }

            if (ConfigurationState.Instance.Hid.EnableKeyboard)
            {
                _directKeyboardAccess.Click();
            }

            if (ConfigurationState.Instance.Ui.EnableCustomTheme)
            {
                _custThemeToggle.Click();
            }

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

            _systemLanguageSelect.SetActiveId(ConfigurationState.Instance.System.Language.Value.ToString());
            _systemRegionSelect.SetActiveId(ConfigurationState.Instance.System.Region.Value.ToString());
            _resScaleCombo.SetActiveId(ConfigurationState.Instance.Graphics.ResScale.Value.ToString());
            _anisotropy.SetActiveId(ConfigurationState.Instance.Graphics.MaxAnisotropy.Value.ToString());

            _custThemePath.Buffer.Text           = ConfigurationState.Instance.Ui.CustomThemePath;
            _resScaleText.Buffer.Text            = ConfigurationState.Instance.Graphics.ResScaleCustom.Value.ToString();
            _resScaleText.Visible                = _resScaleCombo.ActiveId == "-1";
            _graphicsShadersDumpPath.Buffer.Text = ConfigurationState.Instance.Graphics.ShadersDumpPath;
            _fsLogSpinAdjustment.Value           = ConfigurationState.Instance.System.FsGlobalAccessLogMode;
            _systemTimeOffset                    = ConfigurationState.Instance.System.SystemTimeOffset;

            _gameDirsBox.AppendColumn("", new CellRendererText(), "text", 0);
            _gameDirsBoxStore  = new ListStore(typeof(string));
            _gameDirsBox.Model = _gameDirsBoxStore;

            foreach (string gameDir in ConfigurationState.Instance.Ui.GameDirs.Value)
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

            switch (ConfigurationState.Instance.System.AudioBackend.Value)
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
                openAlIsSupported  = OpenALAudioOut.IsSupported;
                soundIoIsSupported = SoundIoAudioOut.IsSupported;
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

            _enabledControllers = new bool[(int)PlayerIndex.Handheld + 1];
            UpdateInputLabels();
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

        //Events
        private void TimeZoneEntry_FocusOut(Object sender, FocusOutEventArgs e)
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
                   ((string)compl.Model.GetValue(iter, 0)).Substring(3).StartsWith(key); // offset
        }

        private void UpdateInputLabels()
        {
            Label[] labels = new Label[]
            { 
                _controllerLabel1, _controllerLabel2, _controllerLabel3, _controllerLabel4,
                _controllerLabel5, _controllerLabel6, _controllerLabel7, _controllerLabel8,
                _controllerLabelH
            };

            _enabledControllers.AsSpan().Clear();

            foreach (var label in labels)
            {
                label.Text = "Disabled";
            }

            foreach(var input in ConfigurationState.Instance.Hid.InputConfig.Value)
            {
                if (input.PlayerIndex == PlayerIndex.Handheld)
                {
                    _enabledControllers[(int)PlayerIndex.Handheld] = true;
                    _controllerLabelH.Text = "Enabled";
                    continue;
                }

                if ((int)input.PlayerIndex < labels.Length)
                {
                    _enabledControllers[(int)input.PlayerIndex] = true;
                    labels[(int)input.PlayerIndex].Text = input.ControllerType switch
                    {
                        ControllerType.JoyconPair => "Joycon Pair",
                        ControllerType.JoyconLeft => "Joycon Left",
                        ControllerType.JoyconRight => "Joycon Right",
                        ControllerType.ProController => "Pro Controller",
                        _ => "Unknown"
                    };
                }
            }
        }

        private void InputConfig_Changed(object sender, Common.ReactiveEventArgs<List<InputConfig>> args)
        {
            UpdateInputLabels();
        }

        private void SystemTimeSpin_ValueChanged(Object sender, EventArgs e)
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

            ((ToggleButton)sender).SetStateFlags(0, true);
        }

        private void RemoveDir_Pressed(object sender, EventArgs args)
        {
            TreeSelection selection = _gameDirsBox.Selection;

            if (selection.GetSelected(out TreeIter treeIter))
            {
                _gameDirsBoxStore.Remove(ref treeIter);
            }

            ((ToggleButton)sender).SetStateFlags(0, true);
        }

        private void CustThemeToggle_Activated(object sender, EventArgs args)
        {
            _custThemePath.Sensitive      = _custThemeToggle.Active;
            _custThemePathLabel.Sensitive = _custThemeToggle.Active;
            _browseThemePath.Sensitive    = _custThemeToggle.Active;
        }

        private void BrowseThemeDir_Pressed(object sender, EventArgs args)
        {
            FileChooserDialog fileChooser = new FileChooserDialog("Choose the theme to load", this, FileChooserAction.Open, "Cancel", ResponseType.Cancel, "Select", ResponseType.Accept);

            fileChooser.Filter = new FileFilter();
            fileChooser.Filter.AddPattern("*.css");

            if (fileChooser.Run() == (int)ResponseType.Accept)
            {
                _custThemePath.Buffer.Text = fileChooser.Filename;
            }

            fileChooser.Dispose();

            _browseThemePath.SetStateFlags(0, true);
        }

        private void OpenLogsFolder_Pressed(object sender, EventArgs args)
        {
            string logPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            
            DirectoryInfo directory = new DirectoryInfo(logPath);
            directory.Create();
            
            Process.Start(new ProcessStartInfo()
            {
                FileName        = logPath,
                UseShellExecute = true,
                Verb            = "open"
            });
        }

        private void ConfigureController_Pressed(object sender, EventArgs args, PlayerIndex playerIndex)
        {
            ((ToggleButton)sender).SetStateFlags(0, true);

            ControllerWindow controllerWin = new ControllerWindow(playerIndex, _virtualFileSystem);
            controllerWin.Show();
        }

        private void SaveToggle_Activated(object sender, EventArgs args)
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
                ConfigurationState.Instance.System.TimeZone.Value = _systemTimeZoneEntry.Text;
            }

            ConfigurationState.Instance.Logger.EnableError.Value               = _errorLogToggle.Active;
            ConfigurationState.Instance.Logger.EnableWarn.Value                = _warningLogToggle.Active;
            ConfigurationState.Instance.Logger.EnableInfo.Value                = _infoLogToggle.Active;
            ConfigurationState.Instance.Logger.EnableStub.Value                = _stubLogToggle.Active;
            ConfigurationState.Instance.Logger.EnableDebug.Value               = _debugLogToggle.Active;
            ConfigurationState.Instance.Logger.EnableGuest.Value               = _guestLogToggle.Active;
            ConfigurationState.Instance.Logger.EnableFsAccessLog.Value         = _fsAccessLogToggle.Active;
            ConfigurationState.Instance.Logger.EnableFileLog.Value             = _fileLogToggle.Active;
            ConfigurationState.Instance.Logger.GraphicsDebugLevel.Value        = Enum.Parse<GraphicsDebugLevel>(_graphicsDebugLevel.ActiveId);
            ConfigurationState.Instance.System.EnableDockedMode.Value          = _dockedModeToggle.Active;
            ConfigurationState.Instance.EnableDiscordIntegration.Value         = _discordToggle.Active;
            ConfigurationState.Instance.Graphics.EnableVsync.Value             = _vSyncToggle.Active;
            ConfigurationState.Instance.System.EnableMulticoreScheduling.Value = _multiSchedToggle.Active;
            ConfigurationState.Instance.System.EnablePtc.Value                 = _ptcToggle.Active;
            ConfigurationState.Instance.System.EnableFsIntegrityChecks.Value   = _fsicToggle.Active;
            ConfigurationState.Instance.System.IgnoreMissingServices.Value     = _ignoreToggle.Active;
            ConfigurationState.Instance.Hid.EnableKeyboard.Value               = _directKeyboardAccess.Active;
            ConfigurationState.Instance.Ui.EnableCustomTheme.Value             = _custThemeToggle.Active;
            ConfigurationState.Instance.System.Language.Value                  = Enum.Parse<Language>(_systemLanguageSelect.ActiveId);
            ConfigurationState.Instance.System.Region.Value                    = Enum.Parse<Configuration.System.Region>(_systemRegionSelect.ActiveId);
            ConfigurationState.Instance.System.SystemTimeOffset.Value          = _systemTimeOffset;
            ConfigurationState.Instance.Ui.CustomThemePath.Value               = _custThemePath.Buffer.Text;
            ConfigurationState.Instance.Graphics.ShadersDumpPath.Value         = _graphicsShadersDumpPath.Buffer.Text;
            ConfigurationState.Instance.Ui.GameDirs.Value                      = gameDirs;
            ConfigurationState.Instance.System.FsGlobalAccessLogMode.Value     = (int)_fsLogSpinAdjustment.Value;
            ConfigurationState.Instance.Graphics.MaxAnisotropy.Value           = float.Parse(_anisotropy.ActiveId);
            ConfigurationState.Instance.Graphics.ResScale.Value                = int.Parse(_resScaleCombo.ActiveId);
            ConfigurationState.Instance.Graphics.ResScaleCustom.Value          = resScaleCustom;

            if (_audioBackendSelect.GetActiveIter(out TreeIter activeIter))
            {
                ConfigurationState.Instance.System.AudioBackend.Value = (AudioBackend)_audioBackendStore.GetValue(activeIter, 1);
            }

            MainWindow.SaveConfig();
            MainWindow.UpdateGraphicsConfig();
            MainWindow.ApplyTheme();
            IsOpen = false;
            ConfigurationState.Instance.Hid.InputConfig.Event -= InputConfig_Changed;
            _closeEvent?.Set();
            Dispose();
        }

        private void CloseToggle_Activated(object sender, EventArgs args)
        {
            IsOpen = false;
            ConfigurationState.Instance.Hid.InputConfig.Event -= InputConfig_Changed;
            _closeEvent?.Set();
            Dispose();
        }
    }
}
