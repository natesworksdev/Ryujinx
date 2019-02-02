using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Ryujinx.Common.Logging
{
    public class ConsoleLogTarget : ILogTarget
    {
        private static readonly ConcurrentDictionary<LogLevel, ConsoleColor> _logColors;

        static ConsoleLogTarget()
        {
            _logColors = new ConcurrentDictionary<LogLevel, ConsoleColor> {
                [ LogLevel.Stub    ] = ConsoleColor.DarkGray,
                [ LogLevel.Info    ] = ConsoleColor.White,
                [ LogLevel.Warning ] = ConsoleColor.Yellow,
                [ LogLevel.Error   ] = ConsoleColor.Red
            };
        }

        public void Log(object sender, LogEventArgs e)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendFormat(@"{0:hh\:mm\:ss\.fff}", e.Time);
            sb.Append(" | ");
            sb.AppendFormat("{0:d4}", e.ThreadId);
            sb.Append(' ');
            sb.Append(e.Message);

            if (e.Data != null)
            {
                PropertyInfo[] props = e.Data.GetType().GetProperties();

                sb.Append(' ');

                foreach (var prop in props)
                {
                    sb.Append(prop.Name);
                    sb.Append(": ");
                    sb.Append(prop.GetValue(e.Data));
                    sb.Append(" - ");
                }

                // We remove the final '-' from the string
                if (props.Length > 0)
                {
                    sb.Remove(sb.Length - 3, 3);
                }
            }

            if (_logColors.TryGetValue(e.Level, out ConsoleColor color))
            {
                Console.ForegroundColor = color;

                Console.WriteLine(sb.ToString());

                Console.ResetColor();
            }
            else
            {
                Console.WriteLine(sb.ToString());
            }
        }

        public void Dispose()
        {
            Console.ResetColor();
        }
    }
}
