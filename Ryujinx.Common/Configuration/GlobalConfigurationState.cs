using Ryujinx.Common;
using Ryujinx.Common.Configuration;
using Ryujinx.Common.Configuration.ConfigurationStateSection;
using Ryujinx.Common.Configuration.Hid;
using Ryujinx.Common.Logging;
using Ryujinx.Configuration.Hid;
using Ryujinx.Configuration.System;
using Ryujinx.Configuration.Ui;
using System;
using System.Collections.Generic;

namespace Ryujinx.Configuration
{
    public class GlobalConfigurationState
    {
        /// <summary>
        /// The default configuration instance
        /// </summary>
        public static GlobalConfigurationState Instance { get; private set; }

        /// <summary>
        /// The Ui section
        /// </summary>
        public UiSection Ui { get; private set; }

        /// <summary>
        /// The Logger section
        /// </summary>
        public LoggerSection Logger { get; private set; }

        /// <summary>
        /// The System section
        /// </summary>
        public SystemSection System { get; private set; }

        /// <summary>
        /// The Graphics section
        /// </summary>
        public GraphicsSection Graphics { get; private set; }

        /// <summary>
        /// The Hid section
        /// </summary>
        public HidSection Hid { get; private set; }

        /// <summary>
        /// Enables or disables Discord Rich Presence
        /// </summary>
        public ReactiveObject<bool> EnableDiscordIntegration { get; private set; }

        /// <summary>
        /// Checks for updates when Ryujinx starts when enabled
        /// </summary>
        public ReactiveObject<bool> CheckUpdatesOnStart { get; private set; }

        /// <summary>
        /// Show "Confirm Exit" Dialog
        /// </summary>
        public ReactiveObject<bool> ShowConfirmExit { get; private set; }

        /// <summary>
        /// Hide Cursor on Idle
        /// </summary>
        public ReactiveObject<bool> HideCursorOnIdle { get; private set; }

        private GlobalConfigurationState()
        {
            Ui                       = new UiSection();
            Logger                   = new LoggerSection();
            System                   = new SystemSection();
            Graphics                 = new GraphicsSection();
            Hid                      = new HidSection();
            EnableDiscordIntegration = new ReactiveObject<bool>();
            CheckUpdatesOnStart      = new ReactiveObject<bool>();
            ShowConfirmExit          = new ReactiveObject<bool>();
            HideCursorOnIdle         = new ReactiveObject<bool>();
        }

