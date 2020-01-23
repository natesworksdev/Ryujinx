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
        public  static string   RyuDir           = Environment.CurrentDirectory;
        private static string[] UpdateFiles      = Directory.GetFiles(Path.Combine(Environment.CurrentDirectory),"*", SearchOption.AllDirectories);
        private static string   ParentDir        = Path.Combine(@"..\..");
        public static void PerformUpdate()
        {
            try
            {
                foreach (string _PathDir in Directory.GetDirectories(RyuDir, "*",
                    SearchOption.AllDirectories))
                    Directory.CreateDirectory(_PathDir.Replace(RyuDir, ParentDir));
                foreach (string _PathNew in Directory.GetFiles(RyuDir, "*.*",
                    SearchOption.AllDirectories))
                    File.Copy(_PathNew, _PathNew.Replace(RyuDir, ParentDir), true);
                Logger.PrintInfo(LogClass.Application, "Package installation was completed.\n");
                GtkDialog.CreateInfoDialog("Update", "Ryujinx - Update", "Almost finished","The package was installed.\nPlease click ok, and the update will complete.");
                try
                {
                    Process.Start(new ProcessStartInfo(Path.Combine(ParentDir, "Ryujinx.exe"), "/C") { UseShellExecute = true });
                }
                catch (System.ComponentModel.Win32Exception)
                {
                    GtkDialog.CreateErrorDialog("Update canceled by user or the installation was not found");
                    return;
                }
                Application.Quit();
                return;
            }
            catch (System.ComponentModel.Win32Exception)
            {
                //Logger.PrintError(LogClass.Application, "Package installation has failed\n" + ex.InnerException.ToString());
                GtkDialog.CreateErrorDialog("Package installation has failed\nCheck the log for more information.");
                return;
            }
           
        }

        public static void Cleanup()
        {
            Directory.Delete(Path.Combine(RyuDir, "temp"), true);
            return;
        }
    }
}
