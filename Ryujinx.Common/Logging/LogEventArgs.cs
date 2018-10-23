using System;

namespace Ryujinx.Common.Logging
{
    public class LogEventArgs : EventArgs
    {
        public LogLevel Level { get; private set; }
        public TimeSpan Time  { get; private set; }

        public string Message { get; private set; }

        public LogEventArgs(LogLevel level, TimeSpan time, string message)
        {
            Level   = level;
            Time    = time;
            Message = message;
        }
    }
}