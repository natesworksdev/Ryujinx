using System;

namespace Ryujinx.Common.Logging
{
    public class LogEventArgs : EventArgs
    {
        public readonly LogLevel Level;
        public readonly TimeSpan Time;
        public readonly string ThreadName;
        public readonly LogClass Class;
        public readonly string Caller;
        public readonly string Message;
        public readonly object Data;

        public LogEventArgs(LogLevel level, TimeSpan time, string threadName, LogClass logClass, string caller, string message, object data)
        {
            Level      = level;
            Time       = time;
            ThreadName = threadName;
            Class = logClass;
            Caller = caller;
            Message    = message;
            Data       = data;
        }
    }
}