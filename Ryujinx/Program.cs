using Gtk;
using Ryujinx.Common.Logging;
using Ryujinx.Configuration;
using Ryujinx.Profiler;
using Ryujinx.Ui;
using System;
using System.IO;

namespace Ryujinx
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "Ryujinx Console";

            string systemPath = Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.Machine);
            Environment.SetEnvironmentVariable("Path", $"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin")};{systemPath}");

            GLib.ExceptionManager.UnhandledException += Glib_UnhandledException;

            // Initialize the configuration
            ConfigurationState.Initialize();

            // Initialize the logger system
            LoggerModule.Initialize();

            // Initialize Discord integration
            DiscordIntegrationModule.Initialize();

            // Now load the configuration as the other subsystem are now registered
            ConfigurationFileFormat configurationFileFormat = ConfigurationFileFormat.Load(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config.json"));
            ConfigurationState.Instance.Load(configurationFileFormat);

            Profile.Initialize();

            Application.Init();

            string appDataPath     = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RyuFs", "system", "prod.keys");
            string userProfilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".switch", "prod.keys");
            if (!File.Exists(appDataPath) && !File.Exists(userProfilePath))
            {
                GtkDialog.CreateErrorDialog($"Key file was not found. Please refer to `KEYS.md` for more info");
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
    }
}