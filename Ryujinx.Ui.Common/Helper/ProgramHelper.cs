using Ryujinx.Common;
using Ryujinx.Common.Configuration;
using Ryujinx.Common.Logging;
using Ryujinx.Common.SystemInfo;
using Ryujinx.Ui.Common.Configuration;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Ryujinx.Ui.Common.Helper
{
    public static class ProgramHelper
    {
        private static string _overrideGraphicsBackend = null;
        private static string _baseDirPathArg          = null;
        public  static string CommandLineProfile       = null;
        public  static string LaunchPathArg            = null;
        public  static bool   StartFullscreenArg       = false;
        public  static bool   ShowVulkanPrompt         = false;

        private const uint MB_ICONWARNING = 0x30;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int MessageBoxA(IntPtr hWnd, string text, string caption, uint type);

        private static void ParseArguments(string[] args)
        {
            // Parse Arguments.
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

                    _baseDirPathArg = args[++i];
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
                    StartFullscreenArg = true;
                }
                else if (arg == "-g" || arg == "--graphics-backend")
                {
                    if (i + 1 >= args.Length)
                    {
                        Logger.Error?.Print(LogClass.Application, $"Invalid option '{arg}'");

                        continue;
                    }

                    _overrideGraphicsBackend = args[++i];
                }
                else
                {
                    LaunchPathArg = arg;
                }
            }
        }

        // Return command line args without config overrides
        public static string[] GetCommandLineArgs()
        {
            var args = Environment.GetCommandLineArgs().AsEnumerable().Skip(1).ToList();

            if (args.Contains("-g") || args.Contains("--graphics-backend"))
            {
                args.RemoveRange(args.FindIndex(0, arg => arg == "-g" || arg == "--graphics-backend"), 2);
            }

            return args.ToArray();
        }

        public static void Initialize(string[] args)
        {
            if (OperatingSystem.IsWindows() && !OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17134))
            {
                MessageBoxA(IntPtr.Zero, "You are running an outdated version of Windows.\n\nStarting on June 1st 2022, Ryujinx will only support Windows 10 1803 and newer.\n", $"Ryujinx {ReleaseInformations.GetVersion()}", MB_ICONWARNING);
            }

            ParseArguments(args);

            // Setup base data directory.
            AppDataManager.Initialize(_baseDirPathArg);

            // Initialize the configuration.
            ConfigurationState.Initialize();

            // Initialize the logger system.
            LoggerModule.Initialize();

            // Initialize Discord integration.
            DiscordIntegrationModule.Initialize();
        }

        public static void PrintSystemInfo()
        {
            Logger.Notice.Print(LogClass.Application, $"Ryujinx Version: {ReleaseInformations.GetVersion()}");
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

        public static string LoadConfig()
        {
            string configurationPath        = null;
            string localConfigurationPath   = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config.json");
            string appDataConfigurationPath = Path.Combine(AppDataManager.BaseDirPath,            "Config.json");

            if (File.Exists(localConfigurationPath))
            {
                configurationPath = localConfigurationPath;
            }
            else if (File.Exists(appDataConfigurationPath))
            {
                configurationPath = appDataConfigurationPath;
            }

            if (configurationPath == null)
            {
                // No configuration, we load the default values and save it to disk
                configurationPath = appDataConfigurationPath;

                ConfigurationState.Instance.LoadDefault();
                ConfigurationState.Instance.ToFileFormat().SaveConfig(configurationPath);

                ShowVulkanPrompt = true;
            }
            else
            {
                if (ConfigurationFileFormat.TryLoad(configurationPath, out ConfigurationFileFormat configurationFileFormat))
                {
                    ConfigurationLoadResult result = ConfigurationState.Instance.Load(configurationFileFormat, configurationPath);

                    if ((result & ConfigurationLoadResult.MigratedFromPreVulkan) != 0)
                    {
                        ShowVulkanPrompt = true;
                    }
                }
                else
                {
                    ConfigurationState.Instance.LoadDefault();

                    ShowVulkanPrompt = true;

                    Logger.Warning?.PrintMsg(LogClass.Application, $"Failed to load config! Loading the default config instead.\nFailed config location {configurationPath}");
                }
            }

            // Check if graphics backend was overridden
            if (_overrideGraphicsBackend != null)
            {
                if (_overrideGraphicsBackend.ToLower() == "opengl")
                {
                    ConfigurationState.Instance.Graphics.GraphicsBackend.Value = GraphicsBackend.OpenGl;
                    ShowVulkanPrompt = false;
                }
                else if (_overrideGraphicsBackend.ToLower() == "vulkan")
                {
                    ConfigurationState.Instance.Graphics.GraphicsBackend.Value = GraphicsBackend.Vulkan;
                    ShowVulkanPrompt = false;
                }
            }

            return configurationPath;
        }
    }
}
