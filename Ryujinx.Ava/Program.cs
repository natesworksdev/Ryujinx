using ARMeilleure.Translation.PTC;
using Avalonia;
using Avalonia.Rendering;
using Avalonia.Threading;
using Ryujinx.Ava.Ui.Controls;
using Ryujinx.Ava.Ui.Windows;
using Ryujinx.Common;
using Ryujinx.Common.Configuration;
using Ryujinx.Common.GraphicsDriver;
using Ryujinx.Common.Logging;
using Ryujinx.Common.System;
using Ryujinx.Common.SystemInfo;
using Ryujinx.Modules;
using Ryujinx.Ui.Common;
using Ryujinx.Ui.Common.Configuration;
using Ryujinx.Ui.Common.Helper;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Ryujinx.Ava
{
    internal class Program
    {
        public static double WindowScaleFactor { get; set; }
        public static double ActualScaleFactor { get; set; }
        public static string Version { get; private set; }
        public static string ConfigurationPath { get; private set; }
        public static bool PreviewerDetached { get; private set; }

        public static RenderTimer RenderTimer { get; private set; }
        private const int BaseDpi = 96;

        public static void Main(string[] args)
        {
            Version = ReleaseInformations.GetVersion();

            PreviewerDetached = true;

            Initialize(args);

            RenderTimer = new RenderTimer();

            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

            RenderTimer.Dispose();
        }

        public static AppBuilder BuildAvaloniaApp()
        {
            return AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .With(new X11PlatformOptions
                {
                    EnableMultiTouch = true,
                    EnableIme = true,
                    UseEGL = false,
                    UseGpu = false
                })
                .With(new Win32PlatformOptions
                {
                    EnableMultitouch = true,
                    UseWgl = false,
                    AllowEglInitialization = false,
                    CompositionBackdropCornerRadius = 8f,
                })
                .UseSkia()
                .AfterSetup(_ =>
                {
                    AvaloniaLocator.CurrentMutable
                        .Bind<IRenderTimer>().ToConstant(RenderTimer)
                        .Bind<IRenderLoop>().ToConstant(new RenderLoop(RenderTimer, Dispatcher.UIThread));
                })
                .LogToTrace();
        }

        private static void Initialize(string[] args)
        {
            Console.Title = $"Ryujinx Console {Version}";

            // Hook unhandled exception and process exit events.
            AppDomain.CurrentDomain.UnhandledException += (object sender, UnhandledExceptionEventArgs e) => ProcessUnhandledException(e.ExceptionObject as Exception, e.IsTerminating);
            AppDomain.CurrentDomain.ProcessExit        += (object sender, EventArgs e)                   => Exit();

            // Delete backup files after updating.
            Task.Run(Updater.CleanupUpdate);

            // Perform common initialization steps
            ProgramHelper.Initialize(args);

            // Now load the configuration as the other subsystems are now registered
            ConfigurationPath = ProgramHelper.LoadConfig();

            ForceDpiAware.Windows();

            WindowScaleFactor = ForceDpiAware.GetWindowScaleFactor();
            ActualScaleFactor = ForceDpiAware.GetActualScaleFactor() / BaseDpi;

            // Logging system information.
            ProgramHelper.PrintSystemInfo();

            // Enable OGL multithreading on the driver, when available.
            BackendThreading threadingMode = ConfigurationState.Instance.Graphics.BackendThreading;
            DriverUtilities.ToggleOGLThreading(threadingMode == BackendThreading.Off);

            // Check if keys exists.
            bool hasSystemProdKeys = File.Exists(Path.Combine(AppDataManager.KeysDirPath, "prod.keys"));
            if (!hasSystemProdKeys)
            {
                if (!(AppDataManager.Mode == AppDataManager.LaunchMode.UserProfile && File.Exists(Path.Combine(AppDataManager.KeysDirPathUser, "prod.keys"))))
                {
                    MainWindow.ShowKeyErrorOnLoad = true;
                }
            }

            if (ProgramHelper.LaunchPathArg != null)
            {
                MainWindow.DeferLoadApplication(ProgramHelper.LaunchPathArg, ProgramHelper.StartFullscreenArg);
            }
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
