using Ryujinx.Audio;
using Ryujinx.Audio.OpenAL;
using Ryujinx.Graphics.Gal;
using Ryujinx.Graphics.Gal.OpenGL;
using Ryujinx.HLE;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Ryujinx
{
    class Program
    {
        private static DiscordRpc.RichPresence Presence;

        private static DiscordRpc.EventHandlers Handlers;

        static void Main(string[] args)
        {
            Console.Title = "Ryujinx Console";

            IGalRenderer Renderer = new OGLRenderer();

            IAalOutput AudioOut = new OpenALAudioOut();

            Switch Ns = new Switch(Renderer, AudioOut);

            Config.Read(Ns.Log);

            if (Config.DiscordRPCEnable)
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Handlers = new DiscordRpc.EventHandlers();
                    Presence = new DiscordRpc.RichPresence();

                    DiscordRpc.Initialize("467315377412767744", ref Handlers, true, null);
                }
            }

            Ns.Log.Updated += ConsoleLog.PrintLog;

            if (args.Length == 1)
            {
                if (Directory.Exists(args[0]))
                {
                    string[] RomFsFiles = Directory.GetFiles(args[0], "*.istorage");

                    if (RomFsFiles.Length == 0)
                    {
                        RomFsFiles = Directory.GetFiles(args[0], "*.romfs");
                    }

                    if (RomFsFiles.Length > 0)
                    {
                        Console.WriteLine("Loading as cart with RomFS.");

                        Ns.LoadCart(args[0], RomFsFiles[0]);
                    }
                    else
                    {
                        Console.WriteLine("Loading as cart WITHOUT RomFS.");

                        Ns.LoadCart(args[0]);
                    }

                    if (Config.DiscordRPCEnable)
                    {
                        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                        {
                            if (Ns.Os.SystemState.GetNpdmTitleName() != "Application")
                            {
                                Presence.details = $"{Ns.Os.SystemState.GetNpdmTitleName()} ({Ns.Os.SystemState.GetNpdmTitleId()})";
                            }
                            else
                            {
                                Presence.details = Ns.Os.SystemState.GetNpdmTitleId();
                            }

                            if (Ns.Os.SystemState.GetNpdmIs64Bit())
                            {
                                Presence.state = "Playing a 64-bit game!";
                            }
                            else
                            {
                                Presence.state = "Playing a 32-bit game!";
                            }

                            Presence.largeImageKey  = "icon";
                            Presence.largeImageText = "Ryujinx";

                            DiscordRpc.UpdatePresence(Presence);
                        }
                    }
                }
                else if (File.Exists(args[0]))
                {
                    Console.WriteLine("Loading as homebrew.");

                    Ns.LoadProgram(args[0]);
                }
            }
            else
            {
                Console.WriteLine("Please specify the folder with the NSOs/IStorage or a NSO/NRO.");
            }

            using (GLScreen Screen = new GLScreen(Ns, Renderer))
            {
                Ns.Finish += (Sender, Args) =>
                {
                    Screen.Exit();
                };

                Screen.MainLoop();
            }

            Environment.Exit(0);
        }
    }
}
