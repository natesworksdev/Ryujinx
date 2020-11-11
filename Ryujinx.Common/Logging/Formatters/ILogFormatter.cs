namespace Ryujinx.Common.Logging
{
    /// <summary>
    /// Defines an interface for parsing to-be-logged data into readable text 
    /// </summary>
    interface ILogFormatter
    {
        string Format(LogEventArgs args);
    }
}
