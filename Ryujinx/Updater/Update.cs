using Gtk;
using Ryujinx.Common.Logging;
using Ryujinx.Ui;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Ryujinx.Updater
{
    public class Update
    {
        private static string   _parentdir   = Path.Combine(@"..\..");
        public  static string   RyuDir       = Environment.CurrentDirectory;

        public static void PerformUpdate()
        {
            try
            {
                //Get list of files from the current directory, and copy them to the parent directory.
                foreach (string _PathDir in Directory.GetDirectories(RyuDir, "*", SearchOption.AllDirectories))
                {
                    Directory.CreateDirectory(_PathDir.Replace(RyuDir, _parentdir));
                }

                foreach (string _PathNew in Directory.GetFiles(RyuDir, "*.*", SearchOption.AllDirectories))
                {
                    File.Copy(_PathNew, _PathNew.Replace(RyuDir, _parentdir), true);
                }

                Logger.PrintInfo(LogClass.Application, "Package installation was completed.\n");
                GtkDialog.CreateInfoDialog("Update", "Ryujinx - Update", "Almost finished", "The package was installed.\nPlease click ok, and the update will complete.");

                try
                {
                    Process.Start(new ProcessStartInfo(Path.Combine(_parentdir, "Ryujinx.exe"), "/C") { UseShellExecute = true });
                    Application.Quit();
                }
                catch (Exception ex)
                {
                    GtkDialog.CreateErrorDialog(ex.Message);
                }
            }
            catch (Exception ex)
            {
                GtkDialog.CreateErrorDialog(ex.Message);
            }
        }

        public static void Cleanup()
        {
            Directory.Delete(Path.Combine(RyuDir, "temp"), true);
        }
    }
}
