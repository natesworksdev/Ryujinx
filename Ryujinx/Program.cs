using Ryujinx.Audio;
using Ryujinx.Common.Logging;
using Ryujinx.Graphics.Gal;
using Ryujinx.Graphics.Gal.OpenGL;
using Ryujinx.HLE;
using System;
using System.IO;
using System.Linq;

namespace Ryujinx
{
    class Program
    {
        public static DiscordRPC.DiscordRpcClient DiscordClient;

        public static DiscordRPC.RichPresence DiscordPresence;

        public static string ApplicationDirectory => AppDomain.CurrentDomain.BaseDirectory;

        static void Main(string[] args)
        {
            Console.Title = "Ryujinx Console";

            DiscordClient                           = new DiscordRPC.DiscordRpcClient("568815339807309834");
            DiscordPresence                         = new DiscordRPC.RichPresence();
            DiscordPresence.Assets                  = new DiscordRPC.Assets();
            DiscordPresence.Timestamps              = new DiscordRPC.Timestamps(DateTime.UtcNow);

            DiscordPresence.Details                 = "Ryujinx Console";
            DiscordPresence.State                   = "Reading the console logs...";
            DiscordPresence.Assets.LargeImageKey    = "ryujinx";
            DiscordPresence.Assets.LargeImageText   = "Ryujinx";

            DiscordClient.Initialize();
            DiscordClient.SetPresence(DiscordPresence);

            IGalRenderer renderer = new OglRenderer();

            IAalOutput audioOut = InitializeAudioEngine();

            Switch device = new Switch(renderer, audioOut);

            Configuration.Load(Path.Combine(ApplicationDirectory, "Config.jsonc"));
            Configuration.Configure(device);

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            AppDomain.CurrentDomain.ProcessExit        += CurrentDomain_ProcessExit;

            if (args.Length == 1)
            {
                if (Directory.Exists(args[0]))
                {
                    string[] romFsFiles = Directory.GetFiles(args[0], "*.istorage");

                    if (romFsFiles.Length == 0)
                    {
                        romFsFiles = Directory.GetFiles(args[0], "*.romfs");
                    }

                    if (romFsFiles.Length > 0)
                    {
                        Logger.PrintInfo(LogClass.Application, "Loading as cart with RomFS.");
                        device.LoadCart(args[0], romFsFiles[0]);
                        SetGamePresence(device);
                    }
                    else
                    {
                        Logger.PrintInfo(LogClass.Application, "Loading as cart WITHOUT RomFS.");
                        device.LoadCart(args[0]);
                        SetGamePresence(device);
                    }
                }
                else if (File.Exists(args[0]))
                {
                    switch (Path.GetExtension(args[0]).ToLowerInvariant())
                    {
                        case ".xci":
                            Logger.PrintInfo(LogClass.Application, "Loading as XCI.");
                            device.LoadXci(args[0]);
                            SetGamePresence(device);
                            break;
                        case ".nca":
                            Logger.PrintInfo(LogClass.Application, "Loading as NCA.");
                            device.LoadNca(args[0]);
                            SetGamePresence(device);
                            break;
                        case ".nsp":
                        case ".pfs0":
                            Logger.PrintInfo(LogClass.Application, "Loading as NSP.");
                            device.LoadNsp(args[0]);
                            SetGamePresence(device);
                            break;
                        default:
                            Logger.PrintInfo(LogClass.Application, "Loading as homebrew.");
                            device.LoadProgram(args[0]);
                            SetGamePresence(device);
                            break;
                    }
                }
                else
                {
                    Logger.PrintWarning(LogClass.Application, "Please specify a valid XCI/NCA/NSP/PFS0/NRO file");
                }
            }
            else
            {
                Logger.PrintWarning(LogClass.Application, "Please specify the folder with the NSOs/IStorage or a NSO/NRO.");
            }

            using (GlScreen screen = new GlScreen(device, renderer))
            {
                screen.MainLoop();

                device.Dispose();
            }

            audioOut.Dispose();

            Logger.Shutdown();

            DiscordClient.Dispose();
        }

        private static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            Logger.Shutdown();

            DiscordClient.Dispose();
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;

            Logger.PrintError(LogClass.Emulation, $"Unhandled exception caught: {exception}");

            if (e.IsTerminating)
            {
                Logger.Shutdown();

                DiscordClient.Dispose();
            }
        }

        private static void SetGamePresence(Switch device)
        {
            if (File.ReadAllLines(Path.Combine(ApplicationDirectory, "RPsupported.dat")).Contains(device.System.TitleID))
            {
                DiscordPresence.Assets.LargeImageKey    = device.System.TitleID;
                DiscordPresence.Assets.LargeImageText   = device.System.TitleName;
            }
            DiscordPresence.Details                     = $"Playing {device.System.TitleName}";
            DiscordPresence.State                       = device.System.TitleID.ToUpper();
            DiscordPresence.Assets.SmallImageKey        = "ryujinx";
            DiscordPresence.Assets.SmallImageText       = "Ryujinx";
            DiscordPresence.Timestamps                  = new DiscordRPC.Timestamps(DateTime.UtcNow);

            DiscordClient.SetPresence(DiscordPresence);
        }

        /// <summary>
        /// Picks an <see cref="IAalOutput"/> audio output renderer supported on this machine
        /// </summary>
        /// <returns>An <see cref="IAalOutput"/> supported by this machine</returns>
        private static IAalOutput InitializeAudioEngine()
        {
            if (SoundIoAudioOut.IsSupported)
            {
                return new SoundIoAudioOut();
            }
            else if (OpenALAudioOut.IsSupported)
            {
                return new OpenALAudioOut();
            }
            else
            {
                return new DummyAudioOut();
            }
        }
    }
}
