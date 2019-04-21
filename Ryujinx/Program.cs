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
        private static DiscordRpc.RichPresence Presence;

        private static DiscordRpc.EventHandlers Handlers;

        public static string ApplicationDirectory => AppDomain.CurrentDomain.BaseDirectory;

        static void Main(string[] args)
        {
            Console.Title = "Ryujinx Console";

            IGalRenderer renderer = new OglRenderer();

            IAalOutput audioOut = InitializeAudioEngine();

            Switch device = new Switch(renderer, audioOut);

            Configuration.Load(Path.Combine(ApplicationDirectory, "Config.jsonc"));
            Configuration.Configure(device);

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            AppDomain.CurrentDomain.ProcessExit        += CurrentDomain_ProcessExit;

            void SetPresence(string FileType)
            {
                if (File.Exists("./discord-rpc.dll")||File.Exists("./discord-rpc.so"))
                {
                    string[] RPsupported = File.ReadAllLines("./RPsupported");
                    if (RPsupported.Contains(device.System.TitleID))
                    {
                        Presence.largeImageKey = device.System.TitleID;
                    }
                    else
                    {
                        Presence.largeImageKey = "ryujinx";
                    }
                    Presence.details = $"Playing {device.System.TitleName}";
                    Presence.state = device.System.TitleID.ToUpper();
                    Presence.largeImageText = device.System.TitleName;
                    Presence.startTimestamp = DateTimeOffset.Now.ToUnixTimeSeconds();
                    Presence.smallImageKey = FileType;
                    Presence.smallImageText = FileType.ToUpper().Replace("-", " ");
                    DiscordRpc.UpdatePresence(Presence);
                }
            }

            if (File.Exists("./discord-rpc.dll") || File.Exists("./discord-rpc.so"))
            {
                Handlers = new DiscordRpc.EventHandlers();
                Presence = new DiscordRpc.RichPresence();
                DiscordRpc.Initialize("568815339807309834", ref Handlers, true, null);
            }

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
                        SetPresence("cart-with-romfs");
                    }
                    else
                    {
                        Logger.PrintInfo(LogClass.Application, "Loading as cart WITHOUT RomFS.");
                        device.LoadCart(args[0]);
                        SetPresence("cart-without-romfs");
                    }
                }
                else if (File.Exists(args[0]))
                {
                    switch (Path.GetExtension(args[0]).ToLowerInvariant())
                    {
                        case ".xci":
                            Logger.PrintInfo(LogClass.Application, "Loading as XCI.");
                            device.LoadXci(args[0]);
                            SetPresence("xci");
                            break;
                        case ".nca":
                            Logger.PrintInfo(LogClass.Application, "Loading as NCA.");
                            device.LoadNca(args[0]);
                            SetPresence("nca");
                            break;
                        case ".nsp":
                        case ".pfs0":
                            Logger.PrintInfo(LogClass.Application, "Loading as NSP.");
                            device.LoadNsp(args[0]);
                            SetPresence("nsp");
                            break;
                        default:
                            Logger.PrintInfo(LogClass.Application, "Loading as homebrew.");
                            device.LoadProgram(args[0]);
                            SetPresence("nro-nso");
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
        }

        private static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            Logger.Shutdown();
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;

            Logger.PrintError(LogClass.Emulation, $"Unhandled exception caught: {exception}");

            if (e.IsTerminating)
            {
                Logger.Shutdown();
            }
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