        public ConfigurationFileFormat ToFileFormat()
        {
            List<ControllerConfig> controllerConfigList = new List<ControllerConfig>();
            List<KeyboardConfig>   keyboardConfigList   = new List<KeyboardConfig>();

            foreach (InputConfig inputConfig in Hid.InputConfig.Value)
            {
                if (inputConfig is ControllerConfig controllerConfig)
                {
                    controllerConfigList.Add(controllerConfig);
                }
                else if (inputConfig is KeyboardConfig keyboardConfig)
                {
                    keyboardConfigList.Add(keyboardConfig);
                }
            }

            ConfigurationFileFormat configurationFile = new ConfigurationFileFormat
            {
                Version                   = ConfigurationFileFormat.CurrentVersion,
                ResScale                  = Graphics.ResScale,
                ResScaleCustom            = Graphics.ResScaleCustom,
                MaxAnisotropy             = Graphics.MaxAnisotropy,
                AspectRatio               = Graphics.AspectRatio,
                GraphicsShadersDumpPath   = Graphics.ShadersDumpPath,
                LoggingEnableDebug        = Logger.EnableDebug,
                LoggingEnableStub         = Logger.EnableStub,
                LoggingEnableInfo         = Logger.EnableInfo,
                LoggingEnableWarn         = Logger.EnableWarn,
                LoggingEnableError        = Logger.EnableError,
                LoggingEnableGuest        = Logger.EnableGuest,
                LoggingEnableFsAccessLog  = Logger.EnableFsAccessLog,
                LoggingFilteredClasses    = Logger.FilteredClasses,
                LoggingGraphicsDebugLevel = Logger.GraphicsDebugLevel,
                EnableFileLog             = Logger.EnableFileLog,
                SystemLanguage            = System.Language,
                SystemRegion              = System.Region,
                SystemTimeZone            = System.TimeZone,
                SystemTimeOffset          = System.SystemTimeOffset,
                DockedMode                = System.EnableDockedMode,
                EnableDiscordIntegration  = EnableDiscordIntegration,
                CheckUpdatesOnStart       = CheckUpdatesOnStart,
                ShowConfirmExit           = ShowConfirmExit,
                HideCursorOnIdle          = HideCursorOnIdle,
                EnableVsync               = Graphics.EnableVsync,
                EnableShaderCache         = Graphics.EnableShaderCache,
                EnablePtc                 = System.EnablePtc,
                EnableFsIntegrityChecks   = System.EnableFsIntegrityChecks,
                FsGlobalAccessLogMode     = System.FsGlobalAccessLogMode,
                AudioBackend              = System.AudioBackend,
                IgnoreMissingServices     = System.IgnoreMissingServices,
                GuiColumns                = new GuiColumns
                {
                    FavColumn        = Ui.GuiColumns.FavColumn,
                    IconColumn       = Ui.GuiColumns.IconColumn,
                    AppColumn        = Ui.GuiColumns.AppColumn,
                    DevColumn        = Ui.GuiColumns.DevColumn,
                    VersionColumn    = Ui.GuiColumns.VersionColumn,
                    TimePlayedColumn = Ui.GuiColumns.TimePlayedColumn,
                    LastPlayedColumn = Ui.GuiColumns.LastPlayedColumn,
                    FileExtColumn    = Ui.GuiColumns.FileExtColumn,
                    FileSizeColumn   = Ui.GuiColumns.FileSizeColumn,
                    PathColumn       = Ui.GuiColumns.PathColumn,
                },
                ColumnSort                = new ColumnSort
                {
                    SortColumnId  = Ui.ColumnSort.SortColumnId,
                    SortAscending = Ui.ColumnSort.SortAscending
                },
                GameDirs                  = Ui.GameDirs,
                EnableCustomTheme         = Ui.EnableCustomTheme,
                CustomThemePath           = Ui.CustomThemePath,
                StartFullscreen           = Ui.StartFullscreen,
                EnableKeyboard            = Hid.EnableKeyboard,
                Hotkeys                   = Hid.Hotkeys,
                KeyboardConfig            = keyboardConfigList,
                ControllerConfig          = controllerConfigList
            };

            return configurationFile;
        }

