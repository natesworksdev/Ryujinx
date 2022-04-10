using Ryujinx.Common.Logging;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Ryujinx.Ava.Common
{
    internal static class OpenHelper
    {
        public static void OpenFolder(string path)
        {
            Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true, Verb = "open" });
        }

        public static void OpenUrl(string url)
        {
            if (OperatingSystem.IsWindows())
            {
                Process.Start(new ProcessStartInfo("cmd", $"/c start {url.Replace("&", "^&")}"));
            }
            else if (OperatingSystem.IsLinux())
            {
                Process.Start("xdg-open", url);
            }
            else if (OperatingSystem.IsMacOS())
            {
                Process.Start("open", url);
            }
            else
            {
                Logger.Notice.Print(LogClass.Application, $"Cannot open url \"{url}\" on this platform!");
            }
        }
    }
}