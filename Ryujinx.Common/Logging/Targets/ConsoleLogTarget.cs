using System;
using System.Collections.Concurrent;

namespace Ryujinx.Common.Logging
{
    public class ConsoleLogTarget : ILogTarget
    {
        private readonly ILogFormatter _formatter;

        private readonly string _name;

        string ILogTarget.Name { get => _name; }

        /// <summary>
        /// Returns the color of the text depending on the <see cref="LogLevel"/>
        /// </summary>
        private static ConsoleColor GetLogColor(LogLevel level) => level switch {
            LogLevel.Info    => ConsoleColor.White,
            LogLevel.Warning => ConsoleColor.Yellow,
            LogLevel.Error   => ConsoleColor.Red,
            LogLevel.Stub    => ConsoleColor.DarkGray,
            LogLevel.Notice  => ConsoleColor.Cyan,
            _                => ConsoleColor.Gray,
        };

        public ConsoleLogTarget(string name)
        {
            _formatter = new DefaultLogFormatter();
            _name      = name;
        }

        /// <summary>
        /// Logs the data to the console
        /// </summary>
        public void Log(object sender, LogEventArgs args)
        {
            Console.ForegroundColor = GetLogColor(args.Level);
            Console.WriteLine(_formatter.Format(args));
            Console.ResetColor();
        }

        public void Dispose()
        {
            Console.ResetColor();
        }
    }
}