        public void LoadDefault()
        {
            Graphics.ResScale.Value                = 1;
            Graphics.ResScaleCustom.Value          = 1.0f;
            Graphics.MaxAnisotropy.Value           = -1.0f;
            Graphics.AspectRatio.Value             = AspectRatio.Fixed16x9;
            Graphics.ShadersDumpPath.Value         = "";
            Logger.EnableDebug.Value               = false;
            Logger.EnableStub.Value                = true;
            Logger.EnableInfo.Value                = true;
            Logger.EnableWarn.Value                = true;
            Logger.EnableError.Value               = true;
            Logger.EnableGuest.Value               = true;
            Logger.EnableFsAccessLog.Value         = false;
            Logger.FilteredClasses.Value           = Array.Empty<LogClass>();
            Logger.GraphicsDebugLevel.Value        = GraphicsDebugLevel.None;
            Logger.EnableFileLog.Value             = true;
            System.Language.Value                  = Language.AmericanEnglish;
            System.Region.Value                    = Region.USA;
            System.TimeZone.Value                  = "UTC";
            System.SystemTimeOffset.Value          = 0;
            System.EnableDockedMode.Value          = true;
            EnableDiscordIntegration.Value         = true;
            CheckUpdatesOnStart.Value              = true;
            ShowConfirmExit.Value                  = true;
            HideCursorOnIdle.Value                 = false;
            Graphics.EnableVsync.Value             = true;
            Graphics.EnableShaderCache.Value       = true;
            System.EnablePtc.Value                 = true;
            System.EnableFsIntegrityChecks.Value   = true;
            System.FsGlobalAccessLogMode.Value     = 0;
            System.AudioBackend.Value              = AudioBackend.OpenAl;
            System.IgnoreMissingServices.Value     = false;
            Ui.GuiColumns.FavColumn.Value          = true;
            Ui.GuiColumns.IconColumn.Value         = true;
            Ui.GuiColumns.AppColumn.Value          = true;
            Ui.GuiColumns.DevColumn.Value          = true;
            Ui.GuiColumns.VersionColumn.Value      = true;
            Ui.GuiColumns.TimePlayedColumn.Value   = true;
            Ui.GuiColumns.LastPlayedColumn.Value   = true;
            Ui.GuiColumns.FileExtColumn.Value      = true;
            Ui.GuiColumns.FileSizeColumn.Value     = true;
            Ui.GuiColumns.PathColumn.Value         = true;
            Ui.ColumnSort.SortColumnId.Value       = 0;
            Ui.ColumnSort.SortAscending.Value      = false;
            Ui.GameDirs.Value                      = new List<string>();
            Ui.EnableCustomTheme.Value             = false;
            Ui.CustomThemePath.Value               = "";
            Ui.StartFullscreen.Value               = false;
            Hid.EnableKeyboard.Value               = false;
            Hid.Hotkeys.Value = new KeyboardHotkeys
            {
                ToggleVsync = Key.Tab
            };
            Hid.InputConfig.Value = new List<InputConfig>
            {
                new KeyboardConfig
                {
                    Index          = 0,
                    ControllerType = ControllerType.JoyconPair,
                    PlayerIndex    = PlayerIndex.Player1,
                    LeftJoycon     = new NpadKeyboardLeft
                    {
                        StickUp     = Key.W,
                        StickDown   = Key.S,
                        StickLeft   = Key.A,
                        StickRight  = Key.D,
                        StickButton = Key.F,
                        DPadUp      = Key.Up,
                        DPadDown    = Key.Down,
                        DPadLeft    = Key.Left,
                        DPadRight   = Key.Right,
                        ButtonMinus = Key.Minus,
                        ButtonL     = Key.E,
                        ButtonZl    = Key.Q,
                        ButtonSl    = Key.Home,
                        ButtonSr    = Key.End
                    },
                    RightJoycon    = new NpadKeyboardRight
                    {
                        StickUp     = Key.I,
                        StickDown   = Key.K,
                        StickLeft   = Key.J,
                        StickRight  = Key.L,
                        StickButton = Key.H,
                        ButtonA     = Key.Z,
                        ButtonB     = Key.X,
                        ButtonX     = Key.C,
                        ButtonY     = Key.V,
                        ButtonPlus  = Key.Plus,
                        ButtonR     = Key.U,
                        ButtonZr    = Key.O,
                        ButtonSl    = Key.PageUp,
                        ButtonSr    = Key.PageDown
                    },
                    EnableMotion  = false,
                    MirrorInput   = false,
                    Slot          = 0,
                    AltSlot       = 0,
                    Sensitivity   = 100,
                    GyroDeadzone  = 1,
                    DsuServerHost = "127.0.0.1",
                    DsuServerPort = 26760
                }
            };
        }

