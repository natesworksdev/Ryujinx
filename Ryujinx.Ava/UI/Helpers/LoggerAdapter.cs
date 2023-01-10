using Avalonia.Utilities;
using System;
using System.Text;

namespace Ryujinx.Ava.UI.Helpers
{
    using AvaLogger = Avalonia.Logging.Logger;
    using AvaLogLevel = Avalonia.Logging.LogEventLevel;
    using RyuLogger = Ryujinx.Common.Logging.Logger;
    using RyuLogClass = Ryujinx.Common.Logging.LogClass;

    internal class LoggerAdapter : Avalonia.Logging.ILogSink
    {
        public static void Register()
        {
            AvaLogger.Sink = new LoggerAdapter();
        }

        public bool IsEnabled(AvaLogLevel level, string area)
        {
            return RyuLogger.Debug != null;
        }

        public void Log(AvaLogLevel level, string area, object source, string messageTemplate)
        {
            RyuLogger.Debug?.PrintMsg(RyuLogClass.Ui, Format(level, area, messageTemplate, source, null));
        }

        public void Log<T0>(AvaLogLevel level, string area, object source, string messageTemplate, T0 propertyValue0)
        {
            RyuLogger.Debug?.PrintMsg(RyuLogClass.Ui, Format(level, area, messageTemplate, source, new object[] { propertyValue0 }));
        }

        public void Log<T0, T1>(AvaLogLevel level, string area, object source, string messageTemplate, T0 propertyValue0,  T1 propertyValue1)
        {
            RyuLogger.Debug?.PrintMsg(RyuLogClass.Ui, Format(level, area, messageTemplate, source, new object[] { propertyValue0, propertyValue1 }));
        }

        public void Log<T0, T1, T2>(AvaLogLevel level, string area, object source, string messageTemplate, T0 propertyValue0, T1 propertyValue1, T2 propertyValue2)
        {
            RyuLogger.Debug?.PrintMsg(RyuLogClass.Ui, Format(level, area, messageTemplate, source, new object[] { propertyValue0, propertyValue1, propertyValue2 }));
        }

        public void Log(AvaLogLevel level, string area, object source, string messageTemplate, params object[] propertyValues)
        {
            RyuLogger.Debug?.PrintMsg(RyuLogClass.Ui, Format(level, area, messageTemplate, source, propertyValues));
        }

        private static string Format(AvaLogLevel level, string area, string template, object source, object[] v)
        {
            var result = new StringBuilder();
            var r = new CharacterReader(template.AsSpan());
            var i = 0;

            result.Append('[');
            result.Append(level);
            result.Append("] ");

            result.Append('[');
            result.Append(area);
            result.Append("] ");

            while (!r.End)
            {
                var c = r.Take();

                if (c != '{')
                {
                    result.Append(c);
                }
                else
                {
                    if (r.Peek != '{')
                    {
                        result.Append('\'');
                        result.Append(i < v.Length ? v[i++] : null);
                        result.Append('\'');
                        r.TakeUntil('}');
                        r.Take();
                    }
                    else
                    {
                        result.Append('{');
                        r.Take();
                    }
                }
            }

            if (source != null)
            {
                result.Append(" (");
                result.Append(source.GetType().Name);
                result.Append(" #");
                result.Append(source.GetHashCode());
                result.Append(')');
            }

            return result.ToString();
        }
    }
}