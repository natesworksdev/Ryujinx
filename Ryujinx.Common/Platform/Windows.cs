using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Ryujinx.Common.Platform
{

    [SupportedOSPlatform("windows")]
    internal class WindowsPlatform: Platform
    {
        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        override public void ShowConsole()
        {
            ShowWindow(GetConsoleWindow(), 5);
        }

        override public void HideConsole()
        {
            ShowWindow(GetConsoleWindow(), 0);
        }
    }
}
