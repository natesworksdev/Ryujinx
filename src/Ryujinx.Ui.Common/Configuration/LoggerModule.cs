using Ryujinx.Common;
using Ryujinx.Common.Configuration;
using Ryujinx.Common.Logging;
using Ryujinx.Common.Logging.Targets;
using Ryujinx.Common.Utilities;
using System;
using System.IO;

namespace Ryujinx.Ui.Common.Configuration
{
    public static class LoggerModule
    {
        public static void Initialize()
        {
            if (ReleaseInformation.IsValid)
            {
                string oldLogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
                string newLogPath = Path.Combine(AppDataManager.BaseDirPath, "Logs");

                if (Directory.Exists(oldLogPath) && !Directory.Exists(newLogPath))
                {
                    FileSystemUtils.MoveDirectory(oldLogPath, newLogPath);
                }
            }

            ConfigurationState.Instance.Logger.EnableDebug.Event += ReloadEnableDebug;
            ConfigurationState.Instance.Logger.EnableStub.Event += ReloadEnableStub;
            ConfigurationState.Instance.Logger.EnableInfo.Event += ReloadEnableInfo;
            ConfigurationState.Instance.Logger.EnableWarn.Event += ReloadEnableWarning;
            ConfigurationState.Instance.Logger.EnableError.Event += ReloadEnableError;
            ConfigurationState.Instance.Logger.EnableTrace.Event += ReloadEnableTrace;
            ConfigurationState.Instance.Logger.EnableGuest.Event += ReloadEnableGuest;
            ConfigurationState.Instance.Logger.EnableFsAccessLog.Event += ReloadEnableFsAccessLog;
            ConfigurationState.Instance.Logger.FilteredClasses.Event += ReloadFilteredClasses;
            ConfigurationState.Instance.Logger.EnableFileLog.Event += ReloadFileLogger;
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

        private static void ReloadEnableTrace(object sender, ReactiveEventArgs<bool> e)
        {
            Logger.SetEnable(LogLevel.Trace, e.NewValue);
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

            foreach (var logClass in Enum.GetValues<LogClass>())
            {
                Logger.SetEnable(logClass, noFilter);
            }

            foreach (var logClass in e.NewValue)
            {
                Logger.SetEnable(logClass, true);
            }
        }

        private static void ReloadFileLogger(object sender, ReactiveEventArgs<bool> e)
        {
            if (e.NewValue)
            {
                string logBasePath = ReleaseInformation.IsValid ? AppDataManager.BaseDirPath : AppDomain.CurrentDomain.BaseDirectory;

                Logger.AddTarget(new AsyncLogTargetWrapper(
                    new FileLogTarget(logBasePath, "file"),
                    1000,
                    AsyncLogTargetOverflowAction.Block
                ));
            }
            else
            {
                Logger.RemoveTarget("file");
            }
        }
    }
}
