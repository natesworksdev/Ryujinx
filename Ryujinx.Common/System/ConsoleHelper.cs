using Microsoft.Win32.SafeHandles;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace Ryujinx.Common.System
{
    public class ConsoleHelper
    {
        [DllImport("kernel32.dll")]
        private static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32")]
        static extern bool AllocConsole();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll", SetLastError = true)]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("kernel32.dll")]
        static extern uint GetCurrentProcessId();

        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        private static readonly string VERSION = Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;

        private const int STD_OUTPUT_HANDLE = -11;
        private const int MY_CODE_PAGE = 437;

        private const int SW_SHOW = 5;
        private const int SW_HIDE = 0;

        public static bool IsHideSupported()
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        }

        private static uint GetWindowId()
        {
            IntPtr consoleWindow = GetConsoleWindow();

            uint dwProcessId;
            GetWindowThreadProcessId(consoleWindow, out dwProcessId);

            return dwProcessId;
        }

        public static void CreateConsole()
        {
            if (AllocConsole())
            {
                IntPtr stdHandle = GetStdHandle(STD_OUTPUT_HANDLE);
                SafeFileHandle safeFileHandle = new SafeFileHandle(stdHandle, true);
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                FileStream fileStream = new FileStream(safeFileHandle, FileAccess.Write);
                Encoding encoding = Encoding.GetEncoding(MY_CODE_PAGE);
                StreamWriter standardOutput = new StreamWriter(fileStream, encoding);
                standardOutput.AutoFlush = true;
                Console.SetOut(standardOutput);
                Console.Title = $"Ryujinx Console {VERSION}";
            }
            else if (GetCurrentProcessId() == GetWindowId())
            {
                ShowWindow(GetConsoleWindow(), SW_SHOW);
            }
        }

        public static void ShowConsole()
        {
            CreateConsole();
        }

        public static void HideConsole()
        {
            if (GetCurrentProcessId() == GetWindowId())
            {
                ShowWindow(GetConsoleWindow(), SW_HIDE);
            }
        }
    }
}