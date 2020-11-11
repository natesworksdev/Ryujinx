using System;

namespace Ryujinx.Common.Logging
{
    /// <summary>
    /// A destination that the <see cref="Logger"/> logs to
    /// </summary>
    public interface ILogTarget : IDisposable
    {
        /// <summary>
        /// The method to be invoked when a message or data is logged by the <see cref="Logger"/>
        /// </summary>
        /// <remark>
        /// Will only be called if <see cref="Logger"/> has added this instance as a logging target
        /// </remark>
        void Log(object sender, LogEventArgs args);

        /// <summary>
        /// The target's name; used to identify the type of target
        /// </summary>
        string Name { get; }
    }
}
