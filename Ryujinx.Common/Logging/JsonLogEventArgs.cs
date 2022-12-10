using System;
using System.Text.Json.Serialization;

namespace Ryujinx.Common.Logging;

internal class JsonLogEventArgs : EventArgs
{
    public LogLevel Level { get; }
    public TimeSpan Time { get; }
    public string   ThreadName { get; }

    public string Message { get; }
    public string Data { get; }

    [JsonConstructor]
    public JsonLogEventArgs(LogLevel level, TimeSpan time, string threadName, string message, string data = null)
    {
        Level      = level;
        Time       = time;
        ThreadName = threadName;
        Message    = message;
        Data       = data;
    }

    public static JsonLogEventArgs FromLogEventArgs(LogEventArgs args, DynamicObjectFormatter objectFormatter)
    {
        return new JsonLogEventArgs(args.Level, args.Time, args.ThreadName, args.Message, objectFormatter.Format(args.Data));
    }
}