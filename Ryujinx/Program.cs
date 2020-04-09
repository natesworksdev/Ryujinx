using Gtk;
using Ryujinx.Common.Logging;
using Ryujinx.Configuration;
using Ryujinx.Debugger.Profiler;
using Ryujinx.Ui;
using OpenTK;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Ryujinx
{
    class Program
    {
        public static string Version { get; private set; }

        public static string ConfigurationPath { get; set; }

        static void Main(string[] args)
        {
            Toolkit.Init(new ToolkitOptions
            {
                Backend = PlatformBackend.PreferNative,
                EnableHighResolution = true
            });

            Version = Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;

            Console.Title = $"Ryujinx Console {Version}";

            string systemPath = Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.Machine);
            Environment.SetEnvironmentVariable("Path", $"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin")};{systemPath}");

            GLib.ExceptionManager.UnhandledException += Glib_UnhandledException;

            // Initialize the configuration
            ConfigurationState.Initialize();

            // Initialize the logger system
            LoggerModule.Initialize();

            // Initialize Discord integration
            DiscordIntegrationModule.Initialize();

            Logger.PrintInfo(LogClass.Application, $"Ryujinx Version: {Version}");
            Logger.PrintInfo(LogClass.Application, $"Operating System: {RuntimeInformation.OSDescription} ({RuntimeInformation.OSArchitecture})");
            Logger.PrintInfo(LogClass.Application, $"CPU: {GetCpuName()}");
            Logger.PrintInfo(LogClass.Application, $"Total RAM: {GetRamSizeMb()} MB");

            string localConfigurationPath  = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config.json");
            string globalBasePath          = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Ryujinx");
            string globalConfigurationPath = Path.Combine(globalBasePath, "Config.json");

            // Now load the configuration as the other subsystems are now registered
            if (File.Exists(localConfigurationPath))
            {
                ConfigurationPath = localConfigurationPath;

                ConfigurationFileFormat configurationFileFormat = ConfigurationFileFormat.Load(localConfigurationPath);

                ConfigurationState.Instance.Load(configurationFileFormat, ConfigurationPath);
            }
            else if (File.Exists(globalConfigurationPath))
            {
                ConfigurationPath = globalConfigurationPath;

                ConfigurationFileFormat configurationFileFormat = ConfigurationFileFormat.Load(globalConfigurationPath);

                ConfigurationState.Instance.Load(configurationFileFormat, ConfigurationPath);
            }
            else
            {
                // No configuration, we load the default values and save it on disk
                ConfigurationPath = globalConfigurationPath;

                // Make sure to create the Ryujinx directory if needed.
                Directory.CreateDirectory(globalBasePath);

                ConfigurationState.Instance.LoadDefault();
                ConfigurationState.Instance.ToFileFormat().SaveConfig(globalConfigurationPath);
            }

            Profile.Initialize();

            Application.Init();

            string globalProdKeysPath = Path.Combine(globalBasePath, "system", "prod.keys");
            string userProfilePath    = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".switch", "prod.keys");
            if (!File.Exists(globalProdKeysPath) && !File.Exists(userProfilePath) && !Migration.IsMigrationNeeded())
            {
                GtkDialog.CreateWarningDialog("Key file was not found", "Please refer to `KEYS.md` for more info");
            }

            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();

            if (args.Length == 1)
            {
                mainWindow.LoadApplication(args[0]);
            }

            Application.Run();
        }

        private static void Glib_UnhandledException(GLib.UnhandledExceptionArgs e)
        {
            Exception exception = e.ExceptionObject as Exception;

            Logger.PrintError(LogClass.Application, $"Unhandled exception caught: {exception}");

            if (e.IsTerminating)
            {
                Logger.Shutdown();
            }
        }

        private static string GetCpuName()
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    using (Process process = Process.Start(new ProcessStartInfo
                    {
                        FileName               = "wmic",
                        Arguments              = "cpu get Name /Value",
                        RedirectStandardOutput = true
                    }))
                    {
                        return process.StandardOutput.ReadToEnd().Trim().Split("=")[1];
                    }
                }
                else
                {
                    using (Process process = Process.Start(new ProcessStartInfo
                    {
                        FileName               = "cat",
                        Arguments              = "/proc/cpuinfo",
                        RedirectStandardOutput = true
                    }))
                    {
                        foreach (string line in process.StandardOutput.ReadToEnd().Split("\n").Where(line => line.StartsWith("model name")))
                        {
                            return line.Split(":")[1].Trim();
                        }

                        return "Unknown";
                    }
                }
            }
            catch 
            {
                return "Unknown";
            }
        }

        private static double GetRamSizeMb()
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    using (Process process = Process.Start(new ProcessStartInfo
                    {
                        FileName               = "wmic",
                        Arguments              = "OS get TotalVisibleMemorySize /Value",
                        RedirectStandardOutput = true
                    }))
                    {
                        return Math.Round(double.Parse(process.StandardOutput.ReadToEnd().Trim().Split("=")[1]) / 1024, 0);
                    }
                }
                else
                {
                    using (Process process = Process.Start(new ProcessStartInfo
                    {
                        FileName               = "cat",
                        Arguments              = "/proc/meminfo",
                        RedirectStandardOutput = true
                    }))
                    {
                        return Math.Round(double.Parse(process.StandardOutput.ReadToEnd().Split("\n")[0].Split(":")[1].Trim().Split(" ")[0]) / 1024, 0);
                    }
                }
            }
            catch
            {
                return 0;
            }
        }
    }
}