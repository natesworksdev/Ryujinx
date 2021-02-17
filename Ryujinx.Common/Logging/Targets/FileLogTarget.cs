using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Ryujinx.Common.Logging
{
    public class FileLogTarget : ILogTarget
    {
        private StreamWriter  _logWriter;
        private readonly ILogFormatter _formatter;
        private readonly string        _name;

        string ILogTarget.Name { get => _name; }

        public FileLogTarget(string path, string name)
            : this(path, name, FileShare.Read, FileMode.Append)
        { }

        public FileLogTarget(string path, string name, FileShare fileShare, FileMode fileMode)
        {
            DirectoryInfo logDir;

            // Ensure directory is present
            if (Directory.Exists(path))
            {
                 logDir = new DirectoryInfo(path);
            }
            else
            {
                logDir = new DirectoryInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs"));
                logDir.Create();
            }
            


            // Clean up old logs, should only keep 3
            FileInfo[] files = logDir.GetFiles("*.log").OrderBy((info => info.CreationTime)).ToArray();
            for (int i = 0; i < files.Length - 2; i++)
            {
                files[i].Delete();
            }

            string version = Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;

            // Get path for the current time
            path = Path.Combine(logDir.FullName, $"Ryujinx_{version}_{DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")}.log");

            _name      = name;
            try { 
                _logWriter = new StreamWriter(File.Open(path, fileMode, FileAccess.Write, fileShare));
            }
            catch
            {
                return;
                //Target was already added and the file is now locked
            }
            _formatter = new DefaultLogFormatter();
        }

        public void Log(object sender, LogEventArgs args)
        {
            _logWriter.WriteLine(_formatter.Format(args));
            _logWriter.Flush();
        }

        public void Dispose()
        {
            _logWriter.WriteLine("---- End of Log ----");
            _logWriter.Flush();
            _logWriter.Dispose();
        }
    }
}
