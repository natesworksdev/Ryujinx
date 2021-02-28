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
        public ReactiveObject<bool> EnableDebug { get; protected set; }

        /// <summary>
        /// Enables printing stub log messages
        /// </summary>
        public ReactiveObject<bool> EnableStub { get; protected set; }

        /// <summary>
        /// Enables printing info log messages
        /// </summary>
        public ReactiveObject<bool> EnableInfo { get; protected set; }

        /// <summary>
        /// Enables printing warning log messages
        /// </summary>
        public ReactiveObject<bool> EnableWarn { get; protected set; }

        /// <summary>
        /// Enables printing error log messages
        /// </summary>
        public ReactiveObject<bool> EnableError { get; protected set; }

        /// <summary>
        /// Enables printing guest log messages
        /// </summary>
        public ReactiveObject<bool> EnableGuest { get; protected set; }

        /// <summary>
        /// Enables printing FS access log messages
        /// </summary>
        public ReactiveObject<bool> EnableFsAccessLog { get; protected set; }

        /// <summary>
        /// Controls which log messages are written to the log targets
        /// </summary>
        public ReactiveObject<LogClass[]> FilteredClasses { get; protected set; }

        /// <summary>
        /// Enables or disables logging to a file on disk
        /// </summary>
        public ReactiveObject<bool> EnableFileLog { get; protected set; }

        /// <summary>
        /// Controls which OpenGL log messages are recorded in the log
        /// </summary>
        public ReactiveObject<GraphicsDebugLevel> GraphicsDebugLevel { get; protected set; }

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
