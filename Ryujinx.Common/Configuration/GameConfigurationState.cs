using Ryujinx.Common.Configuration.Hid;
using Ryujinx.Common.Logging;
using Ryujinx.Configuration;
using Ryujinx.Configuration.Hid;
using Ryujinx.Configuration.System;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Ryujinx.Configuration.ConfigurationState;

namespace Ryujinx.Common.Configuration
{
    class GameConfigurationState
    {
        /// <summary>
        /// Logger configuration section
        /// </summary>
        public class LoggerSection
        {
            /// <summary>
            /// Enables printing debug log messages
            /// </summary>
            public ReactiveObject<bool> EnableDebug { get; private set; }

            /// <summary>
            /// Enables printing stub log messages
            /// </summary>
            public ReactiveObject<bool> EnableStub { get; private set; }

            /// <summary>
            /// Enables printing info log messages
            /// </summary>
            public ReactiveObject<bool> EnableInfo { get; private set; }

            /// <summary>
            /// Enables printing warning log messages
            /// </summary>
            public ReactiveObject<bool> EnableWarn { get; private set; }

            /// <summary>
            /// Enables printing error log messages
            /// </summary>
            public ReactiveObject<bool> EnableError { get; private set; }

            /// <summary>
            /// Enables printing guest log messages
            /// </summary>
            public ReactiveObject<bool> EnableGuest { get; private set; }

            /// <summary>
            /// Enables printing FS access log messages
            /// </summary>
            public ReactiveObject<bool> EnableFsAccessLog { get; private set; }

            /// <summary>
            /// Controls which log messages are written to the log targets
            /// </summary>
            public ReactiveObject<LogClass[]> FilteredClasses { get; private set; }

            /// <summary>
            /// Enables or disables logging to a file on disk
            /// </summary>
            public ReactiveObject<bool> EnableFileLog { get; private set; }

            /// <summary>
            /// Controls which OpenGL log messages are recorded in the log
            /// </summary>
            public ReactiveObject<GraphicsDebugLevel> GraphicsDebugLevel { get; private set; }

            public LoggerSection()
            {
                EnableDebug = new ReactiveObject<bool>();
                EnableStub = new ReactiveObject<bool>();
                EnableInfo = new ReactiveObject<bool>();
                EnableWarn = new ReactiveObject<bool>();
                EnableError = new ReactiveObject<bool>();
                EnableGuest = new ReactiveObject<bool>();
                EnableFsAccessLog = new ReactiveObject<bool>();
                FilteredClasses = new ReactiveObject<LogClass[]>();
                EnableFileLog = new ReactiveObject<bool>();
                GraphicsDebugLevel = new ReactiveObject<GraphicsDebugLevel>();
            }
        }

        /// <summary>
        /// System configuration section
        /// </summary>
        public class SystemSection
        {
            /// <summary>
            /// Change System Language
            /// </summary>
            public ReactiveObject<Language> Language { get; private set; }

            /// <summary>
            /// Change System Region
            /// </summary>
            public ReactiveObject<Region> Region { get; private set; }

            /// <summary>
            /// Change System TimeZone
            /// </summary>
            public ReactiveObject<string> TimeZone { get; private set; }

            /// <summary>
            /// System Time Offset in Seconds
            /// </summary>
            public ReactiveObject<long> SystemTimeOffset { get; private set; }

            /// <summary>
            /// Enables or disables Docked Mode
            /// </summary>
            public ReactiveObject<bool> EnableDockedMode { get; private set; }

            /// <summary>
            /// Enables or disables profiled translation cache persistency
            /// </summary>
            public ReactiveObject<bool> EnablePtc { get; private set; }

            /// <summary>
            /// Enables integrity checks on Game content files
            /// </summary>
            public ReactiveObject<bool> EnableFsIntegrityChecks { get; private set; }

            /// <summary>
            /// Enables FS access log output to the console. Possible modes are 0-3
            /// </summary>
            public ReactiveObject<int> FsGlobalAccessLogMode { get; private set; }

            /// <summary>
            /// The selected audio backend
            /// </summary>
            public ReactiveObject<AudioBackend> AudioBackend { get; private set; }

            /// <summary>
            /// Enable or disable ignoring missing services
            /// </summary>
            public ReactiveObject<bool> IgnoreMissingServices { get; private set; }

