namespace Ryujinx.Common.Logging
{
    public interface ILogFormatter
    {
        string Format(LogEventArgs args);
    }
}
