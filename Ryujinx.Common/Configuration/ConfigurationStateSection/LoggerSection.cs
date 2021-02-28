using Ryujinx.Common.Logging;

namespace Ryujinx.Common.Configuration.ConfigurationStateSection
{
    /// <summary>
    /// Logger configuration section
    /// </summary>
    public class LoggerSection
    {
        /// <summary>
        /// Enables printing debug log messages
        /// </summary>
        public ReactiveObject<bool> EnableDebug { get; }

        /// <summary>
        /// Enables printing stub log messages
        /// </summary>
        public ReactiveObject<bool> EnableStub { get; }

        /// <summary>
        /// Enables printing info log messages
        /// </summary>
        public ReactiveObject<bool> EnableInfo { get; }

        /// <summary>
        /// Enables printing warning log messages
        /// </summary>
        public ReactiveObject<bool> EnableWarn { get; }

        /// <summary>
        /// Enables printing error log messages
        /// </summary>
        public ReactiveObject<bool> EnableError { get; }

        /// <summary>
        /// Enables printing guest log messages
        /// </summary>
        public ReactiveObject<bool> EnableGuest { get; }

        /// <summary>
        /// Enables printing FS access log messages
        /// </summary>
        public ReactiveObject<bool> EnableFsAccessLog { get; }

        /// <summary>
        /// Controls which log messages are written to the log targets
        /// </summary>
        public ReactiveObject<LogClass[]> FilteredClasses { get; }

        /// <summary>
        /// Enables or disables logging to a file on disk
        /// </summary>
        public ReactiveObject<bool> EnableFileLog { get; }

        /// <summary>
        /// Controls which OpenGL log messages are recorded in the log
        /// </summary>
        public ReactiveObject<GraphicsDebugLevel> GraphicsDebugLevel { get; }

        public LoggerSection()
        {
            EnableDebug = new ReactiveObject<bool>();
            EnableStub = new ReactiveObject<bool>();
            EnableInfo = new ReactiveObject<bool>();
            EnableWarn = new ReactiveObject<bool>();
            EnableError = new ReactiveObject<bool>();
            EnableGuest = new ReactiveObject<bool>();
            EnableFsAccessLog = new ReactiveObject<bool>();
            FilteredClasses = new ReactiveObject<LogClass[]>();
            EnableFileLog = new ReactiveObject<bool>();
            GraphicsDebugLevel = new ReactiveObject<GraphicsDebugLevel>();
        }
    }
}