            public SystemSection()
            {
                Language = new ReactiveObject<Language>();
                Region = new ReactiveObject<Region>();
                TimeZone = new ReactiveObject<string>();
                SystemTimeOffset = new ReactiveObject<long>();
                EnableDockedMode = new ReactiveObject<bool>();
                EnablePtc = new ReactiveObject<bool>();
                EnableFsIntegrityChecks = new ReactiveObject<bool>();
                FsGlobalAccessLogMode = new ReactiveObject<int>();
                AudioBackend = new ReactiveObject<AudioBackend>();
                IgnoreMissingServices = new ReactiveObject<bool>();
            }
        }

        /// <summary>
        /// Hid configuration section
        /// </summary>
        public class HidSection
        {
            /// <summary>
            /// Enable or disable keyboard support (Independent from controllers binding)
            /// </summary>
            public ReactiveObject<bool> EnableKeyboard { get; private set; }

            /// <summary>
            /// Hotkey Keyboard Bindings
            /// </summary>
            public ReactiveObject<KeyboardHotkeys> Hotkeys { get; private set; }

            /// <summary>
            /// Input device configuration.
            /// NOTE: This ReactiveObject won't issue an event when the List has elements added or removed.
            /// TODO: Implement a ReactiveList class.
            /// </summary>
            public ReactiveObject<List<InputConfig>> InputConfig { get; private set; }

            public HidSection()
            {
                EnableKeyboard = new ReactiveObject<bool>();
                Hotkeys = new ReactiveObject<KeyboardHotkeys>();
                InputConfig = new ReactiveObject<List<InputConfig>>();
            }
        }

        /// <summary>
        /// Graphics configuration section
        /// </summary>
        public class GraphicsSection
        {
            /// <summary>
            /// Max Anisotropy. Values range from 0 - 16. Set to -1 to let the game decide.
            /// </summary>
            public ReactiveObject<float> MaxAnisotropy { get; private set; }

            /// <summary>
            /// Aspect Ratio applied to the renderer window.
            /// </summary>
            public ReactiveObject<AspectRatio> AspectRatio { get; private set; }

            /// <summary>
            /// Resolution Scale. An integer scale applied to applicable render targets. Values 1-4, or -1 to use a custom floating point scale instead.
            /// </summary>
            public ReactiveObject<int> ResScale { get; private set; }

            /// <summary>
            /// Custom Resolution Scale. A custom floating point scale applied to applicable render targets. Only active when Resolution Scale is -1.
            /// </summary>
            public ReactiveObject<float> ResScaleCustom { get; private set; }

            /// <summary>
            /// Dumps shaders in this local directory
            /// </summary>
            public ReactiveObject<string> ShadersDumpPath { get; private set; }

            /// <summary>
            /// Enables or disables Vertical Sync
            /// </summary>
            public ReactiveObject<bool> EnableVsync { get; private set; }

            /// <summary>
            /// Enables or disables Shader cache
            /// </summary>
            public ReactiveObject<bool> EnableShaderCache { get; private set; }

            public GraphicsSection()
            {
                ResScale = new ReactiveObject<int>();
                ResScaleCustom = new ReactiveObject<float>();
                MaxAnisotropy = new ReactiveObject<float>();
                AspectRatio = new ReactiveObject<AspectRatio>();
                ShadersDumpPath = new ReactiveObject<string>();
                EnableVsync = new ReactiveObject<bool>();
                EnableShaderCache = new ReactiveObject<bool>();
            }
        }

        /// <summary>
        /// The default configuration instance
        /// </summary>
        public static GameConfigurationState Instance { get; private set; }

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

        private GameConfigurationState()
        {
            Logger = new LoggerSection();
            System = new SystemSection();
            Graphics = new GraphicsSection();
            Hid = new HidSection();
        }

