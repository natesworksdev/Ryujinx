using LibHac.IO;
using OpenTK.Input;
using Ryujinx.Common;
using Ryujinx.Common.Logging;
using Ryujinx.HLE;
using Ryujinx.HLE.HOS.SystemState;
using Ryujinx.HLE.Input;
using Ryujinx.UI.Input;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Utf8Json;
using Utf8Json.Formatters;
using Utf8Json.Internal;
using Utf8Json.Resolvers;

namespace Ryujinx
{
    public class Configuration
    {
        /// <summary>
        /// The default configuration instance
        /// </summary>
        public static Configuration Default { get; private set; }

        /// <summary>
        /// Dumps shaders in this local directory
        /// </summary>
        public string GraphicsShadersDumpPath { get; private set; }

        /// <summary>
        /// Enables printing debug log messages
        /// </summary>
        public bool LoggingEnableDebug { get; private set; }

        /// <summary>
        /// Enables printing stub log messages
        /// </summary>
        public bool LoggingEnableStub { get; private set; }

        /// <summary>
        /// Enables printing info log messages
        /// </summary>
        public bool LoggingEnableInfo { get; private set; }

        /// <summary>
        /// Enables printing warning log messages
        /// </summary>
        public bool LoggingEnableWarn { get; private set; }

        /// <summary>
        /// Enables printing error log messages
        /// </summary>
        public bool LoggingEnableError { get; private set; }

        /// <summary>
        /// Controls which log messages are written to the log targets
        /// </summary>
        public LogClass[] LoggingFilteredClasses { get; private set; }

        /// <summary>
        /// Enables or disables logging to a file on disk
        /// </summary>
        public bool EnableFileLog { get; private set; }

        /// <summary>
        /// Change System Language
        /// </summary>
        public SystemLanguage SystemLanguage { get; private set; }

        /// <summary>
        /// Enables or disables Docked Mode
        /// </summary>
        public bool DockedMode { get; private set; }

        /// <summary>
        /// Enables or disables Vertical Sync
        /// </summary>
        public bool EnableVsync { get; private set; }

        /// <summary>
        /// Enables or disables multi-core scheduling of threads
        /// </summary>
        public bool EnableMultiCoreScheduling { get; private set; }

        /// <summary>
        /// Enables integrity checks on Game content files
        /// </summary>
        public bool EnableFsIntegrityChecks { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public HidControllerType ControllerType { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public NpadKeyboard KeyboardControls { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public NpadController GamepadControls { get; private set; }

        /// <summary>
        /// Loads a configuration file from disk
        /// </summary>
        /// <param name="path">The path to the JSON configuration file</param>
        public static void Load(string path)
        {
            var resolver = CompositeResolver.Create(
                new[] { new ConfigurationEnumFormatter<Key>() },
                new[] { StandardResolver.AllowPrivateSnakeCase }
            );

            using (Stream stream = File.OpenRead(path))
            {
                Default = JsonSerializer.Deserialize<Configuration>(stream, resolver);
            }
        }

        /// <summary>
        /// Loads a configuration file asynchronously from disk
        /// </summary>
        /// <param name="path">The path to the JSON configuration file</param>
        public static async Task LoadAsync(string path)
        {
            var resolver = CompositeResolver.Create(StandardResolver.AllowPrivateSnakeCase);

            using (Stream stream = File.OpenRead(path))
            {
                Default = await JsonSerializer.DeserializeAsync<Configuration>(stream, resolver);
            }
        }

        /// <summary>
        /// Configures a <see cref="Switch"/> instance
        /// </summary>
        /// <param name="device">The instance to configure</param>
        public static void Configure(Switch device)
        {
            if (Default == null)
            {
                throw new InvalidOperationException("Configuration has not been loaded yet.");
            }

            GraphicsConfig.ShadersDumpPath = Default.GraphicsShadersDumpPath;

            Logger.AddTarget(new AsyncLogTargetWrapper(
                new ConsoleLogTarget(),
                1000,
                AsyncLogTargetOverflowAction.Block
            ));

            if (Default.EnableFileLog)
            {
                Logger.AddTarget(new AsyncLogTargetWrapper(
                    new FileLogTarget("Ryujinx.log"),
                    1000,
                    AsyncLogTargetOverflowAction.Block
                ));
            }

            Logger.SetEnable(LogLevel.Debug,   Default.LoggingEnableDebug);
            Logger.SetEnable(LogLevel.Stub,    Default.LoggingEnableStub);
            Logger.SetEnable(LogLevel.Info,    Default.LoggingEnableInfo);
            Logger.SetEnable(LogLevel.Warning, Default.LoggingEnableWarn);
            Logger.SetEnable(LogLevel.Error,   Default.LoggingEnableError);

            if (Default.LoggingFilteredClasses.Length > 0)
            {
                foreach (var logClass in EnumExtensions.GetValues<LogClass>())
                {
                    Logger.SetEnable(logClass, false);
                }

                foreach (var logClass in Default.LoggingFilteredClasses)
                {
                    Logger.SetEnable(logClass, true);
                }
            }

            device.EnableDeviceVsync = Default.EnableVsync;

            device.System.State.DockedMode = Default.DockedMode;

            device.System.State.SetLanguage(Default.SystemLanguage);

            if (Default.EnableMultiCoreScheduling)
            {
                device.System.EnableMultiCoreScheduling();
            }

            device.System.FsIntegrityCheckLevel = Default.EnableFsIntegrityChecks
                ? IntegrityCheckLevel.ErrorOnInvalid
                : IntegrityCheckLevel.None;

            if(Default.GamepadControls.Enabled)
            {
                if (GamePad.GetName(Default.GamepadControls.Index) == "Unmapped Controller")
                {
                    Default.GamepadControls.SetEnabled(false);
                }
            }

            device.Hid.InitilizePrimaryController(Default.ControllerType);
        }

        private class ConfigurationEnumFormatter<T> : IJsonFormatter<T>
            where T : struct
        {
            public void Serialize(ref JsonWriter writer, T value, IJsonFormatterResolver formatterResolver)
            {
                formatterResolver.GetFormatterWithVerify<string>()
                                 .Serialize(ref writer, value.ToString(), formatterResolver);
            }

            public T Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
            {
                if (reader.ReadIsNull())
                {
                    return default(T);
                }

                var enumName = formatterResolver.GetFormatterWithVerify<string>()
                                                .Deserialize(ref reader, formatterResolver);

                if(Enum.TryParse<T>(enumName, out T result))
                {
                    return result;
                }

                return default(T);
            }
        }
    }
}