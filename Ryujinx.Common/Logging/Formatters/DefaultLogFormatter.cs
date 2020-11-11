using System;
using System.Reflection;
using System.Text;

namespace Ryujinx.Common.Logging
{
    internal class DefaultLogFormatter : ILogFormatter
    {
        private static readonly ObjectPool<StringBuilder> _stringBuilderPool = SharedPools.Default<StringBuilder>();

        /// <summary>
        /// Formats the logged data into readable text
        /// </summary>
        /// <example>
        /// 00:00:12.092  |W|   HLE.HostThread.0   KernelSvc   PrintResult: SendSyncRequest64 returned error PortRemoteClosed.
        ///   Time       Level    Thread           Log Class     Caller                Message
        /// </example>
        public string Format(LogEventArgs args)
        {
            StringBuilder sb = _stringBuilderPool.Allocate();

            try
            {
                sb.Clear();

                sb.AppendFormat(@"{0:hh\:mm\:ss\.fff}", args.Time);
                sb.Append($" |{args.Level.ToString()[0]}| ");

                if (args.ThreadName != null)
                {
                    sb.Append(args.ThreadName);
                    sb.Append(' ');
                }

                string message = (args.Level == LogLevel.Stub) ? "Stubbed. " + args.Message : args.Message;
                sb.Append($"{args.Class} {args.Caller}: {message}");

                if (args.Data != null)
                {
                    PropertyInfo[] props = args.Data.GetType().GetProperties();

                    sb.Append(" {");

                    foreach (var prop in props)
                    {
                        sb.Append(prop.Name);
                        sb.Append(": ");

                        if (typeof(Array).IsAssignableFrom(prop.PropertyType))
                        {
                            Array array = (Array)prop.GetValue(args.Data);
                            foreach (var item in array)
                            {
                                sb.Append(item.ToString());
                                sb.Append(", ");
                            }

                            if (array.Length > 0)
                            {
                                sb.Remove(sb.Length - 2, 2);
                            }
                        }
                        else
                        {
                            sb.Append(prop.GetValue(args.Data));
                        }

                        sb.Append(" ; ");
                    }

                    // We remove the final ';' from the string
                    if (props.Length > 0)
                    {
                        sb.Remove(sb.Length - 3, 3);
                    }

                    sb.Append('}');
                }

                return sb.ToString();
            }
            finally
            {
                _stringBuilderPool.Release(sb);
            }
        }
    }
}
