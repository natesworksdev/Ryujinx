using Ryujinx.Common;
using Ryujinx.Common.Logging;
using System;
using System.IO;

namespace Ryujinx.Configuration
{
    public static class LoggerModule
    {
        public static void Initialize()
        {
            ConfigurationState.Instance.Logger.EnableDebug.Event       += ReloadEnableDebug;
            ConfigurationState.Instance.Logger.EnableStub.Event        += ReloadEnableStub;
            ConfigurationState.Instance.Logger.EnableInfo.Event        += ReloadEnableInfo;
            ConfigurationState.Instance.Logger.EnableWarn.Event        += ReloadEnableWarning;
            ConfigurationState.Instance.Logger.EnableError.Event       += ReloadEnableError;
            ConfigurationState.Instance.Logger.EnableGuest.Event       += ReloadEnableGuest;
            ConfigurationState.Instance.Logger.EnableFsAccessLog.Event += ReloadEnableFsAccessLog;
            ConfigurationState.Instance.Logger.FilteredClasses.Event   += ReloadFilteredClasses;
            ConfigurationState.Instance.Logger.EnableFileLog.Event     += ReloadFileLogger;
            ConfigurationState.Instance.Logger.CustomLogDirectory.Event      += ReloadFileLoggerNewDirectory;
        }

        private static void ReloadEnableDebug(object sender, ReactiveEventArgs<bool> e)
        {
            Logger.SetEnable(LogLevel.Debug, e.NewValue);
        }

        private static void ReloadEnableStub(object sender, ReactiveEventArgs<bool> e)
        {
            Logger.SetEnable(LogLevel.Stub, e.NewValue);
        }

        private static void ReloadEnableInfo(object sender, ReactiveEventArgs<bool> e)
        {
            Logger.SetEnable(LogLevel.Info, e.NewValue);
        }

        private static void ReloadEnableWarning(object sender, ReactiveEventArgs<bool> e)
        {
            Logger.SetEnable(LogLevel.Warning, e.NewValue);
        }

        private static void ReloadEnableError(object sender, ReactiveEventArgs<bool> e)
        {
            Logger.SetEnable(LogLevel.Error, e.NewValue);
        }

        private static void ReloadEnableGuest(object sender, ReactiveEventArgs<bool> e)
        {
            Logger.SetEnable(LogLevel.Guest, e.NewValue);
        }

        private static void ReloadEnableFsAccessLog(object sender, ReactiveEventArgs<bool> e)
        {
            Logger.SetEnable(LogLevel.AccessLog, e.NewValue);
        }

        private static void ReloadFilteredClasses(object sender, ReactiveEventArgs<LogClass[]> e)
        {
            bool noFilter = e.NewValue.Length == 0;

            foreach (var logClass in EnumExtensions.GetValues<LogClass>())
            {
                Logger.SetEnable(logClass, noFilter);
            }

            foreach (var logClass in e.NewValue)
            {
                Logger.SetEnable(logClass, true);
            }
        }

        public static void ReloadFileLogger(object sender, ReactiveEventArgs<bool> e)
        {
            if (e.NewValue)
            {
                if (Logger.HasTarget("file"))
                {
                    Logger.RemoveTarget("file");
                }
                if (String.IsNullOrEmpty(ConfigurationState.Instance.Logger.CustomLogDirectory.Value)) {
                    Logger.AddTarget(new AsyncLogTargetWrapper(
                    new FileLogTarget(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs"), "file"),
                    1000,
                    AsyncLogTargetOverflowAction.Block));
 
                }
                else
                {
                    Logger.AddTarget(new AsyncLogTargetWrapper(
                    new FileLogTarget(ConfigurationState.Instance.Logger.CustomLogDirectory.Value, "file"),
                    1000,
                    AsyncLogTargetOverflowAction.Block));
                }
            }
            else
            {
                Logger.RemoveTarget("file");
            }
        }

        public static void ReloadFileLoggerNewDirectory(object sender, ReactiveEventArgs<string> e)
        {
            if (e.NewValue!=e.OldValue) {
                if (Logger.HasTarget("file"))
                {
                    Logger.RemoveTarget("file");
                }
                if (String.IsNullOrEmpty(ConfigurationState.Instance.Logger.CustomLogDirectory.Value))
                {
                    Logger.AddTarget(new AsyncLogTargetWrapper(
                    new FileLogTarget(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs"), "file"),
                    1000,
                    AsyncLogTargetOverflowAction.Block));

                }
                else
                {
                    Logger.AddTarget(new AsyncLogTargetWrapper(
                    new FileLogTarget(ConfigurationState.Instance.Logger.CustomLogDirectory.Value, "file"),
                    1000,
                    AsyncLogTargetOverflowAction.Block));
                
                }
            }
        }
    }
}
