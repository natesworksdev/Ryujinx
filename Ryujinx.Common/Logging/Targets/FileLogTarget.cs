using System.IO;
using System.Reflection;
using System.Text;

namespace Ryujinx.Common.Logging
{
    public class FileLogTarget : ILogTarget
    {
        private static readonly ObjectPool<StringBuilder> _stringBuilderPool = SharedPools.Default<StringBuilder>();

        private readonly StreamWriter _logWriter;

        public FileLogTarget(string path)
            : this(path, FileShare.Read, FileMode.Append)
        { }

        public FileLogTarget(string path, FileShare fileShare, FileMode fileMode)
        {
            _logWriter = new StreamWriter(File.Open(path, fileMode, FileAccess.Write, fileShare));
        }

        public void Log(object sender, LogEventArgs e)
        {
            StringBuilder sb = _stringBuilderPool.Allocate();

            try
            {
                sb.Clear();

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

                _logWriter.WriteLine(sb.ToString());
                _logWriter.Flush();
            }
            finally
            {
                _stringBuilderPool.Release(sb);
            }
        }

        public void Dispose()
        {
            _logWriter.WriteLine("---- End of Log ----");
            _logWriter.Flush();
            _logWriter.Dispose();
        }
    }
}
