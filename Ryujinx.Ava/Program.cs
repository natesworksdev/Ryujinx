using ARMeilleure.Translation.PTC;
using Avalonia;
using Avalonia.OpenGL;
using Avalonia.Rendering;
using Avalonia.Threading;
using FFmpeg.AutoGen;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Ryujinx.Ava.Application.Module;
using Ryujinx.Ava.Common;
using Ryujinx.Ava.Common.Locale;
using Ryujinx.Ava.Ui.Controls;
using Ryujinx.Ava.Ui.Windows;
using Ryujinx.Common;
using Ryujinx.Common.Configuration;
using Ryujinx.Common.GraphicsDriver;
using Ryujinx.Common.Logging;
using Ryujinx.Common.System;
using Ryujinx.Common.SystemInfo;
using Ryujinx.Configuration;
using Ryujinx.Modules;
using SixLabors.ImageSharp.Formats.Jpeg;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Ryujinx.Ava
{
    internal class Program
    {
        public static double WindowScaleFactor { get; set; }
        public static string Version           { get; private set; }
        public static string ConfigurationPath { get; private set; }

        public static string CommandLineProfile { get; set; }
        public static bool   PreviewerDetached { get; private set; }
        public static AdjustableRenderTimer RenderTimer { get; private set; }

        // NOTE: Initialization code. Don't use any Avalonia, third-party APIs or any SynchronizationContext-reliant code before AppMain is called:
        //       Things aren't initialized yet and stuff might break.
        public static void Main(string[] args)
        {
            PreviewerDetached = true;
            
            Initialize(args);

            RenderTimer = new AdjustableRenderTimer(60);

            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

            RenderTimer.Dispose();
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
        {
            return AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .With(new X11PlatformOptions
                {
                    EnableMultiTouch = true,
                    EnableIme = true,
                    UseEGL = false,
                    UseGpu = true,
                    GlProfiles = new[] { new GlVersion(GlProfileType.OpenGL, 4, 3) }
                })
                .With(new Win32PlatformOptions
                {
                    EnableMultitouch = true,
                    UseWindowsUIComposition = true,
                    UseWgl = true,
                    AllowEglInitialization = false,
                    CompositionBackdropCornerRadius = 8f,
                    WglProfiles = new[] { new GlVersion(GlProfileType.OpenGL, 4, 3) }
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
            // Parse Arguments.
            string launchPathArg      = null;
            string baseDirPathArg     = null;
            bool   startFullscreenArg = false;

            for (int i = 0; i < args.Length; ++i)
            {
                string arg = args[i];

                if (arg == "-r" || arg == "--root-data-dir")
                {
                    if (i + 1 >= args.Length)
                    {
                        Logger.Error?.Print(LogClass.Application, $"Invalid option '{arg}'");

                        continue;
                    }

                    baseDirPathArg = args[++i];
                }
                else if (arg == "-p" || arg == "--profile")
                {
                    if (i + 1 >= args.Length)
                    {
                        Logger.Error?.Print(LogClass.Application, $"Invalid option '{arg}'");

                        continue;
                    }

                    CommandLineProfile = args[++i];
                }
                else if (arg == "-f" || arg == "--fullscreen")
                {
                    startFullscreenArg = true;
                }
                else if (launchPathArg == null)
                {
                    launchPathArg = arg;
                }
            }

            // Make process DPI aware for proper window sizing on high-res screens.
            ForceDpiAware.Windows();
            WindowScaleFactor = 1;//ForceDpiAware.GetWindowScaleFactor();

            // Delete backup files after updating.
            Task.Run(Updater.CleanupUpdate);

            Version = ReleaseInformations.GetVersion();;

            Console.Title = $"Ryujinx Console {Version}";

            string systemPath = Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.Machine);
            Environment.SetEnvironmentVariable("Path", $"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin")};{systemPath}");

            // Hook unhandled exception and process exit events.
            AppDomain.CurrentDomain.UnhandledException += (object sender, UnhandledExceptionEventArgs e) => ProcessUnhandledException(e.ExceptionObject as Exception, e.IsTerminating);
            AppDomain.CurrentDomain.ProcessExit        += (object sender, EventArgs e)                   => Exit();

            // Setup base data directory.
            AppDataManager.Initialize(baseDirPathArg);

            // Initialize the configuration.
            ConfigurationState.Initialize();

            // Initialize the logger system.
            LoggerModule.Initialize();

            // Initialize Discord integration.
            DiscordIntegrationModule.Initialize();

            // Sets ImageSharp Jpeg Encoder Quality.
            SixLabors.ImageSharp.Configuration.Default.ImageFormatsManager.SetEncoder(JpegFormat.Instance, new JpegEncoder()
            {
                Quality = 100
            });

            ReloadConfig();

            // Logging system information.
            PrintSystemInfo();

            // Enable OGL multithreading on the driver, when available.
            BackendThreading threadingMode = ConfigurationState.Instance.Graphics.BackendThreading;
            DriverUtilities.ToggleOGLThreading(threadingMode == BackendThreading.Off);

            // Check if keys exists.
            bool hasSystemProdKeys = File.Exists(Path.Combine(AppDataManager.KeysDirPath, "prod.keys"));
            bool hasCommonProdKeys = AppDataManager.Mode == AppDataManager.LaunchMode.UserProfile && File.Exists(Path.Combine(AppDataManager.KeysDirPathUser, "prod.keys"));
            if (!hasSystemProdKeys && !hasCommonProdKeys)
            {
                MainWindow.ShowKeyErrorOnLoad = true;
            }

            if (launchPathArg != null)
            {
                MainWindow.DeferLoadApplication(launchPathArg, startFullscreenArg);
            }
        }

        private static void ReloadConfig()
        {
            string localConfigurationPath   = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config.json");
            string appDataConfigurationPath = Path.Combine(AppDataManager.BaseDirPath,            "Config.json");

            // Now load the configuration as the other subsystems are now registered
            ConfigurationPath = File.Exists(localConfigurationPath) ? localConfigurationPath : File.Exists(appDataConfigurationPath) ? appDataConfigurationPath : null;

            if (ConfigurationPath == null)
            {
                // No configuration, we load the default values and save it to disk
                ConfigurationPath = appDataConfigurationPath;

                ConfigurationState.Instance.LoadDefault();
                ConfigurationState.Instance.ToFileFormat().SaveConfig(ConfigurationPath);
            }
            else
            {
                if (ConfigurationFileFormat.TryLoad(ConfigurationPath, out ConfigurationFileFormat configurationFileFormat))
                {
                    ConfigurationState.Instance.Load(configurationFileFormat, ConfigurationPath);
                }
                else
                {
                    ConfigurationState.Instance.LoadDefault();

                    Logger.Warning?.PrintMsg(LogClass.Application, $"Failed to load config! Loading the default config instead.\nFailed config location {ConfigurationPath}");
                }
            }
        }

        private static void PrintSystemInfo()
        {
            Logger.Notice.Print(LogClass.Application, $"Ryujinx Version: {Version}");
            SystemInfo.Gather().Print();

            var enabledLogs = Logger.GetEnabledLevels();
            Logger.Notice.Print(LogClass.Application, $"Logs Enabled: {(enabledLogs.Count == 0 ? "<None>" : string.Join(", ", enabledLogs))}");

            if (AppDataManager.Mode == AppDataManager.LaunchMode.Custom)
            {
                Logger.Notice.Print(LogClass.Application, $"Launch Mode: Custom Path {AppDataManager.BaseDirPath}");
            }
            else
            {
                Logger.Notice.Print(LogClass.Application, $"Launch Mode: {AppDataManager.Mode}");
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