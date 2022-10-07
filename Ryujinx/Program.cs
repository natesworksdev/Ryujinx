using ARMeilleure.Translation.PTC;
using Gtk;
using Ryujinx.Common;
using Ryujinx.Common.Configuration;
using Ryujinx.Common.GraphicsDriver;
using Ryujinx.Common.Logging;
using Ryujinx.Common.System;
using Ryujinx.Modules;
using Ryujinx.Ui;
using Ryujinx.Ui.Common;
using Ryujinx.Ui.Common.Configuration;
using Ryujinx.Ui.Common.Helper;
using Ryujinx.Ui.Widgets;
using SixLabors.ImageSharp.Formats.Jpeg;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Ryujinx
{
    class Program
    {
        public static double WindowScaleFactor { get; private set; }

        public static string Version { get; private set; }

        public static string ConfigurationPath { get; set; }

        [DllImport("libX11")]
        private extern static int XInitThreads();

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int MessageBoxA(IntPtr hWnd, string text, string caption, uint type);

        private const uint MB_ICONWARNING = 0x30;

        static void Main(string[] args)
        {
            Version = ReleaseInformations.GetVersion();
            Console.Title = $"Ryujinx Console {Version}";

            // Hook unhandled exception and process exit events.
            GLib.ExceptionManager.UnhandledException   += (GLib.UnhandledExceptionArgs e)                => ProcessUnhandledException(e.ExceptionObject as Exception, e.IsTerminating);
            AppDomain.CurrentDomain.UnhandledException += (object sender, UnhandledExceptionEventArgs e) => ProcessUnhandledException(e.ExceptionObject as Exception, e.IsTerminating);
            AppDomain.CurrentDomain.ProcessExit        += (object sender, EventArgs e)                   => Exit();

            // Make process DPI aware for proper window sizing on high-res screens.
            ForceDpiAware.Windows();
            WindowScaleFactor = ForceDpiAware.GetWindowScaleFactor();

            // Delete backup files after updating.
            Task.Run(Updater.CleanupUpdate);

            // Perform common initialization steps
            ProgramHelper.Initialize(args);

            // NOTE: GTK3 doesn't init X11 in a multi threaded way.
            // This ends up causing race condition and abort of XCB when a context is created by SPB (even if SPB do call XInitThreads).
            if (OperatingSystem.IsLinux())
            {
                XInitThreads();
            }

            string systemPath = Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.Machine);
            Environment.SetEnvironmentVariable("Path", $"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin")};{systemPath}");

            // Sets ImageSharp Jpeg Encoder Quality.
            SixLabors.ImageSharp.Configuration.Default.ImageFormatsManager.SetEncoder(JpegFormat.Instance, new JpegEncoder()
            {
                Quality = 100
            });

            // Now load the configuration as the other subsystems are now registered
            ConfigurationPath = ProgramHelper.LoadConfig();

            // Logging system information.
            ProgramHelper.PrintSystemInfo();

            // Enable OGL multithreading on the driver, when available.
            BackendThreading threadingMode = ConfigurationState.Instance.Graphics.BackendThreading;
            DriverUtilities.ToggleOGLThreading(threadingMode == BackendThreading.Off);

            // Initialize Gtk.
            Application.Init();

            // Check if keys exists.
            bool hasSystemProdKeys = File.Exists(Path.Combine(AppDataManager.KeysDirPath, "prod.keys"));
            bool hasCommonProdKeys = AppDataManager.Mode == AppDataManager.LaunchMode.UserProfile && File.Exists(Path.Combine(AppDataManager.KeysDirPathUser, "prod.keys"));
            if (!hasSystemProdKeys && !hasCommonProdKeys)
            {
                UserErrorDialog.CreateUserErrorDialog(UserError.NoKeys);
            }

            // Show the main window UI.
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();

            if (ProgramHelper.LaunchPathArg != null)
            {
                mainWindow.LoadApplication(ProgramHelper.LaunchPathArg, ProgramHelper.StartFullscreenArg);
            }

            if (ConfigurationState.Instance.CheckUpdatesOnStart.Value && Updater.CanUpdate(false))
            {
                Updater.BeginParse(mainWindow, false).ContinueWith(task =>
                {
                    Logger.Error?.Print(LogClass.Application, $"Updater Error: {task.Exception}");
                }, TaskContinuationOptions.OnlyOnFaulted);
            }

            if (ProgramHelper.ShowVulkanPrompt)
            {
                var buttonTexts = new Dictionary<int, string>()
                {
                    { 0, "Yes (Vulkan)" },
                    { 1, "No (OpenGL)" }
                };

                ResponseType response = GtkDialog.CreateCustomDialog(
                    "Ryujinx - Default graphics backend",
                    "Use Vulkan as default graphics backend?",
                    "Ryujinx now supports the Vulkan API. " +
                    "Vulkan greatly improves shader compilation performance, " +
                    "and fixes some graphical glitches; however, since it is a new feature, " +
                    "you may experience some issues that did not occur with OpenGL.\n\n" +
                    "Note that you will also lose any existing shader cache the first time you start a game " +
                    "on version 1.1.200 onwards, because Vulkan required changes to the shader cache that makes it incompatible with previous versions.\n\n" +
                    "Would you like to set Vulkan as the default graphics backend? " +
                    "You can change this at any time on the settings window.",
                    buttonTexts,
                    MessageType.Question);

                ConfigurationState.Instance.Graphics.GraphicsBackend.Value = response == 0
                    ? GraphicsBackend.Vulkan
                    : GraphicsBackend.OpenGl;

                ConfigurationState.Instance.ToFileFormat().SaveConfig(Program.ConfigurationPath);
            }

            Application.Run();
        }

        private static void ProcessUnhandledException(Exception ex, bool isTerminating)
        {
            Ptc.Close();
            PtcProfiler.Stop();

            string message = $"Unhandled exception caught: {ex}";

            Logger.Error?.PrintMsg(LogClass.Application, message);

            if (Logger.Error == null)
            {
                Logger.Notice.PrintMsg(LogClass.Application, message);
            }

            if (isTerminating)
            {
                Exit();
            }
        }

        public static void Exit()
        {
            DiscordIntegrationModule.Exit();

            Ptc.Dispose();
            PtcProfiler.Dispose();

            Logger.Shutdown();
        }
    }
}
