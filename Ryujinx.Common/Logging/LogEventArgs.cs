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
            this.Level   = level;
            this.Time    = time;
            this.Message = message;
        }
    }
}