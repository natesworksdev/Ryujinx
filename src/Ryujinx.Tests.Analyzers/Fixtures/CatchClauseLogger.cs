using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
// ReSharper disable All

namespace Ryujinx.Common.Logging
{
    public enum LogClass
    {
        Application,
    }

    public class LogEventArgs : EventArgs
    {
        public readonly int Level;
        public readonly TimeSpan Time;
        public readonly string ThreadName;

        public readonly string Message;
        public readonly object Data;

        public LogEventArgs(int level, TimeSpan time, string threadName, string message, object data = null)
        {
            Level = level;
            Time = time;
            ThreadName = threadName;
            Message = message;
            Data = data;
        }
    }

    public static class Logger
    {
        private static readonly Stopwatch _time;
        private static readonly bool[] _enabledClasses;
        public static event EventHandler<LogEventArgs> Updated;
        public readonly struct Log
        {
            internal readonly int Level;

            internal Log(int level)
            {
                Level = level;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Print(LogClass logClass, string message, [CallerMemberName] string caller = "")
            {
                if (_enabledClasses[(int)logClass])
                {
                    Updated?.Invoke(null, new LogEventArgs(Level, _time.Elapsed, Thread.CurrentThread.Name, FormatMessage(logClass, caller, message)));
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Print(LogClass logClass, string message, object data, [CallerMemberName] string caller = "")
            {
                if (_enabledClasses[(int)logClass])
                {
                    Updated?.Invoke(null, new LogEventArgs(Level, _time.Elapsed, Thread.CurrentThread.Name, FormatMessage(logClass, caller, message), data));
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static string FormatMessage(LogClass logClass, string caller, string message) => $"{logClass} {caller}: {message}";
        }

        public static Log? Debug { get; private set; }
        public static Log? Info { get; private set; }
        public static Log? Warning { get; private set; }
        public static Log? Error { get; private set; }
        public static Log? Guest { get; private set; }
        public static Log? AccessLog { get; private set; }
        public static Log? Stub { get; private set; }
        public static Log? Trace { get; private set; }
        public static Log Notice { get; } // Always enabled
    }
}
