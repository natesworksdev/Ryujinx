﻿using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Ryujinx.Common.Logging
{
    /// <summary>
    /// A target for logging to the file system
    /// </summary>
    public class FileLogTarget : ILogTarget
    {
        private static readonly ObjectPool<StringBuilder> _stringBuilderPool = SharedPools.Default<StringBuilder>();

        private readonly StreamWriter  _logWriter;
        private readonly ILogFormatter _formatter;
        private readonly string        _name;

        string ILogTarget.Name { get => _name; }
        
        /// <param name="path">The path of the directory to log to (stores logs in a folder under the directory named "Logs")</param>
        /// <param name="name">The name of this <c>FileLogTarget</c> instance (not the filename)</param>
        public FileLogTarget(string path, string name)
            : this(path, name, FileShare.Read, FileMode.Append)
        { }

        public FileLogTarget(string path, string name, FileShare fileShare, FileMode fileMode)
        {
            // Ensure directory is present
            DirectoryInfo logDir = new DirectoryInfo(Path.Combine(path, "Logs"));
            logDir.Create();

            // Clean up old logs, should only keep 3
            FileInfo[] files = logDir.GetFiles("*.log").OrderBy((info => info.CreationTime)).ToArray();
            for (int i = 0; i < files.Length - 2; i++)
            {
                files[i].Delete();
            }

            // Get path for the current time
            path = Path.Combine(logDir.FullName, $"Ryujinx_{DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")}.log");

            _name      = name;
            _logWriter = new StreamWriter(File.Open(path, fileMode, FileAccess.Write, fileShare));
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