        public void Load(ConfigurationFileFormat configurationFileFormat, string configurationFilePath)
        {
            bool configurationFileUpdated = false;

            if (configurationFileFormat.Version < 0 || configurationFileFormat.Version > ConfigurationFileFormat.CurrentVersion)
            {
                Common.Logging.Logger.Warning?.Print(LogClass.Application, $"Unsupported configuration version {configurationFileFormat.Version}, loading default.");

                LoadDefault();

                return;
            }

            if (configurationFileFormat.Version < 2)
            {
                Common.Logging.Logger.Warning?.Print(LogClass.Application, $"Outdated configuration version {configurationFileFormat.Version}, migrating to version 2.");

                configurationFileFormat.SystemRegion = Region.USA;

                configurationFileUpdated = true;
            }

            if (configurationFileFormat.Version < 3)
            {
                Common.Logging.Logger.Warning?.Print(LogClass.Application, $"Outdated configuration version {configurationFileFormat.Version}, migrating to version 3.");

                configurationFileFormat.SystemTimeZone = "UTC";

                configurationFileUpdated = true;
            }

            if (configurationFileFormat.Version < 4)
            {
                Common.Logging.Logger.Warning?.Print(LogClass.Application, $"Outdated configuration version {configurationFileFormat.Version}, migrating to version 4.");

                configurationFileFormat.MaxAnisotropy = -1;

                configurationFileUpdated = true;
            }

            if (configurationFileFormat.Version < 5)
            {
                Common.Logging.Logger.Warning?.Print(LogClass.Application, $"Outdated configuration version {configurationFileFormat.Version}, migrating to version 5.");

                configurationFileFormat.SystemTimeOffset = 0;

                configurationFileUpdated = true;
            }

            if (configurationFileFormat.Version < 6)
            {
                Common.Logging.Logger.Warning?.Print(LogClass.Application, $"Outdated configuration version {configurationFileFormat.Version}, migrating to version 6.");

                configurationFileFormat.ControllerConfig = new List<ControllerConfig>();
                configurationFileFormat.KeyboardConfig   = new List<KeyboardConfig>
                {
                    new KeyboardConfig
                    {
                        Index          = 0,
                        ControllerType = ControllerType.JoyconPair,
                        PlayerIndex    = PlayerIndex.Player1,
                        LeftJoycon     = new NpadKeyboardLeft
                        {
                            StickUp     = Key.W,
                            StickDown   = Key.S,
                            StickLeft   = Key.A,
                            StickRight  = Key.D,
                            StickButton = Key.F,
                            DPadUp      = Key.Up,
                            DPadDown    = Key.Down,
                            DPadLeft    = Key.Left,
                            DPadRight   = Key.Right,
                            ButtonMinus = Key.Minus,
                            ButtonL     = Key.E,
                            ButtonZl    = Key.Q,
                            ButtonSl    = Key.Unbound,
                            ButtonSr    = Key.Unbound
                        },
                        RightJoycon    = new NpadKeyboardRight
                        {
                            StickUp     = Key.I,
                            StickDown   = Key.K,
                            StickLeft   = Key.J,
                            StickRight  = Key.L,
                            StickButton = Key.H,
                            ButtonA     = Key.Z,
                            ButtonB     = Key.X,
                            ButtonX     = Key.C,
                            ButtonY     = Key.V,
                            ButtonPlus  = Key.Plus,
                            ButtonR     = Key.U,
                            ButtonZr    = Key.O,
                            ButtonSl    = Key.Unbound,
                            ButtonSr    = Key.Unbound
                        },
                        EnableMotion  = false,
                        MirrorInput   = false,
                        Slot          = 0,
                        AltSlot       = 0,
                        Sensitivity   = 100,
                        GyroDeadzone  = 1,
                        DsuServerHost = "127.0.0.1",
                        DsuServerPort = 26760
                    }
                };

                configurationFileUpdated = true;
            }

            // Only needed for version 6 configurations.
            if (configurationFileFormat.Version == 6)
            {
                Common.Logging.Logger.Warning?.Print(LogClass.Application, $"Outdated configuration version {configurationFileFormat.Version}, migrating to version 7.");

                for (int i = 0; i < configurationFileFormat.KeyboardConfig.Count; i++)
                {
                    if (configurationFileFormat.KeyboardConfig[i].Index != KeyboardConfig.AllKeyboardsIndex)
                    {
                        configurationFileFormat.KeyboardConfig[i].Index++;
                    }
                }
            }

            if (configurationFileFormat.Version < 8)
            {
                Common.Logging.Logger.Warning?.Print(LogClass.Application, $"Outdated configuration version {configurationFileFormat.Version}, migrating to version 8.");

                configurationFileFormat.EnablePtc = true;

                configurationFileUpdated = true;
            }

            if (configurationFileFormat.Version < 9)
            {
                Common.Logging.Logger.Warning?.Print(LogClass.Application, $"Outdated configuration version {configurationFileFormat.Version}, migrating to version 9.");

                configurationFileFormat.ColumnSort = new ColumnSort
                {
                    SortColumnId  = 0,
                    SortAscending = false
                };

                configurationFileFormat.Hotkeys = new KeyboardHotkeys
                {
                    ToggleVsync = Key.Tab
                };

                configurationFileUpdated = true;
            }

            if (configurationFileFormat.Version < 10)
            {
                Common.Logging.Logger.Warning?.Print(LogClass.Application, $"Outdated configuration version {configurationFileFormat.Version}, migrating to version 10.");

                configurationFileFormat.AudioBackend = AudioBackend.OpenAl;

                configurationFileUpdated = true;
            }

            if (configurationFileFormat.Version < 11)
            {
                Common.Logging.Logger.Warning?.Print(LogClass.Application, $"Outdated configuration version {configurationFileFormat.Version}, migrating to version 11.");

                configurationFileFormat.ResScale = 1;
                configurationFileFormat.ResScaleCustom = 1.0f;

                configurationFileUpdated = true;
            }

            if (configurationFileFormat.Version < 12)
            {
                Common.Logging.Logger.Warning?.Print(LogClass.Application, $"Outdated configuration version {configurationFileFormat.Version}, migrating to version 12.");

                configurationFileFormat.LoggingGraphicsDebugLevel = GraphicsDebugLevel.None;

                configurationFileUpdated = true;
            }

            if (configurationFileFormat.Version < 14)
            {
                Common.Logging.Logger.Warning?.Print(LogClass.Application, $"Outdated configuration version {configurationFileFormat.Version}, migrating to version 14.");

                configurationFileFormat.CheckUpdatesOnStart = true;

                configurationFileUpdated = true;
            }

            if (configurationFileFormat.Version < 16)
            {
                Common.Logging.Logger.Warning?.Print(LogClass.Application, $"Outdated configuration version {configurationFileFormat.Version}, migrating to version 16.");

                configurationFileFormat.EnableShaderCache = true;

                configurationFileUpdated = true;
            }

            if (configurationFileFormat.Version < 17)
            {
                Common.Logging.Logger.Warning?.Print(LogClass.Application, $"Outdated configuration version {configurationFileFormat.Version}, migrating to version 17.");

                configurationFileFormat.StartFullscreen = false;

                configurationFileUpdated = true;
            }

            if (configurationFileFormat.Version < 18)
            {
                Common.Logging.Logger.Warning?.Print(LogClass.Application, $"Outdated configuration version {configurationFileFormat.Version}, migrating to version 18.");

                configurationFileFormat.AspectRatio = AspectRatio.Fixed16x9;

                configurationFileUpdated = true;
            }

            if (configurationFileFormat.Version < 20)
            {
                Common.Logging.Logger.Warning?.Print(LogClass.Application, $"Outdated configuration version {configurationFileFormat.Version}, migrating to version 20.");

                configurationFileFormat.ShowConfirmExit = true;

                configurationFileUpdated = true;
            }

            if (configurationFileFormat.Version < 22)
            {
                Common.Logging.Logger.Warning?.Print(LogClass.Application, $"Outdated configuration version {configurationFileFormat.Version}, migrating to version 22.");

                configurationFileFormat.HideCursorOnIdle = false;

                configurationFileUpdated = true;
            }

            List<InputConfig> inputConfig = new List<InputConfig>();
            inputConfig.AddRange(configurationFileFormat.ControllerConfig);
            inputConfig.AddRange(configurationFileFormat.KeyboardConfig);

            Graphics.ResScale.Value                = configurationFileFormat.ResScale;
            Graphics.ResScaleCustom.Value          = configurationFileFormat.ResScaleCustom;
            Graphics.MaxAnisotropy.Value           = configurationFileFormat.MaxAnisotropy;
            Graphics.AspectRatio.Value             = configurationFileFormat.AspectRatio;
            Graphics.ShadersDumpPath.Value         = configurationFileFormat.GraphicsShadersDumpPath;
            Logger.EnableDebug.Value               = configurationFileFormat.LoggingEnableDebug;
            Logger.EnableStub.Value                = configurationFileFormat.LoggingEnableStub;
            Logger.EnableInfo.Value                = configurationFileFormat.LoggingEnableInfo;
            Logger.EnableWarn.Value                = configurationFileFormat.LoggingEnableWarn;
            Logger.EnableError.Value               = configurationFileFormat.LoggingEnableError;
            Logger.EnableGuest.Value               = configurationFileFormat.LoggingEnableGuest;
            Logger.EnableFsAccessLog.Value         = configurationFileFormat.LoggingEnableFsAccessLog;
            Logger.FilteredClasses.Value           = configurationFileFormat.LoggingFilteredClasses;
            Logger.GraphicsDebugLevel.Value        = configurationFileFormat.LoggingGraphicsDebugLevel;
            Logger.EnableFileLog.Value             = configurationFileFormat.EnableFileLog;
            System.Language.Value                  = configurationFileFormat.SystemLanguage;
            System.Region.Value                    = configurationFileFormat.SystemRegion;
            System.TimeZone.Value                  = configurationFileFormat.SystemTimeZone;
            System.SystemTimeOffset.Value          = configurationFileFormat.SystemTimeOffset;
            System.EnableDockedMode.Value          = configurationFileFormat.DockedMode;
            EnableDiscordIntegration.Value         = configurationFileFormat.EnableDiscordIntegration;
            CheckUpdatesOnStart.Value              = configurationFileFormat.CheckUpdatesOnStart;
            ShowConfirmExit.Value                  = configurationFileFormat.ShowConfirmExit;
            HideCursorOnIdle.Value                 = configurationFileFormat.HideCursorOnIdle;
            Graphics.EnableVsync.Value             = configurationFileFormat.EnableVsync;
            Graphics.EnableShaderCache.Value       = configurationFileFormat.EnableShaderCache;
            System.EnablePtc.Value                 = configurationFileFormat.EnablePtc;
            System.EnableFsIntegrityChecks.Value   = configurationFileFormat.EnableFsIntegrityChecks;
            System.FsGlobalAccessLogMode.Value     = configurationFileFormat.FsGlobalAccessLogMode;
            System.AudioBackend.Value              = configurationFileFormat.AudioBackend;
            System.IgnoreMissingServices.Value     = configurationFileFormat.IgnoreMissingServices;
            Ui.GuiColumns.FavColumn.Value          = configurationFileFormat.GuiColumns.FavColumn;
            Ui.GuiColumns.IconColumn.Value         = configurationFileFormat.GuiColumns.IconColumn;
            Ui.GuiColumns.AppColumn.Value          = configurationFileFormat.GuiColumns.AppColumn;
            Ui.GuiColumns.DevColumn.Value          = configurationFileFormat.GuiColumns.DevColumn;
            Ui.GuiColumns.VersionColumn.Value      = configurationFileFormat.GuiColumns.VersionColumn;
            Ui.GuiColumns.TimePlayedColumn.Value   = configurationFileFormat.GuiColumns.TimePlayedColumn;
            Ui.GuiColumns.LastPlayedColumn.Value   = configurationFileFormat.GuiColumns.LastPlayedColumn;
            Ui.GuiColumns.FileExtColumn.Value      = configurationFileFormat.GuiColumns.FileExtColumn;
            Ui.GuiColumns.FileSizeColumn.Value     = configurationFileFormat.GuiColumns.FileSizeColumn;
            Ui.GuiColumns.PathColumn.Value         = configurationFileFormat.GuiColumns.PathColumn;
            Ui.ColumnSort.SortColumnId.Value       = configurationFileFormat.ColumnSort.SortColumnId;
            Ui.ColumnSort.SortAscending.Value      = configurationFileFormat.ColumnSort.SortAscending;
            Ui.GameDirs.Value                      = configurationFileFormat.GameDirs;
            Ui.EnableCustomTheme.Value             = configurationFileFormat.EnableCustomTheme;
            Ui.CustomThemePath.Value               = configurationFileFormat.CustomThemePath;
            Ui.StartFullscreen.Value               = configurationFileFormat.StartFullscreen;
            Hid.EnableKeyboard.Value               = configurationFileFormat.EnableKeyboard;
            Hid.Hotkeys.Value                      = configurationFileFormat.Hotkeys;
            Hid.InputConfig.Value                  = inputConfig;

            if (configurationFileUpdated)
            {
                ToFileFormat().SaveConfig(configurationFilePath);

                Common.Logging.Logger.Notice.Print(LogClass.Application, $"Configuration file updated to version {ConfigurationFileFormat.CurrentVersion}");
            }
        }

        public static void Initialize()
        {
            if (Instance != null)
            {
                throw new InvalidOperationException("Configuration is already initialized");
            }

            Instance = new GlobalConfigurationState();
        }
    }
}
