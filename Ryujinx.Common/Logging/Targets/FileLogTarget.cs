using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;
using System.Runtime.InteropServices;

namespace Ryujinx.Common.Logging
{
    public class FileLogTarget : ILogTarget
    {
        private readonly StreamWriter  _logWriter;
        private readonly ILogFormatter _formatter;
        private readonly string        _name;

        string ILogTarget.Name { get => _name; }

        private static string _config = GetConfigName();

        // Build a config object, using env vars and JSON providers.
        private static IConfiguration config = new ConfigurationBuilder()
            .AddJsonFile(_config)
            .AddEnvironmentVariables()
            .Build();

        // Get values from the config given their key and their target type.
        public static Settings settings = config.GetRequiredSection("Settings").Get<Settings>();

        public FileLogTarget(string path, string name)
            : this(path, name, FileShare.Read, FileMode.Append)
        { }

        public FileLogTarget(string path, string name, FileShare fileShare, FileMode fileMode)
        {
            // Ensure directory is present
            DirectoryInfo logDir = new DirectoryInfo(Path.Combine(settings.logDir, "Logs"));
            logDir.Create();

            // Clean up old logs, should only keep 3
            FileInfo[] files = logDir.GetFiles("*.log").OrderBy((info => info.CreationTime)).ToArray();
            for (int i = 0; i < files.Length - 2; i++)
            {
                files[i].Delete();
            }

            string version = ReleaseInformation.GetVersion();

            // Get path for the current time
            path = Path.Combine(logDir.FullName, $"Ryujinx_{version}_{DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")}.log");

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

        private static string GetConfigName()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return "macOS.json";
            } 
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return "windows.json";
            } 
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return "linux.json";
            }

            throw new Exception("OS not detected.");
        }
    }
}
