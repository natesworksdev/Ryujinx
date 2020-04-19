using Ryujinx.Common.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Ryujinx.Common.Logging
{
    public sealed class ConsoleLogTarget : ILogTarget
    {
        private static readonly ReadOnlyDictionary<LogLevel, ConsoleColor> _logColors = new Dictionary<LogLevel, ConsoleColor>
        {
            [LogLevel.Stub] = ConsoleColor.DarkGray,
            [LogLevel.Info] = ConsoleColor.White,
            [LogLevel.Warning] = ConsoleColor.Yellow,
            [LogLevel.Error] = ConsoleColor.Red
        }.AsReadOnly();

        private readonly ILogFormatter _formatter;

        private readonly string _name;

        string ILogTarget.Name => _name;

        public ConsoleLogTarget(string name)
        {
            _formatter = new DefaultLogFormatter();
            _name      = name;
        }

        public void Log(object sender, LogEventArgs args)
        {
            if (_logColors.TryGetValue(args.Level, out ConsoleColor color))
            {
                Console.ForegroundColor = color;

                Console.WriteLine(_formatter.Format(args));

                Console.ResetColor();
            }
            else
            {
                Console.WriteLine(_formatter.Format(args));
            }
        }

        public void Dispose()
        {
            Console.ResetColor();
        }
    }
}