        public ConfigurationFileFormat ToFileFormat()
        {
            List<ControllerConfig> controllerConfigList = new List<ControllerConfig>();
            List<KeyboardConfig> keyboardConfigList = new List<KeyboardConfig>();

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
                Version = ConfigurationFileFormat.CurrentVersion,
                ResScale = Graphics.ResScale,
                ResScaleCustom = Graphics.ResScaleCustom,
                MaxAnisotropy = Graphics.MaxAnisotropy,
                AspectRatio = Graphics.AspectRatio,
                GraphicsShadersDumpPath = Graphics.ShadersDumpPath,
                LoggingEnableDebug = Logger.EnableDebug,
                LoggingEnableStub = Logger.EnableStub,
                LoggingEnableInfo = Logger.EnableInfo,
                LoggingEnableWarn = Logger.EnableWarn,
                LoggingEnableError = Logger.EnableError,
                LoggingEnableGuest = Logger.EnableGuest,
                LoggingEnableFsAccessLog = Logger.EnableFsAccessLog,
                LoggingFilteredClasses = Logger.FilteredClasses,
                LoggingGraphicsDebugLevel = Logger.GraphicsDebugLevel,
                EnableFileLog = Logger.EnableFileLog,
                SystemLanguage = System.Language,
                SystemRegion = System.Region,
                SystemTimeZone = System.TimeZone,
                SystemTimeOffset = System.SystemTimeOffset,
                DockedMode = System.EnableDockedMode,
                EnableVsync = Graphics.EnableVsync,
                EnableShaderCache = Graphics.EnableShaderCache,
                EnablePtc = System.EnablePtc,
                EnableFsIntegrityChecks = System.EnableFsIntegrityChecks,
                FsGlobalAccessLogMode = System.FsGlobalAccessLogMode,
                AudioBackend = System.AudioBackend,
                IgnoreMissingServices = System.IgnoreMissingServices,
                EnableKeyboard = Hid.EnableKeyboard,
                Hotkeys = Hid.Hotkeys,
                KeyboardConfig = keyboardConfigList,
                ControllerConfig = controllerConfigList
            };

            return configurationFile;
        }

        public void LoadDefault()
        {
            Graphics.ResScale.Value = ConfigurationState.Instance.Graphics.ResScale.Value;
            Graphics.ResScaleCustom.Value = ConfigurationState.Instance.Graphics.ResScaleCustom.Value;
            Graphics.MaxAnisotropy.Value = ConfigurationState.Instance.Graphics.MaxAnisotropy.Value;
            Graphics.AspectRatio.Value = ConfigurationState.Instance.Graphics.AspectRatio.Value;
            Graphics.ShadersDumpPath.Value = ConfigurationState.Instance.Graphics.ShadersDumpPath.Value;
            Logger.EnableDebug.Value = ConfigurationState.Instance.Logger.EnableDebug.Value;
            Logger.EnableStub.Value = ConfigurationState.Instance.Logger.EnableStub.Value;
            Logger.EnableInfo.Value = ConfigurationState.Instance.Logger.EnableInfo.Value;
            Logger.EnableWarn.Value = ConfigurationState.Instance.Logger.EnableWarn.Value;
            Logger.EnableError.Value = ConfigurationState.Instance.Logger.EnableError.Value;
            Logger.EnableGuest.Value = ConfigurationState.Instance.Logger.EnableGuest.Value;
            Logger.EnableFsAccessLog.Value = ConfigurationState.Instance.Logger.EnableFsAccessLog.Value;
            Logger.FilteredClasses.Value = ConfigurationState.Instance.Logger.FilteredClasses.Value;
            Logger.GraphicsDebugLevel.Value = ConfigurationState.Instance.Logger.GraphicsDebugLevel.Value;
            Logger.EnableFileLog.Value = ConfigurationState.Instance.Logger.EnableFileLog.Value;
            System.Language.Value = ConfigurationState.Instance.System.Language.Value;
            System.Region.Value = ConfigurationState.Instance.System.Region.Value;
            System.TimeZone.Value = ConfigurationState.Instance.System.TimeZone.Value;
            System.SystemTimeOffset.Value = ConfigurationState.Instance.System.SystemTimeOffset.Value;
            System.EnableDockedMode.Value = ConfigurationState.Instance.System.EnableDockedMode.Value;
            Graphics.EnableVsync.Value = ConfigurationState.Instance.Graphics.EnableVsync.Value;
            Graphics.EnableShaderCache.Value = ConfigurationState.Instance.Graphics.EnableShaderCache.Value;
            System.EnablePtc.Value = ConfigurationState.Instance.System.EnablePtc.Value;
            System.EnableFsIntegrityChecks.Value = ConfigurationState.Instance.System.EnableFsIntegrityChecks.Value;
            System.FsGlobalAccessLogMode.Value = ConfigurationState.Instance.System.FsGlobalAccessLogMode.Value;
            System.AudioBackend.Value = ConfigurationState.Instance.System.AudioBackend.Value;
            System.IgnoreMissingServices.Value = ConfigurationState.Instance.System.IgnoreMissingServices.Value;
            Hid.EnableKeyboard.Value = ConfigurationState.Instance.Hid.EnableKeyboard;
            Hid.Hotkeys.Value = ConfigurationState.Instance.Hid.Hotkeys.Value;
            Hid.InputConfig.Value = ConfigurationState.Instance.Hid.InputConfig.Value;
        }

