using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Ryujinx.Common.Logging
{
    public static class Logger
    {
        private static bool[] _enabledLevels;
        private static bool[] _enabledClasses;

        public static event EventHandler<LogEventArgs> Updated;

        private static Stopwatch _time;

        static Logger()
        {
            _enabledLevels  = new bool[Enum.GetNames(typeof(LogLevel)).Length];
            _enabledClasses = new bool[Enum.GetNames(typeof(LogClass)).Length];

            _enabledLevels[(int)LogLevel.Stub]    = true;
            _enabledLevels[(int)LogLevel.Info]    = true;
            _enabledLevels[(int)LogLevel.Warning] = true;
            _enabledLevels[(int)LogLevel.Error]   = true;

            for (int index = 0; index < _enabledClasses.Length; index++) _enabledClasses[index] = true;

            _time = new Stopwatch();

            _time.Start();
        }

        public static void SetEnable(LogLevel level, bool enabled)
        {
            _enabledLevels[(int)level] = enabled;
        }

        public static void SetEnable(LogClass Class, bool enabled)
        {
            _enabledClasses[(int)Class] = enabled;
        }

        public static void PrintDebug(LogClass Class, string message, [CallerMemberName] string caller = "")
        {
            Print(LogLevel.Debug, Class, GetFormattedMessage(Class, message, caller));
        }

        public static void PrintStub(LogClass Class, string message, [CallerMemberName] string caller = "")
        {
            Print(LogLevel.Stub, Class, GetFormattedMessage(Class, message, caller));
        }

        public static void PrintInfo(LogClass Class, string message, [CallerMemberName] string caller = "")
        {
            Print(LogLevel.Info, Class, GetFormattedMessage(Class, message, caller));
        }

        public static void PrintWarning(LogClass Class, string message, [CallerMemberName] string caller = "")
        {
            Print(LogLevel.Warning, Class, GetFormattedMessage(Class, message, caller));
        }

        public static void PrintError(LogClass Class, string message, [CallerMemberName] string caller = "")
        {
            Print(LogLevel.Error, Class, GetFormattedMessage(Class, message, caller));
        }

        private static void Print(LogLevel level, LogClass Class, string message)
        {
            if (_enabledLevels[(int)level] && _enabledClasses[(int)Class]) Updated?.Invoke(null, new LogEventArgs(level, _time.Elapsed, message));
        }

        private static string GetFormattedMessage(LogClass Class, string message, string caller)
        {
            return $"{Class} {caller}: {message}";
        }
    }
}