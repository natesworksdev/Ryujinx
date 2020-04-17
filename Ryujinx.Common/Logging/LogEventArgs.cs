using System;

namespace Ryujinx.Common.Logging
{
    public class LogEventArgs : EventArgs
    {
        public LogLevel Level      { get; }
        public TimeSpan Time       { get; }
        public string   ThreadName { get; }

        public string Message { get; }
        public object Data    { get; }

        public LogEventArgs(LogLevel level, TimeSpan time, string threadName, string message, object data = null)
        {
            Level      = level;
            Time       = time;
            ThreadName = threadName;
            Message    = message;
            Data       = data;
        }
    }
}