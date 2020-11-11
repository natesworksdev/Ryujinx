using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Ryujinx.Common.Logging
{
    public static class Logger
    {
        /// <summary>
        /// Tracks the amount of time elapsed since the start of Ryujinx.
        /// </summary>
        /// <remarks>
        /// Time spent idling in the games directory does not increase this value.
        /// </remarks>
        private static readonly Stopwatch m_Time;

        /// <summary>
        /// Array of booleans that describe whether logging for a specific <see cref="LogClass"/> has been enabled.
        /// </summary>
        private static readonly bool[] m_EnabledClasses;

        /// <summary>
        /// A list of targets to log to.
        /// </summary>
        private static readonly List<ILogTarget> m_LogTargets;

        /// <summary>
        /// The event that is raised whenever a message or data needs to be logged
        /// </summary>
        public static event EventHandler<LogEventArgs> Updated;

        public struct Log
        {
            /// <summary>
            /// The level at which to log at
            /// </summary>
            internal readonly LogLevel Level;

            internal Log(LogLevel level)
            {
                Level = level;
            }

            /// <summary>
            /// Logs a message at the specified <see cref="LogLevel"/> and using the specified <see cref="LogClass"/> and caller.
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Print(LogClass logClass, string message = "", [CallerMemberName] string caller = "")
            {
                Print(logClass, message, null, caller);
            }

            /// <summary>
            /// Logs an object at the specified <see cref="LogLevel"/> and using the specified <see cref="LogClass"/> and caller.
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Print(LogClass logClass, object data, [CallerMemberName] string caller = "")
            {
                Print(logClass, "", data, caller);
            }  

            /// <summary>
            /// Logs a message and object at the specified <see cref="LogLevel"/> and using the specified <see cref="LogClass"/> and caller.
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Print(LogClass logClass, string message, object data, [CallerMemberName] string caller = "")
            {
                if (m_EnabledClasses[(int)logClass])
                {
                    Updated?.Invoke(null, new LogEventArgs(Level, m_Time.Elapsed, Thread.CurrentThread.Name, logClass, caller, message, data));
                }
            }
        }

        /*
         By using Nullables, the cost of invoking the logging function if a log level is disabled is greatly reduced.
         For example, the arguments passed to the Log are not computed if it isn't enabled.
        */
        public static Log? Debug     { get; private set; }
        public static Log? Info      { get; private set; }
        public static Log? Warning   { get; private set; }
        public static Log? Error     { get; private set; }
        public static Log? Guest     { get; private set; }
        public static Log? AccessLog { get; private set; }
        public static Log? Stub      { get; private set; }
        public static Log? Trace     { get; private set; }
        public static Log  Notice    { get; } // Always enabled

        static Logger()
        {
            m_EnabledClasses = new bool[Enum.GetNames(typeof(LogClass)).Length];

            for (int index = 0; index < m_EnabledClasses.Length; index++)
            {
                m_EnabledClasses[index] = true;
            }

            m_LogTargets = new List<ILogTarget>();

            m_Time = Stopwatch.StartNew();

            // Logger should log to console by default
            AddTarget(new AsyncLogTargetWrapper(
                new ConsoleLogTarget("console"),
                1000,
                AsyncLogTargetOverflowAction.Discard));

            Notice = new Log(LogLevel.Notice);
            
            // Enable important log levels before configuration is loaded
            Error = new Log(LogLevel.Error);
            Warning = new Log(LogLevel.Warning);
            Info = new Log(LogLevel.Info);
        }

        /// <summary>
        /// Resets the time elapsed since start to 0
        /// </summary>
        public static void RestartTime()
        {
            m_Time.Restart();
        }

        /// <summary>
        /// Retrieves the <see cref="ILogTarget"/> with the corresponding <c>Name</c>. Returns <c>null</c> if nothing was found.
        /// </summary>
        private static ILogTarget GetTarget(string targetName)
        {
            foreach (var target in m_LogTargets)
            {
                if (target.Name.Equals(targetName))
                {
                    return target;
                }
            }

            return null;
        }


        /// <summary>
        /// Adds a target to log when the <c>Updated</c> event is invoked.
        /// </summary>
        /// <remark>
        /// The target's <c>Log(object sender, LogEventArgs args)</c> method is added as an event handler.
        /// </remark>
        public static void AddTarget(ILogTarget target)
        {
            m_LogTargets.Add(target);

            Updated += target.Log;
        }


        /// <summary>
        /// Unsubscribes a target from receiving log notifications
        /// </summary>
        public static void RemoveTarget(string target)
        {
            ILogTarget logTarget = GetTarget(target);

            if (logTarget != null)
            {
                Updated -= logTarget.Log;

                m_LogTargets.Remove(logTarget);

                logTarget.Dispose();
            }
        }

        public static void Shutdown()
        {
            Updated = null;

            foreach (var target in m_LogTargets)
            {
                target.Dispose();
            }

            m_LogTargets.Clear();
        }

        /// <summary>
        /// Returns a read-only list of the enabled log levels
        /// </summary>
        public static IReadOnlyCollection<LogLevel> GetEnabledLevels()
        {
            var logs = new Log?[] { Trace, Debug, Info, Warning, Error, Guest, AccessLog, Stub };
            List<LogLevel> levels = new List<LogLevel>(logs.Length);
            foreach (var log in logs)
            {
                if (log.HasValue)
                {
                    levels.Add(log.Value.Level);
                }
            }

            return levels;
        }

        /// <summary>
        /// Enables/disables logging for a <see cref="LogLevel"/>
        /// </summary>
        public static void SetEnable(LogLevel logLevel, bool enabled)
        {
            switch (logLevel)
            {
                case LogLevel.Trace     : Trace     = enabled ? new Log(LogLevel.Trace)    : new Log?(); break;
                case LogLevel.Debug     : Debug     = enabled ? new Log(LogLevel.Debug)    : new Log?(); break;
                case LogLevel.Info      : Info      = enabled ? new Log(LogLevel.Info)     : new Log?(); break;
                case LogLevel.Warning   : Warning   = enabled ? new Log(LogLevel.Warning)  : new Log?(); break;
                case LogLevel.Error     : Error     = enabled ? new Log(LogLevel.Error)    : new Log?(); break;
                case LogLevel.Guest     : Guest     = enabled ? new Log(LogLevel.Guest)    : new Log?(); break;
                case LogLevel.AccessLog : AccessLog = enabled ? new Log(LogLevel.AccessLog): new Log?(); break;
                case LogLevel.Stub      : Stub      = enabled ? new Log(LogLevel.Stub)     : new Log?(); break;
                default: throw new ArgumentException("Unknown Log Level");
            }
        }

        /// <summary>
        /// Enables/disables logging for a <see cref="LogClass"/>
        /// </summary>
        public static void SetEnable(LogClass logClass, bool enabled)
        {
            m_EnabledClasses[(int)logClass] = enabled;
        }
    }
}
