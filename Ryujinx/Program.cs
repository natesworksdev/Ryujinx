using Gtk;
using Ryujinx.Common.Logging;
using Ryujinx.Profiler;
using System;
using System.IO;

namespace Ryujinx
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "Ryujinx Console";

            string systemPATH = Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.Machine);
            Environment.SetEnvironmentVariable("Path", $"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin")};{systemPATH}");

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            AppDomain.CurrentDomain.ProcessExit        += CurrentDomain_ProcessExit;

            Profile.Initialize();

            ApplicationLibrary.Init();

            Application.Init();

            Application gtkapp = new Application("Ryujinx.Ryujinx", GLib.ApplicationFlags.None);
            MainMenu win       = new MainMenu(args, gtkapp);

            gtkapp.Register(GLib.Cancellable.Current);
            gtkapp.AddWindow(win);
            win.Show();

            Application.Run();
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
    }
}