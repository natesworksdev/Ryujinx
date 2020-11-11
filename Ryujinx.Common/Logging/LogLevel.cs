namespace Ryujinx.Common.Logging
{
    /// <summary>
    /// The levels for which data can be logged at. Provides information on the type/nature of a log entry.
    /// </summary>
    public enum LogLevel
    {
        Debug,
        Stub,
        Info,
        Warning,
        Error,
        Guest,
        AccessLog,
        Trace,
        Notice
    }
}