        public void Load(ConfigurationFileFormat configurationFileFormat, string configurationFilePath)
        {
            bool configurationFileUpdated = false;

            if (configurationFileFormat.Version < 0 || configurationFileFormat.Version > ConfigurationFileFormat.CurrentVersion)
            {
                Logging.Logger.Warning?.Print(LogClass.Application, $"Unsupported configuration version {configurationFileFormat.Version}, loading default.");

                LoadDefault();

                return;
            }

            if (configurationFileFormat.Version < 2)
            {
                Logging.Logger.Warning?.Print(LogClass.Application, $"Outdated configuration version {configurationFileFormat.Version}, migrating to version 2.");

                configurationFileFormat.SystemRegion = Region.USA;

                configurationFileUpdated = true;
            }

            if (configurationFileFormat.Version < 3)
            {
                Logging.Logger.Warning?.Print(LogClass.Application, $"Outdated configuration version {configurationFileFormat.Version}, migrating to version 3.");

                configurationFileFormat.SystemTimeZone = "UTC";

                configurationFileUpdated = true;
            }

            if (configurationFileFormat.Version < 4)
            {
                Logging.Logger.Warning?.Print(LogClass.Application, $"Outdated configuration version {configurationFileFormat.Version}, migrating to version 4.");

                configurationFileFormat.MaxAnisotropy = -1;

                configurationFileUpdated = true;
            }

            if (configurationFileFormat.Version < 5)
            {
                Logging.Logger.Warning?.Print(LogClass.Application, $"Outdated configuration version {configurationFileFormat.Version}, migrating to version 5.");

                configurationFileFormat.SystemTimeOffset = 0;

                configurationFileUpdated = true;
            }

            if (configurationFileFormat.Version < 6)
            {
                Logging.Logger.Warning?.Print(LogClass.Application, $"Outdated configuration version {configurationFileFormat.Version}, migrating to version 6.");

                configurationFileFormat.ControllerConfig = new List<ControllerConfig>();
                configurationFileFormat.KeyboardConfig = new List<KeyboardConfig>
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
                Logging.Logger.Warning?.Print(LogClass.Application, $"Outdated configuration version {configurationFileFormat.Version}, migrating to version 7.");

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
                Logging.Logger.Warning?.Print(LogClass.Application, $"Outdated configuration version {configurationFileFormat.Version}, migrating to version 8.");

                configurationFileFormat.EnablePtc = true;

                configurationFileUpdated = true;
            }

            if (configurationFileFormat.Version < 9)
            {
                Logging.Logger.Warning?.Print(LogClass.Application, $"Outdated configuration version {configurationFileFormat.Version}, migrating to version 9.");

                configurationFileFormat.Hotkeys = new KeyboardHotkeys
                {
                    ToggleVsync = Key.Tab
                };

                configurationFileUpdated = true;
            }

            if (configurationFileFormat.Version < 10)
            {
                Logging.Logger.Warning?.Print(LogClass.Application, $"Outdated configuration version {configurationFileFormat.Version}, migrating to version 10.");

                configurationFileFormat.AudioBackend = AudioBackend.OpenAl;

                configurationFileUpdated = true;
            }

            if (configurationFileFormat.Version < 11)
            {
                Logging.Logger.Warning?.Print(LogClass.Application, $"Outdated configuration version {configurationFileFormat.Version}, migrating to version 11.");

                configurationFileFormat.ResScale = 1;
                configurationFileFormat.ResScaleCustom = 1.0f;

                configurationFileUpdated = true;
            }

            if (configurationFileFormat.Version < 12)
            {
                Logging.Logger.Warning?.Print(LogClass.Application, $"Outdated configuration version {configurationFileFormat.Version}, migrating to version 12.");

                configurationFileFormat.LoggingGraphicsDebugLevel = GraphicsDebugLevel.None;

                configurationFileUpdated = true;
            }

            if (configurationFileFormat.Version < 14)
            {
                Logging.Logger.Warning?.Print(LogClass.Application, $"Outdated configuration version {configurationFileFormat.Version}, migrating to version 14.");

                configurationFileFormat.CheckUpdatesOnStart = true;

                configurationFileUpdated = true;
            }

            if (configurationFileFormat.Version < 16)
            {
                Logging.Logger.Warning?.Print(LogClass.Application, $"Outdated configuration version {configurationFileFormat.Version}, migrating to version 16.");

                configurationFileFormat.EnableShaderCache = true;

                configurationFileUpdated = true;
            }

            if (configurationFileFormat.Version < 17)
            {
                Logging.Logger.Warning?.Print(LogClass.Application, $"Outdated configuration version {configurationFileFormat.Version}, migrating to version 17.");

                configurationFileFormat.StartFullscreen = false;

                configurationFileUpdated = true;
            }

            if (configurationFileFormat.Version < 18)
            {
                Logging.Logger.Warning?.Print(LogClass.Application, $"Outdated configuration version {configurationFileFormat.Version}, migrating to version 18.");

                configurationFileFormat.AspectRatio = AspectRatio.Fixed16x9;

                configurationFileUpdated = true;
            }

            if (configurationFileFormat.Version < 20)
            {
                Logging.Logger.Warning?.Print(LogClass.Application, $"Outdated configuration version {configurationFileFormat.Version}, migrating to version 20.");

                configurationFileFormat.ShowConfirmExit = true;

                configurationFileUpdated = true;
            }

            if (configurationFileFormat.Version < 22)
            {
                Logging.Logger.Warning?.Print(LogClass.Application, $"Outdated configuration version {configurationFileFormat.Version}, migrating to version 22.");

                configurationFileFormat.HideCursorOnIdle = false;

                configurationFileUpdated = true;
            }

            List<InputConfig> inputConfig = new List<InputConfig>();
            inputConfig.AddRange(configurationFileFormat.ControllerConfig);
            inputConfig.AddRange(configurationFileFormat.KeyboardConfig);

            Graphics.ResScale.Value = configurationFileFormat.ResScale;
            Graphics.ResScaleCustom.Value = configurationFileFormat.ResScaleCustom;
            Graphics.MaxAnisotropy.Value = configurationFileFormat.MaxAnisotropy;
            Graphics.AspectRatio.Value = configurationFileFormat.AspectRatio;
            Graphics.ShadersDumpPath.Value = configurationFileFormat.GraphicsShadersDumpPath;
            Logger.EnableDebug.Value = configurationFileFormat.LoggingEnableDebug;
            Logger.EnableStub.Value = configurationFileFormat.LoggingEnableStub;
            Logger.EnableInfo.Value = configurationFileFormat.LoggingEnableInfo;
            Logger.EnableWarn.Value = configurationFileFormat.LoggingEnableWarn;
            Logger.EnableError.Value = configurationFileFormat.LoggingEnableError;
            Logger.EnableGuest.Value = configurationFileFormat.LoggingEnableGuest;
            Logger.EnableFsAccessLog.Value = configurationFileFormat.LoggingEnableFsAccessLog;
            Logger.FilteredClasses.Value = configurationFileFormat.LoggingFilteredClasses;
            Logger.GraphicsDebugLevel.Value = configurationFileFormat.LoggingGraphicsDebugLevel;
            Logger.EnableFileLog.Value = configurationFileFormat.EnableFileLog;
            System.Language.Value = configurationFileFormat.SystemLanguage;
            System.Region.Value = configurationFileFormat.SystemRegion;
            System.TimeZone.Value = configurationFileFormat.SystemTimeZone;
            System.SystemTimeOffset.Value = configurationFileFormat.SystemTimeOffset;
            System.EnableDockedMode.Value = configurationFileFormat.DockedMode;
            Graphics.EnableVsync.Value = configurationFileFormat.EnableVsync;
            Graphics.EnableShaderCache.Value = configurationFileFormat.EnableShaderCache;
            System.EnablePtc.Value = configurationFileFormat.EnablePtc;
            System.EnableFsIntegrityChecks.Value = configurationFileFormat.EnableFsIntegrityChecks;
            System.FsGlobalAccessLogMode.Value = configurationFileFormat.FsGlobalAccessLogMode;
            System.AudioBackend.Value = configurationFileFormat.AudioBackend;
            System.IgnoreMissingServices.Value = configurationFileFormat.IgnoreMissingServices;
            Hid.EnableKeyboard.Value = configurationFileFormat.EnableKeyboard;
            Hid.Hotkeys.Value = configurationFileFormat.Hotkeys;
            Hid.InputConfig.Value = inputConfig;

            if (configurationFileUpdated)
            {
                ToFileFormat().SaveConfig(configurationFilePath);

                Logging.Logger.Notice.Print(LogClass.Application, $"Configuration file updated to version {ConfigurationFileFormat.CurrentVersion}");
            }
        }

        public static void Initialize()
        {
            if (Instance != null)
            {
                throw new InvalidOperationException("Configuration is already initialized");
            }

            Instance = new GameConfigurationState();
        }
    }
}
